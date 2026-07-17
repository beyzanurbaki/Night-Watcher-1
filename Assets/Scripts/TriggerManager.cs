using UnityEngine;

// Çevresel veya sistemsel tetikleyicileri (gürültü, karanlık vb.) yakalayıp 
// bunları oyundaki tüm NPC'lerin belleklerine (ActivateTrigger) ve arayüz bildirimlerine ileten yöneticidir.
public class TriggerManager : MonoBehaviour
{
    public static TriggerManager Instance;

    [Header("All NPCs")]
    public NPCController[] allNPCs; // Sahnedeki tüm NPC'lerin listesi

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // Zaman Yöneticisindeki tetikleme olaylarını bu sınıfa bağlar.
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.OnTriggerActivated.AddListener(HandleTrigger);
        }

        // Sahnedeki tüm NPC'leri tarar ve listeye ekler.
        RefreshNPCList();
    }

    // Sahnedeki tüm aktif NPCController bileşenlerini bulup listeyi günceller.
    public void RefreshNPCList()
    {
        allNPCs = FindObjectsByType<NPCController>(FindObjectsSortMode.None);
    }

    // Oyundaki tüm NPC'lerin anılarını sıfırlar.
    public void ResetAllNPCMemories()
    {
        if (allNPCs == null || allNPCs.Length == 0)
            RefreshNPCList();

        foreach (var npc in allNPCs)
        {
            if (npc != null)
                npc.ClearMemories();
        }
    }

    // Bir tetikleme (olay) meydana geldiğinde çağrılan ana metot.
    public void HandleTrigger(string triggerType)
    {
        if (allNPCs == null || allNPCs.Length == 0)
            RefreshNPCList();

        Debug.Log($"Trigger activated: {triggerType}");

        // Tetikleyici olay türünü Türkçe'ye çevirip arayüzde bildirim (Notification) olarak gösterir.
        if (UIManager.Instance != null)
        {
            string translatedMessage = TranslateTrigger(triggerType);
            if (!string.IsNullOrEmpty(translatedMessage))
            {
                UIManager.Instance.ShowEventNotification(translatedMessage);
            }
        }

        // Tetikleyiciyi tüm aktif NPC'lerin yapay zeka sistemine/belleğine gönderir.
        foreach (var npc in allNPCs)
        {
            if (npc != null)
                npc.ActivateTrigger(triggerType);
        }
    }

    // İngilizce tetikleyici anahtarlarını arayüzde gösterilmek üzere Türkçe açıklamalara dönüştürür.
    private string TranslateTrigger(string triggerType)
    {
        string t = triggerType.ToLower();
        int day = TimeManager.Instance != null ? TimeManager.Instance.currentDay : 1;

        if (t.Contains("night_time")) return $"Gece {day} başladı!";
        if (t.Contains("darkness")) return "Karanlık her yeri kapladı.";
        if (t.Contains("daytime") || t.Contains("morning")) return $"Gün {day} başladı!";
        if (t.Contains("loud_noise") || t.Contains("noise")) return "Mahallede büyük bir gürültü duyuldu!";
        if (t.Contains("location_park") || t.Contains("park")) return "Parkın yakınlarından şüpheli sesler geliyor...";
        if (t.Contains("threat_nearby") || t.Contains("threat")) return "Yakınlarda tehlikeli bir durum var!";
        if (t.Contains("safe")) return "Etraf tekrar sakinleşti.";
        if (t.Contains("rain")) return "Yağmur yağmaya başladı.";
        if (t.Contains("quiet_night")) return "Sakin bir gece...";

        return null; // Tanımlanmamış veya içsel tetikleyiciler için bildirim panelini tetiklemez
    }
}