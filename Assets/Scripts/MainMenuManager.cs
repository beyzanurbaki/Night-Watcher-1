using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.Networking;

// Ana menü ekranındaki tüm arayüz bileşenlerini, buton etkileşimlerini, yapay zeka bağlantı durumunu,
// OCEAN mizaç editörünü ve retro arayüz animasyonlarını kontrol eden merkezi yöneticidir.
public class MainMenuManager : MonoBehaviour
{
    [Header("Retro Terminal Assets (Bakes)")]
    public TMP_FontAsset fontAsset;
    public Sprite ahmetSprite;
    public Sprite ayseSprite;
    public Sprite mehmetSprite;

    [Header("Serialized UI References")]
    public TextMeshProUGUI ollamaStatusText;
    public Image ollamaLed;
    public TextMeshProUGUI ahmetLabel;
    public TextMeshProUGUI ayseLabel;
    public TextMeshProUGUI mehmetLabel;
    public RectTransform neuronContainer;
    public GameObject cognitiveSettingsPanel;

    [Header("OCEAN Sliders")]
    public Slider ahmetO, ahmetC, ahmetE, ahmetA, ahmetN;
    public Slider ayseO, ayseC, ayseE, ayseA, ayseN;
    public Slider mehmetO, mehmetC, mehmetE, mehmetA, mehmetN;

    [Header("Colors")]
    public Color terminalGreen = new Color(0.2f, 1f, 0.2f, 1f);
    public Color terminalCyan = new Color(0f, 1f, 1f, 1f);

    // Arka plandaki şeffaf sinir ağı (Node) simülasyon sınıfı
    private class NeuronNode
    {
        public RectTransform rect;
        public Vector2 velocity;
    }

    private List<NeuronNode> neuronNodes = new List<NeuronNode>();
    private List<RectTransform> activeLines = new List<RectTransform>();
    private List<RectTransform> linePool = new List<RectTransform>();
    private int maxNodes = 25;
    private float maxLineDistance = 160f;

    // Bilişsel profil editöründeki geçici OCEAN değerleri
    private float tAhmetO, tAhmetC, tAhmetE, tAhmetA, tAhmetN;
    private float tAyseO, tAyseC, tAyseE, tAyseA, tAyseN;
    private float tMehmetO, tMehmetC, tMehmetE, tMehmetA, tMehmetN;

    private void Start()
    {
        // FPS sınırını 60'a sabitleyerek kararlı çalışma sağlar
        Application.targetFrameRate = 60;

        // Karakterlerin OCEAN değerleri kayıtlı değilse varsayılanları yükler
        InitializeDefaultTraits();

        // Butonların tıklama ve üzerine gelme (hover) durumlarını bağlar
        BindUIControls();

        // Arka plandaki sinir ağı düğüm yapısını kurar
        SetupNeuralNetwork();

        // Başlangıçta kişilik ayarları panelini gizli tutar
        if (cognitiveSettingsPanel != null)
        {
            cognitiveSettingsPanel.SetActive(false);
        }

        // Karakter mizaç etiketlerini (Huysuz, Sosyal vb.) arayüzde günceller
        UpdatePersonalityLabels();

        // Ollama yapay zeka sunucu bağlantısını kontrol eden döngüyü başlatır
        StartCoroutine(CheckOllamaStatusLoop());
    }

    private void Update()
    {
        // Sinir ağının düğüm hareketlerini ve bağlantı çizgilerini günceller
        UpdateNeuralNetwork();
        // Ollama durum göstergesi LED'ini yanıp söner hale getirir
        AnimateOllamaLed();
    }

