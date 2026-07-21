using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(Rigidbody2D))]
// NPC'lerin fiziksel hareketlerini, yapay zeka model kurulumlarını (Ollama), 
// oyuncu ile olan diyalog etkileşimlerini ve anlık duygu durumlarını yöneten ana sınıftır.
public class NPCController : MonoBehaviour
{
    [Header("NPC Identity & AI")]
    public string npcName = "NPC";
    public OllamaManager ollamaManager;
    public DialogueManager dialogueManager;
    private string modelId;
    private bool modelReady = false;
    public bool isThinking { get; private set; } = false; // NPC'nin o an yapay zeka ile düşünüp düşünmediği durum

    [Header("AI Settings")]
    public float aiTemperature = 0.3f; // Modelin yaratıcılık/rastgelelik katsayısı

    [Header("Personality & Memory")]
    public Personality personality = new Personality();
    public List<NPCMemory> memories = new List<NPCMemory>();
    public int maxMemories = 50; // Karakterin aklında tutabileceği maksimum bellek sayısı

    [Header("Behavior & Movement")]
    public float moveSpeed = 2f;
    public float detectionRange = 5f; // Oyuncuyu algılama mesafesi

    [Header("AI Speech Settings")]
    public float speechCooldown = 8f; // İki konuşma arasındaki minimum bekleme süresi
    private float lastSpeechTime = -999f;
    
    [Header("Emotion UI")]
    public SpriteRenderer emotionIcon;
    public Sprite hostileSprite;
    public Sprite uneasySprite;
    public Sprite neutralSprite;
    public Sprite warmSprite;
    public Sprite friendlySprite;

    private Transform player;
    private Vector2 startPosition;
    private Rigidbody2D rb;
    private Animator anim;
    private Vector2 moveDirection;
    private Vector2 lastMoveDirection;

    private void Awake()
    {
        if (dialogueManager == null)
            dialogueManager = GetComponentInChildren<DialogueManager>(true);
    }

    // PlayerPrefs üzerinde kayıtlı olan OCEAN kişilik parametrelerini NPC adına göre yükler.
    private void LoadPersonalityFromPrefs()
    {
        // Türkçe karakterleri İngilizce karşılıklarıyla değiştirerek PlayerPrefs için güvenli anahtarlar oluşturur.
        string sanitizedName = npcName
            .Replace("ı", "i").Replace("İ", "I")
            .Replace("ş", "s").Replace("Ş", "S")
            .Replace("ğ", "g").Replace("Ğ", "G")
            .Replace("ü", "u").Replace("Ü", "U")
            .Replace("ö", "o").Replace("Ö", "O")
            .Replace("ç", "c").Replace("Ç", "C");

        string safeName = sanitizedName.Replace(" ", "_").Replace("-", "_");
        string prefix = $"NPC_{safeName}_";
        if (PlayerPrefs.HasKey(prefix + "openness"))
        {
            personality.openness = PlayerPrefs.GetFloat(prefix + "openness");
            personality.conscientiousness = PlayerPrefs.GetFloat(prefix + "conscientiousness");
            personality.extraversion = PlayerPrefs.GetFloat(prefix + "extraversion");
            personality.agreeableness = PlayerPrefs.GetFloat(prefix + "agreeableness");
            personality.neuroticism = PlayerPrefs.GetFloat(prefix + "neuroticism");
            Debug.Log($"<color=cyan>{npcName}</color> personality loaded from PlayerPrefs: O={personality.openness:F2}, C={personality.conscientiousness:F2}, E={personality.extraversion:F2}, A={personality.agreeableness:F2}, N={personality.neuroticism:F2}");
        }
    }

    private IEnumerator Start()
    {
        LoadPersonalityFromPrefs();

        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        startPosition = transform.position;

        GameObject playerObject = GameObject.Find("Player");
        if (playerObject != null)
            player = playerObject.transform;

        // Ollama sunucusu aktifse, bu karakter için özelleştirilmiş sistemi (System Prompt) kurarak modeli hazırlar.
        if (ollamaManager != null)
        {
            modelId = ollamaManager.SanitizeModelName(npcName);
            string systemPrompt = GenerateSystemPrompt();

            Debug.Log($"<color=cyan>{npcName}</color> brain is being prepared...");

            isThinking = true;
            bool createSuccess = false;
            // Model oluşturma işlemi asenkron olduğundan Coroutine ile beklenir.
            yield return StartCoroutine(
                ollamaManager.CreateNPCModel(modelId, systemPrompt, success => createSuccess = success)
            );
            isThinking = false;

            modelReady = createSuccess;

            if (modelReady)
                Debug.Log($"<color=cyan>{npcName}</color> brain is ready.");
            else
                Debug.LogError($"<color=red>{npcName}</color> brain could not be created.");
        }
    }

