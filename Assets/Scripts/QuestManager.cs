using UnityEngine;
using TMPro;
using System.Collections.Generic;

// Oyundaki görevlerin tanımlanması, ilerleme durumlarının kontrolü,
// puan kazanma ve arayüzde güncel görev durumunun gösterilmesinden sorumlu yöneticidir.
public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance;

    [Header("Puan")]
    public int totalScore = 0;

    [Header("Gorev Durumu")]
    public bool[] questCompleted = new bool[7];      // 7 geceye ait tamamlanma durumları
    public string[] questDescriptions = new string[7]; // Görev açıklamaları
    public int[] questRewards = new int[7];           // Görevlerin puan ödülleri

    [Header("Gece Takibi")]
    public List<string> greetedNPCs = new List<string>();
    public List<string> visitedNPCs = new List<string>();
    public bool nightEventHandled = false;
    public bool didSomethingBad = false; // Oyuncunun bir NPC'ye bağırıp veya saldırdığını takip eder
    public bool ahmetGiftGiven = false;

    [Header("UI")]
    public TextMeshProUGUI questText;
    public TextMeshProUGUI scoreText;

    [Header("NPC Referanslari")]
    public NPCController ahmetNPC;
    public NPCController ayseNPC;
    public NPCController mehmetNPC;

    void Awake()
    {
        Instance = this;
        SetupQuests();
    }

    // Gece görevlerini (açıklama ve ödül puanı) ilklendiren yardımcı metot.
    void SetupQuests()
    {
        questDescriptions[0] = "En az 2 NPC'yi selamla";
        questDescriptions[1] = "Ahmet Abi'ye hediye ver ve baska birini selamla";
        questDescriptions[2] = "Bu gece görev yok.";
        questDescriptions[3] = "Ayse'ye veya Mehmet'e yardim et (birini sec)";
        questDescriptions[4] = "En dusuk tutumlu NPC'yi iyilestir";
        questDescriptions[5] = "Alarm olayina mudahale et ve kimseye kotu davranma";
        questDescriptions[6] = "Tum NPC'ler en az Sicak tutumda olsun";

        questRewards[0] = 15;
        questRewards[1] = 20;
        questRewards[2] = 0;
        questRewards[3] = 20;
        questRewards[4] = 15;
        questRewards[5] = 30;
        questRewards[6] = 50;
    }

    void Update()
    {
        UpdateQuestUI();
        CheckActiveQuest();
    }

    // Zaman Yöneticisindeki güncel güne/geceye göre aktif görevin gereksinimlerini kontrol eder.
    void CheckActiveQuest()
    {
        if (TimeManager.Instance == null) return;

        int night = TimeManager.Instance.currentDay;

        switch (night)
        {
            case 1: CheckQuest1(); break;
            case 2: CheckQuest2(); break;
            case 3: CheckQuest3(); break;
            case 4: CheckQuest4(); break;
            case 5: CheckQuest5(); break;
            case 6: CheckQuest6(); break;
            case 7: CheckQuest7(); break;
        }
    }

    // Gece 1: En az 2 NPC'yi selamla
    void CheckQuest1()
    {
        if (questCompleted[0]) return;
        if (greetedNPCs.Count >= 2)
        {
            CompleteQuest(0);
        }
    }

    // Gece 2: Ahmet'e hediye ver VE baska birini selamla
    void CheckQuest2()
    {
        if (questCompleted[1]) return;

        // Ahmet'e hediye verildi mi VE Ahmet dışında en az 1 NPC selamlandı mı?
        bool greetedSomeoneElse = greetedNPCs.Contains("Ayse") || greetedNPCs.Contains("Mehmet");

        if (ahmetGiftGiven && greetedSomeoneElse)
        {
            CompleteQuest(1);
        }
    }

    // Gece 3: Görev kaldırıldı
    void CheckQuest3()
    {
        if (!questCompleted[2])
        {
            CompleteQuest(2);
        }
    }

    // Gece 4: Ayse'ye VEYA Mehmet'e yardim et (UIManager üzerinden tetiklenir)
    void CheckQuest4()
    {
    }

    // Gece 5: En dusuk tutumlu NPC'yi iyilestir
    void CheckQuest5()
    {
        if (questCompleted[4]) return;

        NPCController weakest = GetWeakestNPC();
        if (weakest == null) return;

        string weakestShort = weakest.npcName.Contains("Ahmet") ? "Ahmet" :
                              weakest.npcName.Contains("Ayse") ? "Ayse" : "Mehmet";

        // Oyuncu en zayıf durumdaki NPC ile etkileşime girdi mi?
        if (greetedNPCs.Contains(weakestShort) || visitedNPCs.Contains(weakestShort))
        {
            // Eğer en zayıf durumdaki NPC'nin genel tutumu sıfıra yakın veya olumlu düzeye (-0.2'den büyük) yükseldiyse
            if (weakest.GetOverallDisposition() > -0.2f)
            {
                CompleteQuest(4);
            }
        }
    }

    // Gece 6: Alarm olayina mudahale et VE kimseye kotu davranma
    void CheckQuest6()
    {
        if (questCompleted[5]) return;
        if (nightEventHandled && !didSomethingBad)
        {
            CompleteQuest(5);
        }
    }

    // Gece 7: Tum NPC'ler en az Sicak tutumda olsun
    void CheckQuest7()
    {
        if (questCompleted[6]) return;
        if (ahmetNPC == null || ayseNPC == null || mehmetNPC == null) return;

        string ahmetTutum = ahmetNPC.GetDispositionLabel();
        string ayseTutum = ayseNPC.GetDispositionLabel();
        string mehmetTutum = mehmetNPC.GetDispositionLabel();

        bool ahmetOK = ahmetTutum == "Sicak" || ahmetTutum == "Dostca";
        bool ayseOK = ayseTutum == "Sicak" || ayseTutum == "Dostca";
        bool mehmetOK = mehmetTutum == "Sicak" || mehmetTutum == "Dostca";

        if (ahmetOK && ayseOK && mehmetOK)
        {
            CompleteQuest(6);
        }
    }

    // Tüm NPC'ler arasından oyuncuya karşı anlık tutum puanı en düşük olanı bulur.
    NPCController GetWeakestNPC()
    {
        NPCController weakest = ahmetNPC;
        float weakestScore = ahmetNPC.GetOverallDisposition();

        if (ayseNPC.GetOverallDisposition() < weakestScore)
        {
            weakest = ayseNPC;
            weakestScore = ayseNPC.GetOverallDisposition();
        }

        if (mehmetNPC.GetOverallDisposition() < weakestScore)
        {
            weakest = mehmetNPC;
        }

        return weakest;
    }

    // Görevi tamamlanmış olarak işaretler ve ödül puanını ekler.
    void CompleteQuest(int questIndex)
    {
        if (questCompleted[questIndex]) return;

        questCompleted[questIndex] = true;
        totalScore += questRewards[questIndex];

        Debug.Log($"GOREV TAMAMLANDI: {questDescriptions[questIndex]} (+{questRewards[questIndex]} puan)");
        Debug.Log($"Toplam Puan: {totalScore}");
    }

    // Oyuncu bir NPC'yi selamladığında çağrılır.
    public void OnNPCGreeted(string npcName)
    {
        if (!greetedNPCs.Contains(npcName))
        {
            greetedNPCs.Add(npcName);
        }
    }

    // Oyuncu bir NPC ile etkileşime girdiğinde çağrılır.
    public void OnNPCVisited(string npcName)
    {
        if (!visitedNPCs.Contains(npcName))
        {
            visitedNPCs.Add(npcName);
        }
    }

    // Oyuncu bir NPC'ye hediye verdiğinde çağrılır.
    public void OnGiftGiven(string npcName)
    {
        int night = TimeManager.Instance.currentDay;

        if (night == 2 && npcName == "Ahmet")
        {
            ahmetGiftGiven = true;
        }
    }

    // Oyuncu bir NPC'ye yardım ettiğinde çağrılır.
    public void OnHelpGiven(string npcName)
    {
        int night = TimeManager.Instance.currentDay;

        if (night == 4 && (npcName == "Ayse" || npcName == "Mehmet") && !questCompleted[3])
        {
            CompleteQuest(3);
        }
    }

    // Oyuncu bir NPC'ye saldırdığında veya bağırdığında tetiklenir.
    public void OnBadAction()
    {
        didSomethingBad = true;
    }

    // Gürültü olayı çözüldüğünde tetiklenir.
    public void OnNoiseEventHandled()
    {
        nightEventHandled = true;
    }

    // Alarm olayı çözüldüğünde tetiklenir.
    public void OnAlarmEventHandled()
    {
        nightEventHandled = true;
    }

    // Yeni gün başladığında geceye dair geçici etkileşim kayıtlarını sıfırlar.
    public void OnNewDay()
    {
        greetedNPCs.Clear();
        visitedNPCs.Clear();
        nightEventHandled = false;
        didSomethingBad = false;
        ahmetGiftGiven = false;
    }

    // Yeni gece başladığında durumları sıfırlar.
    public void OnNewNight()
    {
        greetedNPCs.Clear();
        visitedNPCs.Clear();
        nightEventHandled = false;
        didSomethingBad = false;
        ahmetGiftGiven = false;
    }

    // Görev ve Puan bilgilerini arayüzdeki TextMeshPro bileşenlerine aktarır.
    void UpdateQuestUI()
    {
        if (TimeManager.Instance == null) return;

        int night = TimeManager.Instance.currentDay;

        if (scoreText != null)
        {
            scoreText.text = $"Puan: {totalScore}";
        }

        if (questText != null)
        {
            if (night >= 1 && night <= 7)
            {
                int questIndex = night - 1;
                string status = questCompleted[questIndex] ? "(TAMAMLANDI)" : "(Devam Ediyor)";
                int kalan = UIManager.Instance != null ? UIManager.Instance.remainingInteractions : 0;
                questText.text = $"{night}. Gorev:\n{questDescriptions[questIndex]}\n{status}\nKalan Hak: {kalan}";
            }
        }
    }
}