    #region Default Traits Initializer
    private void InitializeDefaultTraits()
    {
        // Ahmet Amca
        if (!PlayerPrefs.HasKey("NPC_Ahmet_Amca_openness"))
        {
            PlayerPrefs.SetFloat("NPC_Ahmet_Amca_openness", 0.3f);
            PlayerPrefs.SetFloat("NPC_Ahmet_Amca_conscientiousness", 0.7f);
            PlayerPrefs.SetFloat("NPC_Ahmet_Amca_extraversion", 0.2f);
            PlayerPrefs.SetFloat("NPC_Ahmet_Amca_agreeableness", 0.4f);
            PlayerPrefs.SetFloat("NPC_Ahmet_Amca_neuroticism", 0.8f);
        }

        // Ayse Teyze
        if (!PlayerPrefs.HasKey("NPC_Ayse_Teyze_openness"))
        {
            PlayerPrefs.SetFloat("NPC_Ayse_Teyze_openness", 0.7f);
            PlayerPrefs.SetFloat("NPC_Ayse_Teyze_conscientiousness", 0.5f);
            PlayerPrefs.SetFloat("NPC_Ayse_Teyze_extraversion", 0.8f);
            PlayerPrefs.SetFloat("NPC_Ayse_Teyze_agreeableness", 0.9f);
            PlayerPrefs.SetFloat("NPC_Ayse_Teyze_neuroticism", 0.2f);
        }

        // Mehmet Amca
        if (!PlayerPrefs.HasKey("NPC_Mehmet_Amca_openness"))
        {
            PlayerPrefs.SetFloat("NPC_Mehmet_Amca_openness", 0.5f);
            PlayerPrefs.SetFloat("NPC_Mehmet_Amca_conscientiousness", 0.8f);
            PlayerPrefs.SetFloat("NPC_Mehmet_Amca_extraversion", 0.5f);
            PlayerPrefs.SetFloat("NPC_Mehmet_Amca_agreeableness", 0.5f);
            PlayerPrefs.SetFloat("NPC_Mehmet_Amca_neuroticism", 0.3f);
        }
        PlayerPrefs.Save();
    }
    #endregion

    #region Bind Baked Controls
    private void BindUIControls()
    {
        GameObject rootGo = GameObject.Find("CyberpunkUI_Root");
        if (rootGo == null)
        {
            Debug.LogError("MainMenuManager: CyberpunkUI_Root sahne içinde bulunamadı! Butonlar bağlanamadı.");
            return;
        }

        Transform root = rootGo.transform;

        // Bind Main Menu Buttons
        BindButton(root, "MiddleContainer/LeftPanel_Menu/ButtonContainer/Btn_INITIALIZE_SIMULATION", () => StartGame());
        BindButton(root, "MiddleContainer/LeftPanel_Menu/ButtonContainer/Btn_COGNITIVE_PROFILES", () => OpenSettings());
        BindButton(root, "MiddleContainer/LeftPanel_Menu/ButtonContainer/Btn_SHUTDOWN", () => QuitGame());

        // Bind Settings Overlay Controls (if assigned)
        BindSettingsButton(root, "CognitiveSettingsOverlay/FramePanel/Controls/Btn_Save", () => {
            SaveSettingsValues();
            UpdatePersonalityLabels();
            CloseSettings();
        }, terminalGreen);

        BindSettingsButton(root, "CognitiveSettingsOverlay/FramePanel/Controls/Btn_Reset", () => {
            ResetSettingsToDefault();
            LoadSettingsValues();
        }, terminalCyan);

        BindSettingsButton(root, "CognitiveSettingsOverlay/FramePanel/Controls/Btn_Cancel", () => {
            CloseSettings();
        }, Color.white);

        // Hook up slider label update listeners
        HookSliderLabel(ahmetO, "Deneyime Açıklık");
        HookSliderLabel(ahmetC, "Sorumluluk");
        HookSliderLabel(ahmetE, "Dışadönüklük");
        HookSliderLabel(ahmetA, "Uyumluluk");
        HookSliderLabel(ahmetN, "Nevrotiklik");

        HookSliderLabel(ayseO, "Deneyime Açıklık");
        HookSliderLabel(ayseC, "Sorumluluk");
        HookSliderLabel(ayseE, "Dışadönüklük");
        HookSliderLabel(ayseA, "Uyumluluk");
        HookSliderLabel(ayseN, "Nevrotiklik");

        HookSliderLabel(mehmetO, "Deneyime Açıklık");
        HookSliderLabel(mehmetC, "Sorumluluk");
        HookSliderLabel(mehmetE, "Dışadönüklük");
        HookSliderLabel(mehmetA, "Uyumluluk");
        HookSliderLabel(mehmetN, "Nevrotiklik");
    }