    private void Update()
    {
        UpdateEmotionDisplay();
        UpdateAnimation();
    }

    private void FixedUpdate()
    {
        UpdateBehavior();
    }

    private void UpdateAnimation()
    {
        if (anim != null)
        {
            anim.SetFloat("Horizontal", moveDirection.x);
            anim.SetFloat("Vertical", moveDirection.y);
            anim.SetFloat("Speed", moveDirection.sqrMagnitude);

            if (moveDirection.sqrMagnitude > 0.01f)
            {
                anim.SetFloat("LastHorizontal", moveDirection.x);
                anim.SetFloat("LastVertical", moveDirection.y);
            }
        }
    }

    #region AI Interaction
    // Oyuncunun etkileşimi sonrasında yapay zeka üzerinden karaktere uygun bir cevap üretilmesini sağlar.
    public void InteractWithPlayer(string playerMessage)
    {
        if (ollamaManager == null || string.IsNullOrEmpty(modelId) || !modelReady)
        {
            Debug.LogWarning($"{npcName}: Model is not ready!");
            return;
        }

        // Karakterin en güçlü anılarını alarak modele bağlam (context) olarak verir.
        string memoryContext = GetMemoryContextForAI();

        // Yapay zekaya gönderilecek son komut setini (Prompt) birleştirir.
        string finalPrompt =
            $"You are {npcName}.\n" +
            $"Your current mood: {GetDispositionLabel()}.\n" +
            $"Recent events: {memoryContext}.\n" +
            $"What happened: {playerMessage}\n" +
            $"Respond as {npcName} would. ONLY 1 sentence, max 8 words. No explanations.";

        Debug.Log($"<color=yellow>{npcName}</color> is thinking...");

        isThinking = true;
        // API çağrısını başlatır
        ollamaManager.SendMessageToNPC(modelId, finalPrompt, (reply) =>
        {
            isThinking = false;
            // Cevabı 8 kelime ile sınırlandırır
            string shortReply = LimitReplyByWords(reply, 5);

            // Üretilen cevabı diyalog balonunda gösterir
            if (dialogueManager != null)
                dialogueManager.ShowMessage(shortReply);

            Debug.Log($"<color=cyan>{npcName}</color>: {shortReply}");
        }, aiTemperature);
    }

    // Gelen cevabı kelime sınırına göre kırpan yardımcı fonksiyon
    private string LimitReplyByWords(string reply, int maxWords = 5)
    {
        if (string.IsNullOrWhiteSpace(reply))
            return "";

        string cleaned = reply.Replace("\n", " ").Trim();
        string[] words = cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (words.Length <= maxWords)
            return cleaned;

        return string.Join(" ", words, 0, maxWords).Trim();
    }

    private string GetMemoryContextForAI()
    {
        var strongMemories = memories
            .Where(m => Mathf.Abs(m.GetStrength()) > 0.1f)
            .OrderByDescending(m => Mathf.Abs(m.GetStrength()))
            .Take(3)
            .Select(m => m.eventType);

        return strongMemories.Any() ? string.Join(", ", strongMemories) : "No significant memories.";
    }

    // Karakterin adına ve OCEAN kişilik parametrelerine bağlı olarak başlangıç System Prompt'unu oluşturur.
    private string GenerateSystemPrompt()  
    {
        string cleanName = npcName.Replace("-", " ").Replace("_", " ");

        string basePrompt = "";

        // Ayşe Teyze: Sıcakkanlı ve neşeli
        if (cleanName.Contains("Ayse"))
        {
            basePrompt = "You are Aunt Ayse, a warm and cheerful old woman. " +
                         "You love chatting and always speak kindly. " +
                         "You call people dear. " +
                         "Example replies: 'So sweet, dear.', 'Bless you, dear.', 'How lovely, dear.'";
        }
        // Ahmet Amca: Huysuz ve şüpheci
        else if (cleanName.Contains("Ahmet"))
        {
            basePrompt = "You are Uncle Ahmet, a grumpy and suspicious old man. " +
                         "You dislike noise and trust people slowly. " +
                         "You sound annoyed and blunt. " +
                         "Example replies: 'Leave me alone.', 'What now?', 'Go away.'";
        }
        // Mehmet Amca: Ciddi ve sakin
        else if (cleanName.Contains("Mehmet"))
        {
            basePrompt = "You are Uncle Mehmet, a calm and serious old man. " +
                         "You are polite, careful, and formal. " +
                         "You sound measured and reserved. " +
                         "Example replies: 'Thank you kindly.', 'I appreciate this.', 'Very well then.'";
        }
        else
        {
            basePrompt = $"You are {cleanName}.";
        }

        // OCEAN değerlerini içeren prompt eklemesi
        string personalityPrompt =
            $"\nYour personality is defined by these OCEAN traits (scale 0.0 to 1.0):\n" +
            $"- Openness: {personality.openness:F2}\n" +
            $"- Conscientiousness: {personality.conscientiousness:F2}\n" +
            $"- Extraversion: {personality.extraversion:F2}\n" +
            $"- Agreeableness: {personality.agreeableness:F2}\n" +
            $"- Neuroticism: {personality.neuroticism:F2}\n" +
            $"Adjust your tone and replies to reflect these traits (e.g., higher Agreeableness makes you nicer, higher Neuroticism makes you more anxious or grumpier).";

        string rules = "\nRules: Reply only in English. One short sentence. Maximum 5 words. React to events (like noises, park sounds, or darkness) naturally based on your personality, do not blindly repeat template greetings.";

        return basePrompt + personalityPrompt + rules;
    }
    #endregion

