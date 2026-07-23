using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Oyundaki etkileşim menülerini (NPC ile konuşma, eylem seçimi vb.),
// oyuncunun etkileşim haklarının yönetimini ve ekrandaki fade efektli bildirim panellerini kontrol eder.
public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("UI References")]
    public GameObject interactionPanel; // NPC etkileşim penceresi

    [Header("Memory Panel")]
    public GameObject memoryPanel;

    [Header("Interaction Rights")]
    public int maxInteractions = 2;       // Bir günde kullanılabilecek maksimum etkileşim sayısı
    public int remainingInteractions = 2; // Kalan günlük etkileşim hakkı

    // Performans ölçümü içindir — açıkken günlük etkileşim hakkı kontrol edilmez.
    // Varsayılan kapalı: normal oyun dengesine dokunmaz. F10 ile runtime'da açılıp kapanır,
    // Inspector'dan elle ayarlanması gerekmez (bkz. FrameTimeTracker ekranındaki durum yazısı).
    public bool unlimitedInteractionsForBenchmark = false;

    [Header("Warning")]
    public TextMeshProUGUI warningText;

    [Header("Event Notification")]
    public TextMeshProUGUI eventNotificationText;
    public GameObject eventNotificationPanel;
    public float eventNotificationDuration = 3f;

    private Coroutine eventNotificationCoroutine;
    private Queue<string> eventNotificationQueue = new Queue<string>(); // Bildirim kuyruğu (üst üste gelen bildirimleri sırayla göstermek için)
    private bool isDisplayingNotification = false;
    private GameObject currentNPC;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        interactionPanel.SetActive(false);
        memoryPanel.SetActive(true); // Hafıza paneli başlangıçta hep açık kalsın

        if (warningText != null)
            warningText.gameObject.SetActive(false);

        if (eventNotificationText != null)
        {
            eventNotificationText.text = "";
            eventNotificationText.gameObject.SetActive(false);
        }

        if (eventNotificationPanel != null)
            eventNotificationPanel.SetActive(false);
    }

    void Update()
    {
        // ESC tuşuna basıldığında oyunu devam ettirerek Ana Menüye döner.
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // NPC etkileşimi sırasında zaman durdurulduğu için (timeScale = 0) menüye dönerken zaman akışını normale çeker.
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }

        // Performans ölçüm modunu aç/kapat (madde: günlük etkileşim hakkı limiti ölçüm için kaldırılır).
        if (Input.GetKeyDown(KeyCode.F10))
        {
            unlimitedInteractionsForBenchmark = !unlimitedInteractionsForBenchmark;
            Debug.Log($"<color=magenta>Benchmark modu:</color> {(unlimitedInteractionsForBenchmark ? "AÇIK" : "KAPALI")}");
        }
    }

    public void ToggleMemoryPanel()
    {
        bool isActive = memoryPanel.activeSelf;
        memoryPanel.SetActive(!isActive);
    }

    // Günlük etkileşim haklarını yeniler.
    public void ResetInteractions()
    {
        remainingInteractions = maxInteractions;
        Debug.Log($"Interaction rights reset: {remainingInteractions}");
    }

    // NPC ile konuşma etkileşim menüsünü ekranda gösterir.
    public void ShowInteractionMenu(GameObject npc)
    {
        // Günlük hak kalmamışsa uyarı yazısı gösterir ve menüyü açmaz.
        if (!unlimitedInteractionsForBenchmark && remainingInteractions <= 0)
        {
            Debug.Log("No interactions left! Wait for the next day.");
            StartCoroutine(ShowWarning("No interactions left! Wait for the next day."));
            return;
        }

        currentNPC = npc;
        interactionPanel.SetActive(true);
        Time.timeScale = 0f; // Menü açıkken arka planda oyun zamanını durdurur.
        Debug.Log($"Menu opened: {npc.name} (Remaining: {remainingInteractions})");
    }

    // Ekranda geçici uyarı yazısı gösteren coroutine
    IEnumerator ShowWarning(string message)
    {
        if (warningText == null) yield break;

        warningText.text = message;
        warningText.gameObject.SetActive(true);

        // Oyun duraklatılmış olsa dahi gerçek saniye cinsinden bekler (Realtime)
        yield return new WaitForSecondsRealtime(4f);

        warningText.gameObject.SetActive(false);
    }

    public void CloseInteractionMenu()
    {
        interactionPanel.SetActive(false);
        Time.timeScale = 1f; // Menü kapandığında zaman akışını normale döndürür.
        currentNPC = null;
        Debug.Log("Menu closed");
    }

    // Etkileşim menüsündeki butonlara basıldığında (selamla, hediye ver vb.) tetiklenen fonksiyon.
    public void OnActionButton(string actionType)
    {
        if (currentNPC == null) return;

        NPCController npc = currentNPC.GetComponent<NPCController>();
        if (npc == null)
        {
            Debug.Log("ERROR: NPCController not found!");
            return;
        }

        // Görmezden gelme eylemi haricinde yapılan tüm etkileşimler hakkı 1 azaltır.
        if (actionType != "ignore" && !unlimitedInteractionsForBenchmark)
        {
            remainingInteractions--;
            Debug.Log($"Interaction used. Remaining: {remainingInteractions}");
        }

        float impact = 0f;
        List<string> tags = new List<string>();

        // Eylem türüne göre NPC üzerinde oluşacak duygusal etki katsayılarını ve etiketleri belirler.
        switch (actionType)
        {
            case "greet":
                impact = 0.3f;
                tags.Add("social");
                tags.Add("daytime");
                tags.Add("location_park");
                break;

            case "gift":
                impact = 0.6f;
                tags.Add("social");
                tags.Add("gift_item");
                tags.Add("daytime");
                break;

            case "help":
                impact = 0.5f;
                tags.Add("help");
                tags.Add("threat_nearby");
                tags.Add("night_patrol");
                break;

            case "ignore":
                impact = 0.0f;
                tags.Add("neutral");
                tags.Add("ignore");
                break;

            case "shout":
                impact = -0.4f;
                tags.Add("negative");
                tags.Add("noise");
                tags.Add("loud_noise");
                tags.Add("night_time");
                tags.Add("darkness");
                break;

            case "attack":
                impact = -0.7f;
                tags.Add("negative");
                tags.Add("threat_nearby");
                tags.Add("loud_noise");
                tags.Add("night_time");
                tags.Add("darkness");
                break;
        }

        // Eylemi NPC'nin hafızasına (anısına) ekler.
        npc.AddMemory(actionType, impact, tags);

        Debug.Log($"{npc.npcName} disposition: {npc.GetDispositionLabel()} ({npc.GetOverallDisposition():F2})");

        // Görev yöneticisini oyuncunun yaptığı eylemler hakkında bilgilendirir.
        if (QuestManager.Instance != null)
        {
            string npcShortName = npc.npcName.Contains("Ahmet") ? "Ahmet" :
                                  npc.npcName.Contains("Ayse") ? "Ayse" : "Mehmet";

            QuestManager.Instance.OnNPCVisited(npcShortName);

            switch (actionType)
            {
                case "greet":
                    QuestManager.Instance.OnNPCGreeted(npcShortName);
                    break;
                case "gift":
                    QuestManager.Instance.OnGiftGiven(npcShortName);
                    break;
                case "help":
                    QuestManager.Instance.OnHelpGiven(npcShortName);
                    break;
            }
        }

        // Kötü/zararlı bir eylem yapıldığında görev yöneticisine rapor eder.
        if (actionType == "shout" || actionType == "attack")
        {
            if (QuestManager.Instance != null)
                QuestManager.Instance.OnBadAction();
        }

        // Eylemin İngilizce açıklamasını yapay zeka girdisi olarak NPC'ye gönderir.
        string aiMessage = ActionToAIMessage(actionType);
        npc.InteractWithPlayer(aiMessage);

        CloseInteractionMenu();
    }

    // Eylem türünü yapay zekaya uygun İngilizce durum cümlesine çevirir.
    private string ActionToAIMessage(string actionType)
    {
        switch (actionType)
        {
            case "greet": return "The player greeted you.";
            case "gift": return "The player gave you a gift.";
            case "help": return "The player helped you.";
            case "ignore": return "The player ignored you.";
            case "shout": return "The player shouted at you.";
            case "attack": return "The player attacked you.";
            default: return "The player is talking to you.";
        }
    }

    // Ekranda beliren olay bildirimlerini (Örnek: "Mahallede büyük bir gürültü duyuldu!") sıraya ekleyen metot.
    public void ShowEventNotification(string message)
    {
        if (eventNotificationText == null)
        {
            Debug.LogWarning("EventNotificationText reference is missing in UIManager!");
            return;
        }

        eventNotificationQueue.Enqueue(message);

        // Eğer halihazırda gösterilen bir bildirim yoksa kuyruktan işlemeye başlar.
        if (!isDisplayingNotification)
        {
            eventNotificationCoroutine = StartCoroutine(ProcessNotificationQueue());
        }
    }

    // Bildirim kuyruğunu sırayla işleyen döngüsel coroutine.
    private IEnumerator ProcessNotificationQueue()
    {
        isDisplayingNotification = true;

        while (eventNotificationQueue.Count > 0)
        {
            string nextMessage = eventNotificationQueue.Dequeue();
            yield return StartCoroutine(FadeNotificationRoutine(nextMessage));
            yield return new WaitForSecondsRealtime(0.2f); // Bildirimler arası çok kısa bekleme süresi
        }

        isDisplayingNotification = false;
        eventNotificationCoroutine = null;
    }

    // Bildirimin ekranda yavaşça belirip (fade-in), bekleyip, yavaşça kaybolmasını (fade-out) sağlayan animasyon coroutine'i.
    private IEnumerator FadeNotificationRoutine(string message)
    {
        eventNotificationText.gameObject.SetActive(true);
        eventNotificationText.text = message;
        
        if (eventNotificationPanel != null)
            eventNotificationPanel.SetActive(true);

        Color originalColor = eventNotificationText.color;
        
        // Yavaşça Belirleme (Fade In - 0.5 saniye)
        float elapsed = 0f;
        float fadeDuration = 0.5f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime; // Menüde zaman dursa bile çalışması için unscaledDeltaTime kullanılır.
            float alpha = Mathf.Clamp01(elapsed / fadeDuration);
            eventNotificationText.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            yield return null;
        }
        eventNotificationText.color = new Color(originalColor.r, originalColor.g, originalColor.b, 1f);

        // Belirli süre ekranda sabit tut
        yield return new WaitForSecondsRealtime(eventNotificationDuration);

        // Yavaşça Kaybolma (Fade Out - 0.5 saniye)
        elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = Mathf.Clamp01(1f - (elapsed / fadeDuration));
            eventNotificationText.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            yield return null;
        }
        eventNotificationText.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0f);

        if (eventNotificationPanel != null)
            eventNotificationPanel.SetActive(false);

        eventNotificationText.text = "";
        eventNotificationText.gameObject.SetActive(false);
        eventNotificationText.color = originalColor;
    }
}