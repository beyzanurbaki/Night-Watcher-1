using System;
using System.Collections.Generic;
using UnityEngine;

// NPC'lerin belleklerindeki tek bir anıyı temsil eden sınıftır.
// Ebbinghaus Unutma Eğrisi (forgetting curve) formülünü kullanarak anıların zamanla sönümlenmesini hesaplar.
[System.Serializable]
public class NPCMemory
{
    public string eventType;        // Olay türü (örneğin: loud_noise, gift_item vb.)
    public float emotionalImpact;   // Olayın karaktere olan anlık duygusal etkisi (-1.0 ile +1.0 arası)
    public float timestamp;         // Olayın gerçekleştiği zaman (saniye bazında)
    public float decayRate;         // Anının sönümlenme/unutulma hızı
    public List<string> tags;        // Olay etiketleri (social, negative vb.)

    public NPCMemory(string type, float impact, List<string> memoryTags = null)
    {
        eventType = type;
        emotionalImpact = Mathf.Clamp(impact, -1f, 1f);
        timestamp = Time.time;
        decayRate = 0.01f;
        tags = memoryTags ?? new List<string>();
    }

    // Anının anlık duygusal gücünü Ebbinghaus sönümleme formülü kullanarak hesaplar.
    public float GetStrength()
    {
        // Olaydan bu yana geçen toplam süre
        float elapsed = Time.time - timestamp;
        // Bellek kararlılığı (decayRate azaldıkça kararlılık artar)
        float stability = 1f / decayRate;
        // Üstel sönümleme formülü: e^(-t / S)
        float retention = Mathf.Exp(-elapsed / stability);
        
        return emotionalImpact * retention;
    }

    // Anının gücü çok zayıfladığında bellekten temizlenip temizlenmeyeceğini belirler.
    public bool ShouldBeRemoved()
    {
        return Mathf.Abs(GetStrength()) < 0.05f;
    }
}