    private void BindButton(Transform root, string relativePath, Action onClickAction)
    {
        Transform btnTrans = root.Find(relativePath);
        if (btnTrans == null)
        {
            Debug.LogWarning($"MainMenuManager: '{relativePath}' konumunda buton bulunamadı!");
            return;
        }

        Button btn = btnTrans.GetComponent<Button>();
        if (btn == null) return;

        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => {
            PlaySynthBeep(440f, 0.1f);
            onClickAction?.Invoke();
        });

        // Hover events
        Transform glow = btnTrans.Find("Glow");
        Transform arrow = btnTrans.Find("Arrow");
        TextMeshProUGUI txt = btnTrans.Find("Text")?.GetComponent<TextMeshProUGUI>();

        EventTrigger trigger = btnTrans.GetComponent<EventTrigger>();
        if (trigger == null) trigger = btnTrans.gameObject.AddComponent<EventTrigger>();
        trigger.triggers.Clear();

        EventTrigger.Entry entryEnter = new EventTrigger.Entry();
        entryEnter.eventID = EventTriggerType.PointerEnter;
        entryEnter.callback.AddListener((data) => {
            if (glow != null) glow.gameObject.SetActive(true);
            if (arrow != null) arrow.gameObject.SetActive(true);
            if (txt != null) txt.color = terminalGreen;
            PlaySynthBeep(880f, 0.02f);
        });
        trigger.triggers.Add(entryEnter);

