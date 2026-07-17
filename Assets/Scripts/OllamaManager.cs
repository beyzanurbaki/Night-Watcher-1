using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;

// Local'de (127.0.0.1:11434) çalışan Ollama API ile iletişimi kuran,
// NPC modellerini oluşturan ve metin tabanlı sohbet isteklerini ileten yöneticidir.
public class OllamaManager : MonoBehaviour
{
    [Header("Ollama Server Settings")]
    [SerializeField] private string baseUrl = "http://localhost:11434/api";
    [SerializeField] private string baseModel = "phi3"; // Kullanılacak temel LLM modeli
    [SerializeField] private float temperature = 0.1f;
    [SerializeField] private int maxPredict = 12; // Modelin üretebileceği maksimum kelime/token sayısı

    private int activeRequests = 0;
    // Sunucuya o an gönderilen aktif bir istek olup olmadığını belirtir
    public bool IsThinking => activeRequests > 0;

    // Ollama API'si üzerinden `/api/create` uç noktasına istek göndererek NPC'ye özel sistem yönergelerine sahip yeni bir model oluşturur.
    public IEnumerator CreateNPCModel(string npcModelName, string systemPrompt, Action<bool> onDone = null)
    {
        string safeModelName = SanitizeModelName(npcModelName);
        string safeSystemPrompt = EscapeJson(systemPrompt);

        // JSON Payload yapısı
        string jsonPayload = "{"
            + "\"model\":\"" + safeModelName + "\","
            + "\"from\":\"" + baseModel + "\","
            + "\"system\":\"" + safeSystemPrompt + "\","
            + "\"stream\":false"
            + "}";

        activeRequests++;
        using (UnityWebRequest request = new UnityWebRequest($"{baseUrl}/create", "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = 120; // Büyük modeller için uzun zaman aşımı süresi

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"<color=green>Ollama:</color> {safeModelName} model created.");
                onDone?.Invoke(true);
            }
            else
            {
                Debug.LogError($"Model Creation Error ({safeModelName}): {request.downloadHandler.text}");
                onDone?.Invoke(false);
            }
        }
        activeRequests--;
    }

    public void SendMessageToNPC(string npcModelName, string playerMessage, Action<string> onReply = null)
    {
        SendMessageToNPC(npcModelName, playerMessage, onReply, temperature);
    }

    // NPC modeline `/api/chat` uç noktası üzerinden mesaj gönderir ve cevabı geri döndürür.
    public void SendMessageToNPC(string npcModelName, string playerMessage, Action<string> onReply, float customTemp)
    {
        if (string.IsNullOrWhiteSpace(playerMessage))
            return;

        string safeModelName = SanitizeModelName(npcModelName);
        StartCoroutine(CallOllama(safeModelName, playerMessage, onReply, customTemp));
    }

    // Arka planda Ollama sunucusuyla HTTP POST üzerinden haberleşen asenkron metot.
    private IEnumerator CallOllama(string modelName, string playerMessage, Action<string> onReply, float temp)
    {
        // İstek verisi nesnesi
        OllamaChatRequest requestData = new OllamaChatRequest
        {
            model = modelName,
            messages = new OllamaRequestMessage[]
            {
                new OllamaRequestMessage
                {
                    role = "user",
                    content = playerMessage
                }
            },
            stream = false,
            options = new OllamaOptions
            {
                temperature = temp,
                num_predict = maxPredict
            }
        };

        string jsonBody = JsonUtility.ToJson(requestData);

        activeRequests++;
        using (UnityWebRequest request = new UnityWebRequest($"{baseUrl}/chat", "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = 60;

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                OllamaResponse responseData = JsonUtility.FromJson<OllamaResponse>(request.downloadHandler.text);

                if (responseData != null && responseData.message != null)
                {
                    // Başarılı cevabı geri döndürür
                    onReply?.Invoke(responseData.message.content.Trim());
                }
                else
                {
                    Debug.LogError($"Ollama Parse Error ({modelName}): {request.downloadHandler.text}");
                }
            }
            else
            {
                Debug.LogError($"Ollama Chat Error ({modelName}): {request.downloadHandler.text}");
            }
        }
        activeRequests--;
    }

    // Model isimlerinin sadece harf, sayı ve tire içermesini sağlayan temizleme fonksiyonu.
    public string SanitizeModelName(string rawName)
    {
        string clean = rawName.ToLower().Trim();
        StringBuilder sb = new StringBuilder();

        foreach (char c in clean)
        {
            if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '-')
                sb.Append(c);
            else if (c == ' ' || c == '_')
                sb.Append('-');
        }

        return sb.ToString();
    }

    // JSON string değerlerinde hata oluşturabilecek kaçış karakterlerini (escape characters) temizleyen fonksiyon.
    private string EscapeJson(string text)
    {
        return text
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "");
    }
}

[Serializable]
public class OllamaChatRequest
{
    public string model;
    public OllamaRequestMessage[] messages;
    public bool stream;
    public OllamaOptions options;
}

[Serializable]
public class OllamaRequestMessage
{
    public string role;
    public string content;
}

[Serializable]
public class OllamaOptions
{
    public float temperature;
    public int num_predict;
}

[Serializable]
public class OllamaResponse
{
    public OllamaMessage message;
}

[Serializable]
public class OllamaMessage
{
    public string role;
    public string content;
}