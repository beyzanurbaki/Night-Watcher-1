using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

// Performans ölçümlerini bellekte biriktirir, koşum bitince tek seferde CSV'ye yazar.
// Ölçüm penceresi içinde dosya I/O yapmaz (kendi ölçtüğü sivrilmeyi kendi yaratmasın diye).
public static class PerfLogger
{
    private struct Entry
    {
        public string timestamp;
        public string metric;
        public string npc;
        public double valueMs;
        public int promptTokens;
        public int evalTokens;
        public string condition;
    }

    private const int targetN = 30; // madde 0 protokolü: koşul başına en az 30 istek

    private static readonly List<Entry> buffer = new List<Entry>(1024);
    // npc|condition -> o ana kadar loglanan InferenceLatency (yani gerçek istek) sayısı.
    // Canlı ilerleme göstergesi için; koşumu manuel sayması gereken kişiye bırakmamak için.
    private static readonly Dictionary<string, int> requestCounts = new Dictionary<string, int>();

    public static void Log(string metric, string npc, double valueMs, int promptTokens = -1, int evalTokens = -1, string condition = "")
    {
        buffer.Add(new Entry
        {
            timestamp = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
            metric = metric,
            npc = npc,
            valueMs = valueMs,
            promptTokens = promptTokens,
            evalTokens = evalTokens,
            condition = condition
        });

        // Yalnızca gerçek isteği temsil eden satırı say (türetilmiş metrikleri değil,
        // yoksa aynı istek 5 kez sayılır).
        if (metric == "InferenceLatency")
        {
            string key = $"{npc}|{condition}";
            requestCounts.TryGetValue(key, out int current);
            requestCounts[key] = current + 1;
        }
    }

    // Ekranda göstermek için: her npc/koşul için "n/30" satırları. Oyunu çalıştıran kişi
    // manuel sayım tutmasın diye — sayaç koddan geliyor, elle takip gerektirmiyor.
    public static string GetProgressSummary()
    {
        if (requestCounts.Count == 0)
            return "Henüz istek yok.";

        var sb = new StringBuilder();
        foreach (var kv in requestCounts)
        {
            string label = kv.Key.Replace("|", " / ");
            string mark = kv.Value >= targetN ? "OK" : "..";
            sb.AppendLine($"[{mark}] {label}: {kv.Value}/{targetN}");
        }
        return sb.ToString();
    }

    // Ölçüm koşumu bitince manuel çağır (örn. bir debug tuşu veya OnApplicationQuit).
    public static void Flush()
    {
        if (buffer.Count == 0)
        {
            Debug.Log("<color=cyan>PerfLogger:</color> buffer boş, yazılacak kayıt yok.");
            return;
        }

        string path = Path.Combine(Application.persistentDataPath,
            $"perf_log_{DateTime.Now:yyyyMMdd_HHmmss}.csv");

        var sb = new StringBuilder();
        sb.AppendLine("timestamp,metric,npc,value_ms,prompt_tokens,eval_tokens,condition");
        foreach (var e in buffer)
        {
            sb.AppendLine(string.Join(",",
                e.timestamp,
                e.metric,
                e.npc,
                e.valueMs.ToString("F4", CultureInfo.InvariantCulture),
                e.promptTokens.ToString(CultureInfo.InvariantCulture),
                e.evalTokens.ToString(CultureInfo.InvariantCulture),
                e.condition));
        }

        File.WriteAllText(path, sb.ToString());
        Debug.Log($"<color=cyan>PerfLogger:</color> {buffer.Count} kayıt yazıldı -> {path}");
        buffer.Clear();
    }
}