    #region Trigger System
    // Çevresel olaylar tetiklendiğinde karaktere etki uygulayan ve hafızaya anı ekleyen metot.
    public void ActivateTrigger(string triggerType, float impact = 0f, List<string> tags = null)
    {
        if (impact == 0f)
            impact = GetTriggerImpact(triggerType);

        if (tags == null)
            tags = new List<string> { triggerType };

        // Belleğe yeni olayı ekler
        AddMemory(triggerType, impact, tags);

        if (memories.Count > 0)
            StartCoroutine(TemporaryBoostMemory(memories[memories.Count - 1], 1f, 5f));

        Debug.Log($"{npcName} received trigger: {triggerType} ({impact:F2})");

        // Konuşma bekleme süresi (cooldown) kontrolüyle sözel tepki verilip verilmeyeceğini belirler.
        if (Time.time - lastSpeechTime >= speechCooldown)
        {
            if (ShouldReactVerbally(impact))
            {
                lastSpeechTime = Time.time + 2.0f; // Eşzamanlı spam tetiklemeleri önlemek için geçici tampon
                string triggerMessage = TriggerToAIMessage(triggerType);
                StartCoroutine(StaggeredSpeechRoutine(triggerMessage));
            }
        }
    }

    // Sözel tepki kapısı: Mehmet (Sorumluluk/Conscientiousness eksenli, deterministik eşik) için
    // yalnızca duygusal etki (ΔE) kişiliğe bağlı eşiği (θ) aşarsa konuşur; diğer NPC'ler stokastik (%40) tepki verir.
    private bool ShouldReactVerbally(float impact)
    {
        if (npcName.Contains("Mehmet"))
        {
            float theta = 0.1f + personality.conscientiousness * 0.15f;
            return Mathf.Abs(impact) >= theta;
        }

        return UnityEngine.Random.value < 0.4f;
    }

    // Karakterlerin tepkilerinin üst üste binmemesi için rastgele bir gecikmeyle diyalog başlatır.
    private IEnumerator StaggeredSpeechRoutine(string triggerMessage)
    {
        // Doğallık katmak için 0.3 ile 1.8 saniye arası bekler
        float delay = UnityEngine.Random.Range(0.3f, 1.8f);
        yield return new WaitForSeconds(delay);

        lastSpeechTime = Time.time;
        InteractWithPlayer(triggerMessage);
    }

    private string TriggerToAIMessage(string triggerType)
    {
        string t = triggerType.ToLower();

        if (t.Contains("night_time")) return "It just became night.";
        if (t.Contains("darkness")) return "It is very dark now.";
        if (t.Contains("night_patrol")) return "The patrol has started.";
        if (t.Contains("loud_noise")) return "A loud noise happened.";
        if (t.Contains("threat")) return "There is danger nearby.";
        if (t.Contains("safe")) return "Things feel safe now.";
        if (t.Contains("rain")) return "It started raining.";
        if (t.Contains("morning")) return "Morning has arrived.";
        if (t.Contains("park")) return "Sounds are coming from the park.";
        if (t.Contains("quiet_night")) return "The night is very quiet.";

        return $"Something happened: {triggerType}.";
    }

    private float GetTriggerImpact(string triggerType)
    {
        string t = triggerType.ToLower();

        if (t.Contains("attack") || t.Contains("threat") || t.Contains("noise") || t.Contains("dark"))
            return -0.25f;

        if (t.Contains("gift") || t.Contains("help") || t.Contains("social") || t.Contains("safe"))
            return 0.25f;

        return 0.10f;
    }
    #endregion

