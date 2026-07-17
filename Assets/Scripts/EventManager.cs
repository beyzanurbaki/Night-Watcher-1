using UnityEngine;

// Gece boyunca belirli periyotlarla rastgele mahalle olaylarını (gürültü, parktan gelen sesler vb.) tetikleyen sınıftır.
public class EventManager : MonoBehaviour
{
    // Singleton tasarım deseni (Single Instance)
    public static EventManager Instance;

    [Header("Olay Ayarlari")]
    // Olayların ne kadar sürede bir kontrol edilip tetikleneceği (saniye)
    public float eventCheckInterval = 15f;
    private float eventTimer = 0f;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // Zaman Yöneticisindeki tetikleme olaylarına dinleyici ekler.
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.OnTriggerActivated.AddListener(OnTimeTrigger);
        }
    }

    void Update()
    {
        // Olaylar sadece gece aktif olduğunda gerçekleşir.
        if (!TimeManager.Instance.isNightActive) return;

        // Geçen süreyi sayaçta biriktirir.
        eventTimer += Time.deltaTime;

        // Belirlenen süre aralığına ulaşıldığında rastgele bir olay tetikler.
        if (eventTimer >= eventCheckInterval)
        {
            eventTimer = 0f;
            SpawnRandomEvent();
        }
    }

    // Zaman olaylarına (Gündüzden geceye geçiş vb.) verilen tepki
    void OnTimeTrigger(string triggerType)
    {
        // Gece basladiginda olay zamanlayicisini sifirla
        if (triggerType == "night_time")
        {
            eventTimer = 0f;
        }
    }

    // Rastgele bir mahalle olayı belirleyip tetikleyici yöneticisine (TriggerManager) gönderen metot.
    void SpawnRandomEvent()
    {
        int random = Random.Range(0, 4);

        switch (random)
        {
            case 0:
                Debug.Log("Mahallede gurultu duyuldu!");
                TriggerManager.Instance.HandleTrigger("loud_noise");
                break;

            case 1:
                Debug.Log("Parktan sesler geliyor!");
                TriggerManager.Instance.HandleTrigger("location_park");
                break;

            case 2:
                Debug.Log("Suphe cekici bi durum var!");
                TriggerManager.Instance.HandleTrigger("threat_nearby");
                break;

            case 3:
                // Sessiz gece, olay yok
                Debug.Log("Sakin bir gece...");
                TriggerManager.Instance.HandleTrigger("quiet_night");
                break;
        }
    }
}