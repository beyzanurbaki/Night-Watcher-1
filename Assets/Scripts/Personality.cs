using System;
using UnityEngine;

// Karakterin Big Five (OCEAN) kişilik modelini temsil eden sınıftır.
// Bu kişilik özellikleri karakterin diyalog cevaplarını ve hafıza sönümlenme hızını etkiler.
[System.Serializable]
public class Personality
{
    [Header("Big Five (OCEAN) - 0.0 ile 1.0 arasi")]

    [Tooltip("Deneyime Aciklik")]
    [Range(0, 1)] public float openness = 0.5f;

    [Tooltip("Sorumluluk")]
    [Range(0, 1)] public float conscientiousness = 0.5f;

    [Tooltip("Disadonukluk")]
    [Range(0, 1)] public float extraversion = 0.5f;

    [Tooltip("Uyumluluk")]
    [Range(0, 1)] public float agreeableness = 0.5f;

    [Tooltip("Nevrotiklik")]
    [Range(0, 1)] public float neuroticism = 0.5f;

    // Karakterin nevrotiklik (duygusal dengesizlik) düzeyine göre anının hafızada sönümlenme hızını hesaplar.
    public float GetDecayRate(float emotionalImpact)
    {
        float baseDecay = 0.05f;

        // Eğer olay olumsuz bir etkiye sahipse (emotionalImpact < 0)
        if (emotionalImpact < 0)
        {
            // Nevrotikliği yüksek olan karakterler olumsuz anıları daha zor unutur (sönümlenme hızı düşer).
            return baseDecay * (1f - neuroticism * 0.8f);
        }
        // Eğer olay olumlu bir etkiye sahipse
        else
        {
            // Nevrotikliği yüksek olan karakterler olumlu anıları daha hızlı unutur (sönümlenme hızı artar).
            return baseDecay * (1f + neuroticism * 0.3f);
        }
    }
}