    #region Movement & Memory
    // NPC'nin oyuncuyla arasındaki mesafeye ve anlık tutumuna göre (Hostile, Friendly vb.) hareketini belirler.
    void UpdateBehavior()
    {
        if (player == null || rb == null) return;

        float distance = Vector2.Distance(transform.position, player.position);

        // Algılama menzilinde ise tutuma göre davranış sergiler
        if (distance < detectionRange)
        {
            string disposition = GetDispositionLabel();

            switch (disposition)
            {
                case "Friendly":
                    MoveTowards(player.position, moveSpeed); // Dostça: Oyuncuya doğru hızlı yürü
                    break;
                case "Warm":
                    MoveTowards(player.position, moveSpeed * 0.5f); // Sıcak: Oyuncuya yavaşça yaklaş
                    break;
                case "Neutral":
                    StopMovement(); // Nötr: Hareketsiz kal
                    break;
                case "Uneasy":
                    MoveAway(player.position, moveSpeed * 0.5f); // Tedirgin: Oyuncudan yavaşça uzaklaş
                    break;
                case "Hostile":
                    MoveAway(player.position, moveSpeed); // Düşmanca: Oyuncudan hızlıca kaç
                    break;
            }
        }
        // Oyuncu menzilden çıktığında eski başlangıç noktasına geri döner
        else if (Vector2.Distance(transform.position, startPosition) > 0.1f)
        {
            MoveTowards(startPosition, moveSpeed * 0.3f);
        }
        else
        {
            StopMovement();
        }
    }

    void StopMovement()
    {
        rb.linearVelocity = Vector2.zero;
        moveDirection = Vector2.zero;
    }

    void MoveTowards(Vector2 target, float speed)
    {
        moveDirection = (target - (Vector2)transform.position).normalized;
        rb.MovePosition(rb.position + moveDirection * speed * Time.fixedDeltaTime);
    }

    void MoveAway(Vector2 target, float speed)
    {
        moveDirection = ((Vector2)transform.position - target).normalized;
        rb.MovePosition(rb.position + moveDirection * speed * Time.fixedDeltaTime);
    }

    public void AddMemory(string eventType, float impact, List<string> tags = null)
    {
        NPCMemory newMemory = new NPCMemory(eventType, impact, tags);
        newMemory.decayRate = personality.GetDecayRate(impact);

        memories.Add(newMemory);

        if (memories.Count > maxMemories)
            memories.RemoveAt(0);
    }

    // Karakterin belleğindeki anıları sıfırlayarak temel mizaç ayarlarına dönmesini sağlar.
    public void ClearMemories()
    {
        memories.Clear();
        UpdateEmotionDisplay();
    }

    // Karakterin temel mizaç tutumunu (Agreeableness ve Neuroticism) ve anılarının toplamını hesaba katarak genel tutum puanını hesaplar.
    public float GetOverallDisposition()
    {
        // Temel mizaç puanı: Uzlaşmacılık (Agreeableness) arttıkça olumluya, Nevrotiklik (Neuroticism) arttıkça olumsuza eğilim gösterir.
        float baseDisp = (personality.agreeableness - 0.5f) * 1.0f - (personality.neuroticism - 0.5f) * 0.4f;
        float memorySum = memories.Sum(m => m.GetStrength());
        return Mathf.Clamp(baseDisp + memorySum, -1.0f, 1.0f);
    }

    // Toplam tutum puanına göre karaktere uygun ruh hali etiketini döndürür.
    public string GetDispositionLabel()
    {
        float disp = GetOverallDisposition();

        if (disp < -0.5f) return "Hostile";
        if (disp < -0.2f) return "Uneasy";
        if (disp > 0.5f) return "Friendly";
        if (disp > 0.2f) return "Warm";
        return "Neutral";
    }

    // Karaktere bağlı SpriteRenderer üzerindeki duygu durum ikonunu günceller.
    void UpdateEmotionDisplay()
    {
        if (emotionIcon == null) return;

        string disposition = GetDispositionLabel();

      
        emotionIcon.color = Color.white;

        switch (disposition)
        {
            case "Hostile":
                if (hostileSprite != null) emotionIcon.sprite = hostileSprite;
                break;
                
            case "Uneasy":
                if (uneasySprite != null) emotionIcon.sprite = uneasySprite;
                break;
                
            case "Friendly":
                if (friendlySprite != null) emotionIcon.sprite = friendlySprite;
                break;
                
            case "Warm":
                if (warmSprite != null) emotionIcon.sprite = warmSprite;
                break;
                
            case "Neutral":
            default:
                if (neutralSprite != null) emotionIcon.sprite = neutralSprite;
                break;
        }
    }

    IEnumerator TemporaryBoostMemory(NPCMemory memory, float boostAmount, float duration)
    {
        float original = memory.emotionalImpact;
        memory.emotionalImpact *= 2f;

        UpdateEmotionDisplay();

        yield return new WaitForSeconds(duration);

        memory.emotionalImpact = original;
        UpdateEmotionDisplay();
    }
    #endregion
}