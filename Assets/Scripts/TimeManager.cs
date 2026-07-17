using UnityEngine;
using UnityEngine.Events;

// Oyundaki zaman akışını (gün/gece döngüsü), gün değişimini ve
// sokak lambalarının otomatik olarak ilklendirilmesini kontrol eden yöneticidir.
public class TimeManager : MonoBehaviour
{
    public static TimeManager Instance;

    [Header("Zaman Ayarlari")]
    public float dayDuration = 60f; // Bir tam gün/gece döngüsünün süresi (saniye)

    [Header("Durum")]
    public float currentTime = 0f;
    public int currentDay = 1;
    public bool isNightActive = false;

    [Header("Gece Efekti")]
    public GameObject darkOverlay; // Gece olduğunda ekranı karartan UI paneli

    [Header("Events")]
    // Zaman değişikliklerini (gece oldu, sabah oldu vb.) dinleyen sistem olayları
    public UnityEvent<string> OnTriggerActivated;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // Sahnedeki sokak lambalarını bulur ve kontrollerini ekler
        InitializeStreetLights();
    }

    // Sahnedeki "Street-light" isimli tüm nesnelere dinamik olarak StreetLightController scriptini bağlar.
    void InitializeStreetLights()
    {
        GameObject decorations = GameObject.Find("Decorations");
        if (decorations != null)
        {
            foreach (Transform child in decorations.transform)
            {
                if (child.name.Contains("Street-light"))
                {
                    if (child.gameObject.GetComponent<StreetLightController>() == null)
                    {
                        child.gameObject.AddComponent<StreetLightController>();
                    }
                }
            }
        }
        else
        {
            // Fallback (Yedek arama yöntemi): Sahnedeki tüm objeleri kontrol eder
            foreach (GameObject go in GameObject.FindObjectsOfType<GameObject>())
            {
                if (go.name.Contains("Street-light"))
                {
                    if (go.GetComponent<StreetLightController>() == null)
                    {
                        go.AddComponent<StreetLightController>();
                    }
                }
            }
        }
    }

    void Update()
    {
        // Zamanı akıtır
        currentTime += Time.deltaTime;

        // Döngü süresinin yarısına gelindiğinde ve henüz gece değilse geceyi başlatır
        if (currentTime >= dayDuration / 2f && !isNightActive)
        {
            StartNight();
        }

        // Gün süresi dolduğunda yeni güne geçiş yapar
        if (currentTime >= dayDuration)
        {
            StartNewDay();
        }
    }

    // Gece dönemini başlatan metot.
    void StartNight()
    {
        isNightActive = true;
        Debug.Log($"Gece {currentDay} basladi!");

        // Görev yöneticisini yeni gece hakkında bilgilendirir
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnNewNight();
        }

        // Çevresel olay tetikleyicilerine ilgili gece bildirimlerini gönderir
        OnTriggerActivated?.Invoke("night_time");
        OnTriggerActivated?.Invoke("darkness");
        OnTriggerActivated?.Invoke("night_patrol");

        // Ekran karartma panelini aktif yapar
        if (darkOverlay != null)
        {
            darkOverlay.SetActive(true);
        }
    }

    // Yeni gün/sabah dönemini başlatan metot.
    void StartNewDay()
    {
        isNightActive = false;
        currentTime = 0f;
        currentDay++;
        Debug.Log($"Gun {currentDay} basladi!");

        // Oyuncunun günlük konuşma/etkileşim hakkını sabah sıfırlar
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ResetInteractions();
        }

        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnNewDay();
        }

        // Yeni gün başladığında tüm NPC'lerin hafızasını (ve dolayısıyla anlık tutumlarını) sıfırlar
        if (TriggerManager.Instance != null)
        {
            TriggerManager.Instance.ResetAllNPCMemories();
        }

        // Gündüz olayı tetiklenir
        OnTriggerActivated?.Invoke("daytime");

        // Ekran karartma panelini deaktif yapar
        if (darkOverlay != null)
        {
            darkOverlay.SetActive(false);
        }
    }
}