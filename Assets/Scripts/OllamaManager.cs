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
    // Aynı anda bekleyen istek sayısı (FrameTimeTracker'ın kare başına loglaması için;
    // bool yerine sayı: "boşta / tek istek / örtüşen istek" ayrımı analizde geri alınamaz kaybolmasın diye)
    public int ActiveRequestCount => activeRequests;

    // Performans ölçümü sırasında FrameTimeTracker'ı elle Inspector'dan eklemeye gerek kalmasın diye
    // burada kendini otomatik ekliyor. RequireComponent zaten bu objeyi zorunlu kılıyor.
    private void Awake()
    {
        if (GetComponent<FrameTimeTracker>() == null)
            gameObject.AddComponent<FrameTimeTracker>();
    }

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
        SendMessageToNPC(npcModelName, playerMessage, onReply, temperature, "");
    }

    // NPC modeline `/api/chat` uç noktası üzerinden mesaj gönderir ve cevabı geri döndürür.
    // conditionTag: performans ölçümü için bellek durumu vb. etiketi (boş bırakılırsa loglanmaz).
    public void SendMessageToNPC(string npcModelName, string playerMessage, Action<string> onReply, float customTemp, string conditionTag = "")
    {
        if (string.IsNullOrWhiteSpace(playerMessage))
            return;

        string safeModelName = SanitizeModelName(npcModelName);
        StartCoroutine(CallOllama(safeModelName, playerMessage, onReply, customTemp, conditionTag));
    }

    // Arka planda Ollama sunucusuyla HTTP POST üzerinden haberleşen asenkron metot.
    private IEnumerator CallOllama(string modelName, string playerMessage, Action<string> onReply, float temp, string conditionTag)
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

            var networkSw = System.Diagnostics.Stopwatch.StartNew();
            yield return request.SendWebRequest();
            networkSw.Stop();

            if (request.result == UnityWebRequest.Result.Success)
            {
                var deserializeSw = System.Diagnostics.Stopwatch.StartNew();
                OllamaResponse responseData = JsonUtility.FromJson<OllamaResponse>(request.downloadHandler.text);
                deserializeSw.Stop();

                if (responseData != null && responseData.message != null)
                {
                    // Çıkarım gecikmesi: istek gönderiminden yanıtın C# tarafına ulaşmasına kadar (madde 1)
                    // Bu satır aynı zamanda PerfLogger'daki canlı ilerleme sayacını (npc/koşul -> n/30) besler.
                    PerfLogger.Log("InferenceLatency", modelName, networkSw.Elapsed.TotalMilliseconds,
                        responseData.prompt_eval_count, responseData.eval_count, conditionTag);
                    // Ana iş parçacığı bloğu, sadece JsonUtility.FromJson kısmı
                    // (LimitReplyByWords ayrı bir dosyada/callback'te, NPCController tarafında ayrıca loglanıyor)
                    PerfLogger.Log("DeserializeBlock", modelName, deserializeSw.Elapsed.TotalMilliseconds, condition: conditionTag);
                    // Ollama'nın kendi bildirdiği sunucu-içi kırılım (nanosaniye -> ms)
                    PerfLogger.Log("LoadDuration", modelName, responseData.load_duration / 1_000_000.0, condition: conditionTag);
                    PerfLogger.Log("PromptEvalDuration", modelName, responseData.prompt_eval_duration / 1_000_000.0, condition: conditionTag);
                    PerfLogger.Log("EvalDuration", modelName, responseData.eval_duration / 1_000_000.0, condition: conditionTag);

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

    // DOĞRULA: alan adları Ollama /api/chat dokümantasyonundan alındı, bu makinedeki
    // Ollama sürümüyle curl edip teyit edilmedi. JsonUtility eşleşmeyen alanı sessizce
    // 0 bırakır — kullanmadan önce ham JSON'a bak.
    // Süreler nanosaniye cinsindendir (ms için 1_000_000'a bölünür).
    public bool done;
    public long total_duration;
    public long load_duration;
    public int prompt_eval_count;
    public long prompt_eval_duration;
    public int eval_count;
    public long eval_duration;
}

[Serializable]
public class OllamaMessage
{
    public string role;
    public string content;
}