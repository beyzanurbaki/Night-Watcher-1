using UnityEngine;
using TMPro;
using System.Collections;

// Karakterlerin (NPC/Oyuncu) başının üstünde çıkan diyalog balonunu ve metnini belirli bir süre ekranda göstermekle sorumlu yöneticidir.
public class DialogueManager : MonoBehaviour
{
    // Diyalog metnini yazdıran TMP text bileşeni
    public TextMeshProUGUI dialogueText;
    
    // Diyalog balonunun arka plan görsel paneli
    public GameObject bubbleBackground;
    
    // Diyaloğun ekranda kalma süresi (saniye cinsinden)
    public float showDuration = 5f;

    // Aynı anda birden fazla diyalog çalışmaması için anlık diyalog coroutine referansı
    private Coroutine currentRoutine;

    void Awake()
    {
        // Başlangıçta diyalog balonunu gizle ve metni sıfırla
        if (bubbleBackground != null)
            bubbleBackground.SetActive(false);

        if (dialogueText != null)
            dialogueText.text = "";
    }

    // Dışarıdan bir mesaj gönderildiğinde bu fonksiyon çağrılır ve mesajı ekranda göstermeye başlar.
    public void ShowMessage(string message)
    {
        if (string.IsNullOrEmpty(message)) return;

        Debug.Log("Text to display: " + message);

        // Eğer halihazırda gösterilen bir diyalog varsa, eski coroutine'i durdurur.
        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        // Yeni mesajı göstermek için coroutine'i başlatır.
        currentRoutine = StartCoroutine(DisplayRoutine(message));
    }

    // Diyalog balonunun açılması, beklemesi ve kapanmasını yöneten Coroutine yapısı
    private IEnumerator DisplayRoutine(string message)
    {
        // Diyalog balonunu görünür yap
        if (bubbleBackground != null)
            bubbleBackground.SetActive(true);

        // Diyalog metnini güncelle
        if (dialogueText != null)
            dialogueText.text = message;

        // Oyun duraklatılmış olsa bile gerçek zamanlı olarak belirtilen süre kadar bekle (Realtime kullanılır)
        yield return new WaitForSecondsRealtime(showDuration);

        // Bekleme süresi bitince balonu gizle ve metni temizle
        if (bubbleBackground != null)
            bubbleBackground.SetActive(false);

        if (dialogueText != null)
            dialogueText.text = "";

        // Coroutine referansını sıfırla
        currentRoutine = null;
    }
}