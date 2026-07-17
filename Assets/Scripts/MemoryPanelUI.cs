using UnityEngine;
using TMPro;

// Oyun içi ekranda karakterlerin (NPC) anlık durumlarını, duygusal bellek düzeylerini,
// en güçlü hafıza türlerini ve oyunun performans istatistiklerini (FPS) gösteren panel arayüz yöneticisidir.
public class MemoryPanelUI : MonoBehaviour
{
    [Header("NPC Referanslari")]
    public NPCController ahmetNPC;
    public NPCController ayseNPC;
    public NPCController mehmetNPC;

    [Header("UI Text Referanslari")]
    public TextMeshProUGUI ahmetInfoText;
    public TextMeshProUGUI ayseInfoText;
    public TextMeshProUGUI mehmetInfoText;
    public TextMeshProUGUI statsText; // FPS ve CPU değerlerini gösterecek olan Text

    [Header("Ollama Referans")]
    public OllamaManager ollamaManager;

    private float deltaTime = 0.0f;

    // CPU takibi için kullanılan sistem işlem değişkenleri
    private System.Diagnostics.Process currentProcess;
    private System.TimeSpan lastCpuTime;
    private float lastSampleTime;
    private float currentCpuUsagePercentage = 0f;

    void Start()
    {
        // Ollama yöneticisini sahnede bulur
        if (ollamaManager == null)
        {
            ollamaManager = FindObjectOfType<OllamaManager>();
        }

        // CPU kullanımı ölçümü için sistem sürecini başlatır
        try
        {
            currentProcess = System.Diagnostics.Process.GetCurrentProcess();
            lastCpuTime = currentProcess.TotalProcessorTime;
            lastSampleTime = Time.realtimeSinceStartup;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("CPU tracking initialization failed: " + e.Message);
        }
    }

    void Update()
    {
        // FPS hesaplaması için geçen süreyi yumuşatarak alır
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        UpdatePanel();
    }

    // Panel verilerini her karede güncelleyen ana fonksiyon
    void UpdatePanel()
    {
        // Ahmet Amca bilgilerini güncelle
        if (ahmetNPC != null && ahmetInfoText != null)
        {
            float disposition = ahmetNPC.GetOverallDisposition();
            string label = ahmetNPC.GetDispositionLabel();
            int memoryCount = ahmetNPC.memories.Count;
            string strongest = GetStrongestMemory(ahmetNPC);

            ahmetInfoText.text =
                $"{GetAIStatus(ahmetNPC)}\n" +
                $"{label} ({disposition:F2})\n" +
                $"{memoryCount}\n" +
                $"{strongest}";
        }

        // Ayşe Teyze bilgilerini güncelle
        if (ayseNPC != null && ayseInfoText != null)
        {
            float disposition = ayseNPC.GetOverallDisposition();
            string label = ayseNPC.GetDispositionLabel();
            int memoryCount = ayseNPC.memories.Count;
            string strongest = GetStrongestMemory(ayseNPC);

            ayseInfoText.text =
                $"{GetAIStatus(ayseNPC)}\n" +
                $"{label} ({disposition:F2})\n" +
                $"{memoryCount}\n" +
                $"{strongest}";
        }

        // Mehmet Amca bilgilerini güncelle
        if (mehmetNPC != null && mehmetInfoText != null)
        {
            float disposition = mehmetNPC.GetOverallDisposition();
            string label = mehmetNPC.GetDispositionLabel();
            int memoryCount = mehmetNPC.memories.Count;
            string strongest = GetStrongestMemory(mehmetNPC);

            mehmetInfoText.text =
                $"{GetAIStatus(mehmetNPC)}\n" +
                $"{label} ({disposition:F2})\n" +
                $"{memoryCount}\n" +
                $"{strongest}";
        }

        // İşlemin CPU kullanım yüzdesini hesaplar (Aşırı yük oluşturmamak için 0.5 saniyede bir ölçüm yapar)
        if (currentProcess != null)
        {
            float currentTime = Time.realtimeSinceStartup;
            float timeDiff = currentTime - lastSampleTime;
            if (timeDiff >= 0.5f)
            {
                try
                {
                    System.TimeSpan currentCpuTime = currentProcess.TotalProcessorTime;
                    double cpuTimeMs = (currentCpuTime - lastCpuTime).TotalMilliseconds;
                    double systemTimeMs = timeDiff * 1000f;
                    // Çok çekirdekli işlemciler için çekirdek sayısına bölünür
                    double usage = (cpuTimeMs / (systemTimeMs * System.Environment.ProcessorCount)) * 100f;
                    
                    currentCpuUsagePercentage = Mathf.Clamp((float)usage, 0f, 100f);
                    
                    lastCpuTime = currentCpuTime;
                    lastSampleTime = currentTime;
                }
                catch (System.Exception)
                {
                    // Geçici sistem hatalarını yok say
                }
            }
        }

        // FPS değerini arayüze yazdırır
        if (statsText != null)
        {
            float msec = deltaTime * 1000.0f;
            float fps = 1.0f / deltaTime;
            statsText.text = $"{fps:F1} FPS ({msec:F1}ms)";
        }
    }

    // NPC'nin düşünme durumunu kontrol edip animasyonlu (Düşünüyor...) şekilde döndüren metot
    string GetAIStatus(NPCController npc)
    {
        if (npc.isThinking)
        {
            // Zaman katsayısına göre dinamik nokta sayısını belirler (nokta animasyonu)
            int dotsCount = (int)(Time.time * 3f) % 4; 
            string dots = new string('.', dotsCount);
            return $"Düşünüyor{dots}";
        }
        return "Hazır";
    }

    // NPC'nin hafızasındaki en yüksek etkiye (mutlak değerce en büyük) sahip anıyı bulan yardımcı metot
    string GetStrongestMemory(NPCController npc)
    {
        if (npc.memories.Count == 0) return "Yok";

        NPCMemory strongest = npc.memories[0];
        float strongestValue = Mathf.Abs(strongest.GetStrength());

        foreach (var memory in npc.memories)
        {
            float value = Mathf.Abs(memory.GetStrength());
            if (value > strongestValue)
            {
                strongest = memory;
                strongestValue = value;
            }
        }

        return $"{strongest.eventType} ({strongest.GetStrength():F2})";
    }
}