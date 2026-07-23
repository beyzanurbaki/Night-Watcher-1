using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

// Kare süresini (ms) sıfır-allocation halka tampona yazar; ölçüm penceresi
// içinde dosya I/O yapmaz. OllamaManager'ın bulunduğu objeye bağlanır
// (sahnede tekil garanti var olan obje odur) ve RequireComponent bunu zorunlu kılar.
// Aynı obje üzerinde yanlışlıkla ikinci bir kopya eklenirse Awake() kendini kapatır.
[RequireComponent(typeof(OllamaManager))]
public class FrameTimeTracker : MonoBehaviour
{
    private static FrameTimeTracker Instance;

    [Header("Frame Time Tracker Settings")]
    [SerializeField] private int bufferCapacity = 20000;
    [SerializeField] private int warmupFrames = 60; // sahne yüklendikten sonraki ilk N kare analiz dışı (madde 0)
    [SerializeField] private KeyCode flushKey = KeyCode.F9; // koşum bitince CSV'ye yaz
    [SerializeField] private OllamaManager ollamaManager;

    private float[] deltaBuffer;
    private int[] activeRequestBuffer;
    private int writeIndex = 0;
    private int frameCount = 0;
    private bool bufferFull = false;

    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogWarning("<color=orange>FrameTimeTracker:</color> zaten bir örnek var, bu kopya kapatılıyor.");
            Destroy(this);
            return;
        }
        Instance = this;

        deltaBuffer = new float[bufferCapacity];
        activeRequestBuffer = new int[bufferCapacity];

        if (ollamaManager == null)
            ollamaManager = GetComponent<OllamaManager>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(flushKey))
        {
            Flush();
            PerfLogger.Flush();
        }

        frameCount++;
        if (frameCount <= warmupFrames)
            return;

        if (writeIndex >= bufferCapacity)
        {
            bufferFull = true;
            return;
        }

        deltaBuffer[writeIndex] = Time.unscaledDeltaTime;
        activeRequestBuffer[writeIndex] = ollamaManager != null ? ollamaManager.ActiveRequestCount : 0;
        writeIndex++;
    }

    // Güvenlik ağı: oyunu çalıştıran kişi F9'a basmayı unutur/oyunu direkt kapatırsa
    // veriler kaybolmasın diye çıkışta otomatik kaydeder.
    private void OnApplicationQuit()
    {
        Flush();
        PerfLogger.Flush();
    }

    // Ekranda canlı ilerleme göstergesi: hangi npc/koşulda kaç istek atıldığı elle
    // sayılmasın diye koddan geliyor (bkz. PerfLogger.GetProgressSummary).
    private void OnGUI()
    {
        string summary = PerfLogger.GetProgressSummary();
        GUI.Box(new Rect(10, 10, 320, 20 + 18 * (summary.Split('\n').Length)), "");
        GUI.Label(new Rect(20, 15, 300, 300), $"Perf ölçüm ilerlemesi ({flushKey} = kaydet):\n{summary}");
    }

    // Ölçüm koşumu bitince manuel çağır (varsayılan: F9 tuşu). Tek seferde, tek dosya yazımı.
    public void Flush()
    {
        if (writeIndex == 0)
        {
            Debug.Log("<color=cyan>FrameTimeTracker:</color> tampon boş, yazılacak kare yok.");
            return;
        }
        if (bufferFull)
            Debug.LogWarning("<color=orange>FrameTimeTracker:</color> tampon doldu, bazı kareler kaydedilmedi — bufferCapacity artırılmalı.");

        string path = Path.Combine(Application.persistentDataPath,
            $"frametime_log_{DateTime.Now:yyyyMMdd_HHmmss}.csv");

        var sb = new StringBuilder();
        sb.AppendLine("frame_delta_ms,active_requests");
        for (int i = 0; i < writeIndex; i++)
        {
            sb.AppendLine(string.Join(",",
                (deltaBuffer[i] * 1000.0).ToString("F4", CultureInfo.InvariantCulture),
                activeRequestBuffer[i].ToString(CultureInfo.InvariantCulture)));
        }

        File.WriteAllText(path, sb.ToString());
        Debug.Log($"<color=cyan>FrameTimeTracker:</color> {writeIndex} kare yazıldı -> {path}");

        writeIndex = 0;
        bufferFull = false;
    }
}