        EventTrigger.Entry entryExit = new EventTrigger.Entry();
        entryExit.eventID = EventTriggerType.PointerExit;
        entryExit.callback.AddListener((data) => {
            if (glow != null) glow.gameObject.SetActive(false);
            if (arrow != null) arrow.gameObject.SetActive(false);
            if (txt != null) txt.color = Color.white;
        });
        trigger.triggers.Add(entryExit);
    }

    private void BindSettingsButton(Transform root, string relativePath, Action onClickAction, Color highlightColor)
    {
        Transform btnTrans = root.Find(relativePath);
        if (btnTrans == null)
        {
            Debug.LogWarning($"MainMenuManager: '{relativePath}' konumunda ayar butonu bulunamadı!");
            return;
        }

        Button btn = btnTrans.GetComponent<Button>();
        if (btn == null) return;

        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => {
            PlaySynthBeep(520f, 0.1f);
            onClickAction?.Invoke();
        });

        Image img = btnTrans.GetComponent<Image>();

        EventTrigger trigger = btnTrans.GetComponent<EventTrigger>();
        if (trigger == null) trigger = btnTrans.gameObject.AddComponent<EventTrigger>();
        trigger.triggers.Clear();

        EventTrigger.Entry enter = new EventTrigger.Entry();
        enter.eventID = EventTriggerType.PointerEnter;
        enter.callback.AddListener((data) => {
            if (img != null) img.color = new Color(highlightColor.r, highlightColor.g, highlightColor.b, 0.28f);
            PlaySynthBeep(880f, 0.02f);
        });
        trigger.triggers.Add(enter);

        EventTrigger.Entry exit = new EventTrigger.Entry();
        exit.eventID = EventTriggerType.PointerExit;
        exit.callback.AddListener((data) => {
            if (img != null) img.color = new Color(highlightColor.r, highlightColor.g, highlightColor.b, 0.12f);
        });
        trigger.triggers.Add(exit);
    }

    private void HookSliderLabel(Slider slider, string labelName)
    {
        if (slider == null) return;

        // Label is usually a sibling text or inside container
        TextMeshProUGUI labelTxt = slider.transform.parent.Find("Label")?.GetComponent<TextMeshProUGUI>();
        if (labelTxt == null) return;

        slider.onValueChanged.RemoveAllListeners();
        slider.onValueChanged.AddListener((val) => {
            labelTxt.text = $"{labelName}: {val:F2}";
        });
    }
    #endregion

    #region Neural Network Simulation
    // Arka plandaki cyberpunk tarzı hareketli sinir ağı parçacıklarını oluşturur.
    private void SetupNeuralNetwork()
    {
        if (neuronContainer == null) return;

        // Önceden oluşturulmuş eski çocuk nesneleri temizler
        foreach (Transform child in neuronContainer)
        {
            Destroy(child.gameObject);
        }
        neuronNodes.Clear();
        activeLines.Clear();
        linePool.Clear();

        // Düğüm noktalarını rastgele pozisyon ve hız değerleriyle sahneye yerleştirir
        for (int i = 0; i < maxNodes; i++)
        {
            GameObject nodeGo = new GameObject("Node_" + i);
            nodeGo.transform.SetParent(neuronContainer, false);
            RectTransform rt = nodeGo.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(6f, 6f);
            
            rt.anchoredPosition = new Vector2(
                UnityEngine.Random.Range(-900f, 900f),
                UnityEngine.Random.Range(-500f, 500f)
            );

            Image img = nodeGo.AddComponent<Image>();
            img.color = new Color(0f, 0.8f, 0.8f, 0.3f); // Yarı saydam Cyan rengi

            NeuronNode node = new NeuronNode
            {
                rect = rt,
                velocity = new Vector2(
                    UnityEngine.Random.Range(-25f, 25f),
                    UnityEngine.Random.Range(-25f, 25f)
                )
            };
            neuronNodes.Add(node);
        }
    }

    // Düğümleri hareket ettirir, ekran dışına çıkanları sarar ve birbirine yakın olan düğümler arasına çizgi çizer.
    private void UpdateNeuralNetwork()
    {
        if (neuronContainer == null) return;

        float dt = Time.deltaTime;
        float xMax = neuronContainer.rect.width / 2f;
        float yMax = neuronContainer.rect.height / 2f;

        // Animate nodes
        for (int i = 0; i < neuronNodes.Count; i++)
        {
            NeuronNode node = neuronNodes[i];
            Vector2 pos = node.rect.anchoredPosition;
            pos += node.velocity * dt;

            if (pos.x > xMax + 20f) pos.x = -xMax - 20f;
            else if (pos.x < -xMax - 20f) pos.x = xMax + 20f;

            if (pos.y > yMax + 20f) pos.y = -yMax - 20f;
            else if (pos.y < -yMax - 20f) pos.y = yMax + 20f;

            node.rect.anchoredPosition = pos;
        }

        // Return active lines to pool
        for (int i = 0; i < activeLines.Count; i++)
        {
            activeLines[i].gameObject.SetActive(false);
            linePool.Add(activeLines[i]);
        }
        activeLines.Clear();

        // Connecting lines
        for (int i = 0; i < neuronNodes.Count; i++)
        {
            for (int j = i + 1; j < neuronNodes.Count; j++)
            {
                Vector2 pA = neuronNodes[i].rect.anchoredPosition;
                Vector2 pB = neuronNodes[j].rect.anchoredPosition;
                float d = Vector2.Distance(pA, pB);

                if (d < maxLineDistance)
                {
                    RectTransform lineRt = GetLineFromPool();
                    lineRt.gameObject.SetActive(true);

                    lineRt.anchoredPosition = (pA + pB) * 0.5f;
                    
                    Vector2 delta = pB - pA;
                    float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
                    lineRt.localRotation = Quaternion.Euler(0f, 0f, angle);

                    lineRt.sizeDelta = new Vector2(d, 1f);

                    float alpha = 0.12f * (1f - (d / maxLineDistance));
                    Image lineImg = lineRt.GetComponent<Image>();
                    lineImg.color = new Color(0f, 1f, 0.8f, alpha);

                    activeLines.Add(lineRt);
                }
            }
        }
    }

    private RectTransform GetLineFromPool()
    {
        if (linePool.Count > 0)
        {
            RectTransform rt = linePool[linePool.Count - 1];
            linePool.RemoveAt(linePool.Count - 1);
            return rt;
        }

        GameObject lineGo = new GameObject("Line");
        lineGo.transform.SetParent(neuronContainer, false);
        lineGo.transform.SetAsFirstSibling(); // Render behind node dots
        RectTransform lineRt = lineGo.AddComponent<RectTransform>();
        lineRt.pivot = new Vector2(0.5f, 0.5f);
        Image img = lineGo.AddComponent<Image>();
        img.color = new Color(0f, 1f, 0.8f, 0.1f);
        return lineRt;
    }
    #endregion

    #region Ollama Status Checker
    // Yerel Ollama sunucusuna HTTP GET isteği göndererek yapay zeka modülünün aktif olup olmadığını kontrol eden döngü.
    private IEnumerator CheckOllamaStatusLoop()
    {
        while (true)
        {
            using (UnityWebRequest request = UnityWebRequest.Get("http://127.0.0.1:11434"))
            {
                request.timeout = 2; // Cevap vermezse 2 saniye sonra zaman aşımına uğrar
                yield return request.SendWebRequest();

                if (ollamaStatusText != null && ollamaLed != null)
                {
                    // Bağlantı başarılı ise yeşil ışık yak ve durum metnini güncelle
                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        ollamaStatusText.text = "[Ollama: CONNECTED (127.0.0.1:11434)]";
                        ollamaStatusText.color = terminalGreen;
                        ollamaLed.color = Color.green;
                        ollamaLed.transform.GetChild(0).GetComponent<Image>().color = new Color(0f, 1f, 0f, 0.4f);
                    }
                    // Sunucu kapalı veya ulaşılamazsa kırmızı ışık yak
                    else
                    {
                        ollamaStatusText.text = "[Ollama: DISCONNECTED (OFFLINE)]";
                        ollamaStatusText.color = new Color(1f, 0.3f, 0.3f);
                        ollamaLed.color = Color.red;
                        ollamaLed.transform.GetChild(0).GetComponent<Image>().color = new Color(1f, 0f, 0f, 0.4f);
                    }
                }
            }
            // Her 3.5 saniyede bir durumu kontrol etmeye devam eder
            yield return new WaitForSeconds(3.5f);
        }
    }

    // Arayüzdeki Ollama durumu LED'inin parıldama efekti animasyonu
    private void AnimateOllamaLed()
    {
        if (ollamaLed == null) return;

        Transform glow = ollamaLed.transform.GetChild(0);
        if (glow != null)
        {
            // Sinüs dalgası kullanarak boyutunu dinamik olarak büyütüp küçültür
            float scale = 1f + 0.3f * Mathf.Sin(Time.time * 4f);
            glow.localScale = new Vector3(scale, scale, 1f);
        }
    }
    #endregion

    #region Cognitive Customization Editor
    public void OpenSettings()
    {
        PlaySynthBeep(520f, 0.1f);

        LoadSettingsValues();
        
        if (cognitiveSettingsPanel != null)
        {
            cognitiveSettingsPanel.SetActive(true);
        }
    }

    public void CloseSettings()
    {
        PlaySynthBeep(330f, 0.15f);
        if (cognitiveSettingsPanel != null)
        {
            cognitiveSettingsPanel.SetActive(false);
        }
    }

    private void LoadSettingsValues()
    {
        // Load actual PlayerPrefs
        tAhmetO = PlayerPrefs.GetFloat("NPC_Ahmet_Amca_openness", 0.3f);
        tAhmetC = PlayerPrefs.GetFloat("NPC_Ahmet_Amca_conscientiousness", 0.7f);
        tAhmetE = PlayerPrefs.GetFloat("NPC_Ahmet_Amca_extraversion", 0.2f);
        tAhmetA = PlayerPrefs.GetFloat("NPC_Ahmet_Amca_agreeableness", 0.4f);
        tAhmetN = PlayerPrefs.GetFloat("NPC_Ahmet_Amca_neuroticism", 0.8f);

        tAyseO = PlayerPrefs.GetFloat("NPC_Ayse_Teyze_openness", 0.7f);
        tAyseC = PlayerPrefs.GetFloat("NPC_Ayse_Teyze_conscientiousness", 0.5f);
        tAyseE = PlayerPrefs.GetFloat("NPC_Ayse_Teyze_extraversion", 0.8f);
        tAyseA = PlayerPrefs.GetFloat("NPC_Ayse_Teyze_agreeableness", 0.9f);
        tAyseN = PlayerPrefs.GetFloat("NPC_Ayse_Teyze_neuroticism", 0.2f);

        tMehmetO = PlayerPrefs.GetFloat("NPC_Mehmet_Amca_openness", 0.5f);
        tMehmetC = PlayerPrefs.GetFloat("NPC_Mehmet_Amca_conscientiousness", 0.8f);
        tMehmetE = PlayerPrefs.GetFloat("NPC_Mehmet_Amca_extraversion", 0.5f);
        tMehmetA = PlayerPrefs.GetFloat("NPC_Mehmet_Amca_agreeableness", 0.5f);
        tMehmetN = PlayerPrefs.GetFloat("NPC_Mehmet_Amca_neuroticism", 0.3f);

        // Sync values to Slider components
        SetSliderVal(ahmetO, tAhmetO);
        SetSliderVal(ahmetC, tAhmetC);
        SetSliderVal(ahmetE, tAhmetE);
        SetSliderVal(ahmetA, tAhmetA);
        SetSliderVal(ahmetN, tAhmetN);

        SetSliderVal(ayseO, tAyseO);
        SetSliderVal(ayseC, tAyseC);
        SetSliderVal(ayseE, tAyseE);
        SetSliderVal(ayseA, tAyseA);
        SetSliderVal(ayseN, tAyseN);

        SetSliderVal(mehmetO, tMehmetO);
        SetSliderVal(mehmetC, tMehmetC);
        SetSliderVal(mehmetE, tMehmetE);
        SetSliderVal(mehmetA, tMehmetA);
        SetSliderVal(mehmetN, tMehmetN);
    }

    private void SetSliderVal(Slider sl, float val)
    {
        if (sl != null)
        {
            sl.value = val;
            // Force label text update
            sl.onValueChanged?.Invoke(val);
        }
    }

    // Slider bileşenlerinden okunan güncel OCEAN değerlerini PlayerPrefs üzerine kaydeder
    private void SaveSettingsValues()
    {
        PlaySynthBeep(660f, 0.15f);

        // Ahmet Amca değerleri
        PlayerPrefs.SetFloat("NPC_Ahmet_Amca_openness", ahmetO ? ahmetO.value : 0.3f);
        PlayerPrefs.SetFloat("NPC_Ahmet_Amca_conscientiousness", ahmetC ? ahmetC.value : 0.7f);
        PlayerPrefs.SetFloat("NPC_Ahmet_Amca_extraversion", ahmetE ? ahmetE.value : 0.2f);
        PlayerPrefs.SetFloat("NPC_Ahmet_Amca_agreeableness", ahmetA ? ahmetA.value : 0.4f);
        PlayerPrefs.SetFloat("NPC_Ahmet_Amca_neuroticism", ahmetN ? ahmetN.value : 0.8f);

        // Ayse Teyze değerleri
        PlayerPrefs.SetFloat("NPC_Ayse_Teyze_openness", ayseO ? ayseO.value : 0.7f);
        PlayerPrefs.SetFloat("NPC_Ayse_Teyze_conscientiousness", ayseC ? ayseC.value : 0.5f);
        PlayerPrefs.SetFloat("NPC_Ayse_Teyze_extraversion", ayseE ? ayseE.value : 0.8f);
        PlayerPrefs.SetFloat("NPC_Ayse_Teyze_agreeableness", ayseA ? ayseA.value : 0.9f);
        PlayerPrefs.SetFloat("NPC_Ayse_Teyze_neuroticism", ayseN ? ayseN.value : 0.2f);

        // Mehmet Amca değerleri
        PlayerPrefs.SetFloat("NPC_Mehmet_Amca_openness", mehmetO ? mehmetO.value : 0.5f);
        PlayerPrefs.SetFloat("NPC_Mehmet_Amca_conscientiousness", mehmetC ? mehmetC.value : 0.8f);
        PlayerPrefs.SetFloat("NPC_Mehmet_Amca_extraversion", mehmetE ? mehmetE.value : 0.5f);
        PlayerPrefs.SetFloat("NPC_Mehmet_Amca_agreeableness", mehmetA ? mehmetA.value : 0.5f);
        PlayerPrefs.SetFloat("NPC_Mehmet_Amca_neuroticism", mehmetN ? mehmetN.value : 0.3f);

        PlayerPrefs.Save();
        Debug.Log("Saved updated OCEAN values to PlayerPrefs.");
    }

    private void ResetSettingsToDefault()
    {
        PlayerPrefs.DeleteKey("NPC_Ahmet_Amca_openness");
        PlayerPrefs.DeleteKey("NPC_Ahmet_Amca_conscientiousness");
        PlayerPrefs.DeleteKey("NPC_Ahmet_Amca_extraversion");
        PlayerPrefs.DeleteKey("NPC_Ahmet_Amca_agreeableness");
        PlayerPrefs.DeleteKey("NPC_Ahmet_Amca_neuroticism");

        PlayerPrefs.DeleteKey("NPC_Ayse_Teyze_openness");
        PlayerPrefs.DeleteKey("NPC_Ayse_Teyze_conscientiousness");
        PlayerPrefs.DeleteKey("NPC_Ayse_Teyze_extraversion");
        PlayerPrefs.DeleteKey("NPC_Ayse_Teyze_agreeableness");
        PlayerPrefs.DeleteKey("NPC_Ayse_Teyze_neuroticism");

        PlayerPrefs.DeleteKey("NPC_Mehmet_Amca_openness");
        PlayerPrefs.DeleteKey("NPC_Mehmet_Amca_conscientiousness");
        PlayerPrefs.DeleteKey("NPC_Mehmet_Amca_extraversion");
        PlayerPrefs.DeleteKey("NPC_Mehmet_Amca_agreeableness");
        PlayerPrefs.DeleteKey("NPC_Mehmet_Amca_neuroticism");

        InitializeDefaultTraits();
        UpdatePersonalityLabels();
        Debug.Log("Reset all traits to defaults.");
    }

    private void UpdatePersonalityLabels()
    {
        if (ahmetLabel == null || ayseLabel == null || mehmetLabel == null) return;

        float ahmetOVal = PlayerPrefs.GetFloat("NPC_Ahmet_Amca_openness", 0.3f);
        float ahmetNVal = PlayerPrefs.GetFloat("NPC_Ahmet_Amca_neuroticism", 0.8f);
        float ahmetEVal = PlayerPrefs.GetFloat("NPC_Ahmet_Amca_extraversion", 0.2f);
        float ahmetCVal = PlayerPrefs.GetFloat("NPC_Ahmet_Amca_conscientiousness", 0.7f);
        float ahmetAVal = PlayerPrefs.GetFloat("NPC_Ahmet_Amca_agreeableness", 0.4f);
        string ahmetTrait = EvaluateTrait("Ahmet", ahmetOVal, ahmetCVal, ahmetEVal, ahmetAVal, ahmetNVal);
        ahmetLabel.text = $"[ Ahmet Amca:  <color=#ff6666>{ahmetTrait}</color> ]";

        float ayseOVal = PlayerPrefs.GetFloat("NPC_Ayse_Teyze_openness", 0.7f);
        float ayseNVal = PlayerPrefs.GetFloat("NPC_Ayse_Teyze_neuroticism", 0.2f);
        float ayseEVal = PlayerPrefs.GetFloat("NPC_Ayse_Teyze_extraversion", 0.8f);
        float ayseCVal = PlayerPrefs.GetFloat("NPC_Ayse_Teyze_conscientiousness", 0.5f);
        float ayseAVal = PlayerPrefs.GetFloat("NPC_Ayse_Teyze_agreeableness", 0.9f);
        string ayseTrait = EvaluateTrait("Ayse", ayseOVal, ayseCVal, ayseEVal, ayseAVal, ayseNVal);
        ayseLabel.text = $"[ Ayşe Teyze:  <color=#66ff66>{ayseTrait}</color> ]";

        float mehmetOVal = PlayerPrefs.GetFloat("NPC_Mehmet_Amca_openness", 0.5f);
        float mehmetNVal = PlayerPrefs.GetFloat("NPC_Mehmet_Amca_neuroticism", 0.3f);
        float mehmetEVal = PlayerPrefs.GetFloat("NPC_Mehmet_Amca_extraversion", 0.5f);
        float mehmetCVal = PlayerPrefs.GetFloat("NPC_Mehmet_Amca_conscientiousness", 0.8f);
        float mehmetAVal = PlayerPrefs.GetFloat("NPC_Mehmet_Amca_agreeableness", 0.5f);
        string mehmetTrait = EvaluateTrait("Mehmet", mehmetOVal, mehmetCVal, mehmetEVal, mehmetAVal, mehmetNVal);
        mehmetLabel.text = $"[ Mehmet Amca:  <color=#ffff66>{mehmetTrait}</color> ]";
    }

    private string EvaluateTrait(string npc, float o, float c, float e, float a, float n)
    {
        if (npc == "Ahmet")
        {
            if (n > 0.6f && e < 0.4f) return "Kırılgan";
            if (a < 0.4f) return "Huysuz";
            return "Dengeli";
        }
        else if (npc == "Ayse")
        {
            if (e > 0.6f && a > 0.6f) return "Sosyal";
            if (n > 0.6f) return "Kaygılı";
            return "Sakin";
        }
        else if (npc == "Mehmet")
        {
            if (n < 0.4f && c > 0.6f) return "Dengeli";
            if (e < 0.3f) return "İçedönük";
            return "Sakin";
        }
        return "Dengeli";
    }
    #endregion

    #region Scene Navigation Controls
    public void StartGame()
    {
        Debug.Log("Loading SampleScene...");
        SceneManager.LoadScene("SampleScene");
    }

    public void QuitGame()
    {
        Debug.Log("Exiting Application...");
        Application.Quit();
    }
    #endregion

    #region Audio Generation
    // Menü etkileşim seslerini (klik ve hover bip sesleri) kod tarafında anlık sentezleyerek çalan metot
    private void PlaySynthBeep(float frequency, float duration)
    {
        // Ses çalmak için geçici bir GameObject oluşturur
        GameObject audioObj = new GameObject("SynthBeep");
        AudioSource audioSource = audioObj.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // Sesi 2D/Stereo olarak oynatır (derinliksiz)

        int sampleRate = 44100;
        float[] samples = new float[(int)(sampleRate * duration)];
        
        // Matematiksel sinüs dalgası kullanarak ses örneklerini oluşturur
        for (int i = 0; i < samples.Length; i++)
        {
            float percent = (float)i / samples.Length;
            float envelope = 1f - percent; // Sesin sonuna doğru azalan sönümleme zarfı (envelope)
            samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * i / sampleRate) * 0.08f * envelope * envelope;
        }

        // Oluşturulan örneklerle yeni bir ses klibi yaratır ve çalar
        AudioClip clip = AudioClip.Create("BeepClip", samples.Length, 1, sampleRate, false);
        clip.SetData(samples, 0);
        audioSource.clip = clip;
        audioSource.Play();
        
        // Ses çalma işlemi tamamlandıktan kısa süre sonra geçici objeyi siler
        Destroy(audioObj, duration + 0.15f);
    }
    #endregion
}
