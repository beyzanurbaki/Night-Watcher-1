using UnityEngine;
using TMPro;

// Oyundaki gün ve gece/gündüz durumunu ekrandaki TextMeshPro bileşeni aracılığıyla gösteren UI sınıfıdır.
public class DayDisplayUI : MonoBehaviour
{
    
    public TextMeshProUGUI dayText;

    void Update()
    {
        
        if (dayText == null || TimeManager.Instance == null) return;

        // Zaman yöneticisinden anlık gece/gündüz durumunu alır.
        string zaman = TimeManager.Instance.isNightActive ? "Gece" : "Gunduz";
        
       
        dayText.text = $"Gun: {TimeManager.Instance.currentDay} - {zaman}";
    }
}