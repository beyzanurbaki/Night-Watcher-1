using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

// Unity Editöründe geliştiricinin menü tasarımını otomatik oluşturmasını (Baking) sağlayan araç sınıfıdır.
// Sahne içerisine gerekli Canvas, Panel, Buton ve OCEAN ayar bileşenlerini yerleştirip birbirine bağlar.
public class MainMenuSetup : Editor
{
    private static Color terminalGreen = new Color(0.2f, 1f, 0.2f, 1f);
    private static Color terminalCyan = new Color(0f, 1f, 1f, 1f);
    private static Color darkBg = new Color(0.04f, 0.06f, 0.1f, 0.9f);
    private static Color borderColors = new Color(0f, 0.8f, 0.8f, 0.4f);

    // Üst menüde Tools altında "Set Up Main Menu UI" butonu oluşturur
    [MenuItem("Tools/Set Up Main Menu UI")]
    public static void SetupMainMenuUI()
    {
        // 1. Ana Menü sahnesini açar
        string scenePath = "Assets/Scenes/MainMenu.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        
        if (!scene.IsValid())
        {
            Debug.LogError("Error: MainMenu scene could not be opened: " + scenePath);
            return;
        }

        // 2. Sahnedeki MenuManager nesnesini bulur veya yoksa oluşturur
        GameObject managerGo = GameObject.Find("MenuManager");
        if (managerGo == null)
        {
            managerGo = new GameObject("MenuManager");
        }
        MainMenuManager manager = managerGo.GetComponent<MainMenuManager>();
        if (manager == null)
        {
            manager = managerGo.AddComponent<MainMenuManager>();
        }

        // 3. Canvas bileşenini bulur veya yeni bir Canvas oluşturur
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGo = new GameObject("Canvas");
            canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();
            Debug.Log("Created new Canvas in scene.");
        }

        // Temiz bir oluşturma yapmak için Canvas altındaki diğer çocukları deaktive eder (MenuManager ve CyberpunkUI_Root hariç)
        foreach (Transform child in canvas.transform)
        {
            if (child.gameObject.name != managerGo.name && child.gameObject.name != "CyberpunkUI_Root")
            {
                child.gameObject.SetActive(false);
            }
        }

        // Çakışmaları engellemek için eğer eski bir CyberpunkUI_Root varsa siler
        Transform existingRoot = canvas.transform.Find("CyberpunkUI_Root");
        if (existingRoot != null)
        {
            Undo.DestroyObjectImmediate(existingRoot.gameObject);
        }

        // Projedeki gerekli font ve görsel kaynakları (AssetDatabase ile) yükler
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/PixelifySans-VariableFont_wght SDF.asset");
        Sprite ahmet = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/NPC/Ahmet/character_maleAdventurer_idle.png");
        Sprite ayse = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/NPC/Ayse/character_femalePerson_idle 1.png");
        Sprite mehmet = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/NPC/Mehmet/character_malePerson_idle.png");

        manager.fontAsset = font;
        manager.ahmetSprite = ahmet;
        manager.ayseSprite = ayse;
        manager.mehmetSprite = mehmet;

        // 4. Ana UI kök nesnesini (CyberpunkUI_Root) inşa eder
        GameObject rootGo = new GameObject("CyberpunkUI_Root");
        rootGo.transform.SetParent(canvas.transform, false);
        RectTransform rootRt = rootGo.AddComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.sizeDelta = Vector2.zero;
        Undo.RegisterCreatedObjectUndo(rootGo, "Create Cyberpunk UI Root");

        // Koyu renkli arka plan paneli
        GameObject bg = new GameObject("BgSlate");
        bg.transform.SetParent(rootGo.transform, false);
        RectTransform bgRt = bg.AddComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.sizeDelta = Vector2.zero;
        Image bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.03f, 0.05f, 0.08f, 1f);

        // Sinir ağı simülasyonu parçacık konteyneri
        GameObject neuronGo = new GameObject("NeuronContainer");
        neuronGo.transform.SetParent(rootGo.transform, false);
        RectTransform neuronRt = neuronGo.AddComponent<RectTransform>();
        neuronRt.anchorMin = Vector2.zero;
        neuronRt.anchorMax = Vector2.one;
        neuronRt.sizeDelta = Vector2.zero;
        manager.neuronContainer = neuronRt;

        // --- ÜST BAŞLIK BARI (HEADER BAR) ---
        GameObject header = CreateGlassPanel(rootGo.transform, "HeaderBar", new Vector2(-40f, 50f), new Vector2(0f, -10f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Color(0f, 1f, 1f, 0.15f), darkBg);

        // Ollama Durum paneli
        GameObject ollamaPanel = new GameObject("OllamaPanel");
        ollamaPanel.transform.SetParent(header.transform, false);
        RectTransform ollamaRt = ollamaPanel.AddComponent<RectTransform>();
        ollamaRt.anchorMin = new Vector2(0f, 0.5f);
        ollamaRt.anchorMax = new Vector2(0f, 0.5f);
        ollamaRt.pivot = new Vector2(0f, 0.5f);
        ollamaRt.anchoredPosition = new Vector2(15f, 0f);
        ollamaRt.sizeDelta = new Vector2(400f, 30f);

        // LED Durum Göstergesi
        GameObject led = new GameObject("LedIndicator");
        led.transform.SetParent(ollamaPanel.transform, false);
        RectTransform ledRt = led.AddComponent<RectTransform>();
        ledRt.anchorMin = new Vector2(0f, 0.5f);
        ledRt.anchorMax = new Vector2(0f, 0.5f);
        ledRt.pivot = new Vector2(0f, 0.5f);
        ledRt.anchoredPosition = new Vector2(0f, 0f);
        ledRt.sizeDelta = new Vector2(10f, 10f);
        Image ledImg = led.AddComponent<Image>();
        ledImg.color = Color.red;
        manager.ollamaLed = ledImg;

        // LED Parlama Efekti
        GameObject ledGlow = new GameObject("LedGlow");
        ledGlow.transform.SetParent(led.transform, false);
        RectTransform ledGlowRt = ledGlow.AddComponent<RectTransform>();
        ledGlowRt.anchorMin = Vector2.zero;
        ledGlowRt.anchorMax = Vector2.one;
        ledGlowRt.sizeDelta = new Vector2(8f, 8f);
        Image ledGlowImg = ledGlow.AddComponent<Image>();
        ledGlowImg.color = new Color(1f, 0f, 0f, 0.4f);

        // Bağlantı durum metni
        var statusTxt = CreateText(ollamaPanel.transform, "StatusText", "[Ollama: CONNECTING...]", 13, TextAlignmentOptions.Left, new Color(0.7f, 0.8f, 0.9f), new Vector2(380f, 30f), new Vector2(20f, 0f), font);
        manager.ollamaStatusText = statusTxt;

        // Akademik proje bilgilendirme metni
        CreateText(header.transform, "AcademicText", "Haziran 2026 - ISTANBUL   |   (Akademik Savunma Notu)", 13, TextAlignmentOptions.Right, terminalCyan, new Vector2(600f, 40f), new Vector2(-15f, 0f), font);

        // --- ANA PROJE BAŞLIK PANELİ (TITLE) ---
        GameObject titleGo = new GameObject("MainTitle");
        titleGo.transform.SetParent(rootGo.transform, false);
        RectTransform titleRt = titleGo.AddComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.5f, 1f);
        titleRt.anchorMax = new Vector2(0.5f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.anchoredPosition = new Vector2(0f, -80f);
        titleRt.sizeDelta = new Vector2(1100f, 90f);

        TextMeshProUGUI titleText = titleGo.AddComponent<TextMeshProUGUI>();
        titleText.font = font;
        titleText.fontSize = 25;
        titleText.color = Color.white;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.lineSpacing = 15;
        titleText.text = "<color=#00ffff>||  YAPAY ZEKA DESTEKLİ OYUNCU OLMAYAN KARAKTER (NPC) GELİŞTİRME  ||</color>";

        GameObject titleSubGo = new GameObject("MainTitleSub");
        titleSubGo.transform.SetParent(titleGo.transform, false);
        RectTransform titleSubRt = titleSubGo.AddComponent<RectTransform>();
        titleSubRt.anchorMin = new Vector2(0f, 0f);
        titleSubRt.anchorMax = new Vector2(1f, 0f);
        titleSubRt.anchoredPosition = new Vector2(0f, 10f);
        titleSubRt.sizeDelta = new Vector2(0f, 20f);
        TextMeshProUGUI titleSubText = titleSubGo.AddComponent<TextMeshProUGUI>();
        titleSubText.font = font;
        titleSubText.fontSize = 15;
        titleSubText.color = new Color(0.6f, 0.7f, 0.8f);
        titleSubText.alignment = TextAlignmentOptions.Center;
        titleSubText.text = "(Bilişsel Simülasyon Arayüzü)";

        // --- ORTA ALAN PANELİ ---
        GameObject middleContainer = new GameObject("MiddleContainer");
        middleContainer.transform.SetParent(rootGo.transform, false);
        RectTransform midRt = middleContainer.AddComponent<RectTransform>();
        midRt.anchorMin = new Vector2(0.5f, 0.5f);
        midRt.anchorMax = new Vector2(0.5f, 0.5f);
        midRt.pivot = new Vector2(0.5f, 0.5f);
        midRt.anchoredPosition = new Vector2(0f, -40f);
        midRt.sizeDelta = new Vector2(1200f, 440f);

        // --- SOL PANEL: ANA MENÜ SEÇENEKLERİ ---
        GameObject leftPanel = CreateGlassPanel(middleContainer.transform, "LeftPanel_Menu", new Vector2(520f, 380f), new Vector2(40f, 0f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), borderColors, darkBg);
        
        CreateText(leftPanel.transform, "LeftPanelTitle", "[ MENÜ SEÇENEKLERİ ]", 18, TextAlignmentOptions.Center, terminalCyan, new Vector2(480f, 30f), new Vector2(0f, -20f), font);

        GameObject btnContainer = new GameObject("ButtonContainer");
        btnContainer.transform.SetParent(leftPanel.transform, false);
        RectTransform btnConRt = btnContainer.AddComponent<RectTransform>();
        btnConRt.anchorMin = Vector2.zero;
        btnConRt.anchorMax = Vector2.one;
        btnConRt.anchoredPosition = new Vector2(0f, -25f);
        btnConRt.sizeDelta = new Vector2(-40f, -100f);

        // Hazır butonları oluşturur ve yerleştirir
        CreateBakedButton(btnContainer.transform, new Vector2(0f, 160f), "INITIALIZE_SIMULATION", "INITIALIZE SIMULATION", "Simülasyonu Başlat", font);
        CreateBakedButton(btnContainer.transform, new Vector2(0f, 80f), "COGNITIVE_PROFILES", "COGNITIVE PROFILES", "Mizaç ve Kişilik Ayarları", font);
        CreateBakedButton(btnContainer.transform, new Vector2(0f, 0f), "SHUTDOWN", "SHUTDOWN", "Çıkış", font);

        CreateText(leftPanel.transform, "LeftCaption", "(Arka Planda Şeffaf Nöron Çizgileri)", 11, TextAlignmentOptions.Center, new Color(0.5f, 0.6f, 0.7f, 0.7f), new Vector2(480f, 20f), new Vector2(0f, 15f), font);

        // --- SAĞ PANEL: COGNITIVE DURUM EKRANI ---
        GameObject rightPanel = CreateGlassPanel(middleContainer.transform, "RightPanel_Status", new Vector2(560f, 380f), new Vector2(-40f, 0f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), borderColors, darkBg);

        CreateText(rightPanel.transform, "RightPanelTitle", "[ SİSTEM COGNITIVE DURUMU ]", 18, TextAlignmentOptions.Center, terminalCyan, new Vector2(520f, 30f), new Vector2(0f, -20f), font);

        // Sunucu özellikleri bilgi bloğu
        CreateText(rightPanel.transform, "SpecsBlock", "* Model: Phi-3 (3.8B) [Q4]\n* Altyapı: Local Ollama API\n* Algoritma: Ebbinghaus & OCEAN Model", 13, TextAlignmentOptions.Left, new Color(0.8f, 0.9f, 1f), new Vector2(500f, 75f), new Vector2(30f, -50f), font);

        GameObject charListGo = new GameObject("CharacterList");
        charListGo.transform.SetParent(rightPanel.transform, false);
        RectTransform charListRt = charListGo.AddComponent<RectTransform>();
        charListRt.anchorMin = Vector2.zero;
        charListRt.anchorMax = Vector2.one;
        charListRt.anchoredPosition = new Vector2(0f, -50f);
        charListRt.sizeDelta = new Vector2(-40f, -170f);

        // Karakter durum satırlarını ekler
        manager.ahmetLabel = CreateCharacterStatusRow(charListGo.transform, new Vector2(0f, 100f), ahmet, "Ahmet Amca", font);
        manager.ayseLabel = CreateCharacterStatusRow(charListGo.transform, new Vector2(0f, 50f), ayse, "Ayşe Teyze", font);
        manager.mehmetLabel = CreateCharacterStatusRow(charListGo.transform, new Vector2(0f, 0f), mehmet, "Mehmet Amca", font);

        CreateText(rightPanel.transform, "RightCaption", "(Karakterlerin Minik Sprite'ları)", 11, TextAlignmentOptions.Center, new Color(0.5f, 0.6f, 0.7f, 0.7f), new Vector2(520f, 20f), new Vector2(0f, 15f), font);

        // --- ALT BİLGİ ALANI (FOOTER BAR) ---
        GameObject footer = new GameObject("FooterBar");
        footer.transform.SetParent(rootGo.transform, false);
        RectTransform footerRt = footer.AddComponent<RectTransform>();
        footerRt.anchorMin = new Vector2(0f, 0f);
        footerRt.anchorMax = new Vector2(1f, 0f);
        footerRt.pivot = new Vector2(0.5f, 0f);
        footerRt.anchoredPosition = new Vector2(0f, 20f);
        footerRt.sizeDelta = new Vector2(-80f, 30f);

        GameObject footerLine = new GameObject("FooterLine");
        footerLine.transform.SetParent(footer.transform, false);
        RectTransform footerLineRt = footerLine.AddComponent<RectTransform>();
        footerLineRt.anchorMin = new Vector2(0f, 1f);
        footerLineRt.anchorMax = new Vector2(1f, 1f);
        footerLineRt.anchoredPosition = Vector2.zero;
        footerLineRt.sizeDelta = new Vector2(0f, 1.5f);
        Image flImg = footerLine.AddComponent<Image>();
        flImg.color = borderColors;

        CreateText(footer.transform, "FooterLeftText", "Beyzanur BAKİ - Yazılım Mühendisliği", 13, TextAlignmentOptions.Left, new Color(0.7f, 0.8f, 0.9f), new Vector2(500f, 30f), new Vector2(5f, -10f), font);
        CreateText(footer.transform, "FooterRightText", "Danışman: Dr. Öğr. Üyesi İlhan Gari", 13, TextAlignmentOptions.Right, new Color(0.7f, 0.8f, 0.9f), new Vector2(500f, 30f), new Vector2(-5f, -10f), font);

        // --- COGNITIVE KİŞİLİK PROFLİ AYARLARI PANELİ (OVERLAY) ---
        GameObject overlay = new GameObject("CognitiveSettingsOverlay");
        overlay.transform.SetParent(rootGo.transform, false);
        RectTransform overlayRt = overlay.AddComponent<RectTransform>();
        overlayRt.anchorMin = Vector2.zero;
        overlayRt.anchorMax = Vector2.one;
        overlayRt.sizeDelta = Vector2.zero;
        manager.cognitiveSettingsPanel = overlay;

        GameObject tint = new GameObject("Tint");
        tint.transform.SetParent(overlay.transform, false);
        RectTransform tintRt = tint.AddComponent<RectTransform>();
        tintRt.anchorMin = Vector2.zero;
        tintRt.anchorMax = Vector2.one;
        tintRt.sizeDelta = Vector2.zero;
        Image tintImg = tint.AddComponent<Image>();
        tintImg.color = new Color(0.02f, 0.04f, 0.07f, 0.95f);

        // Ayar çerçeve paneli
        GameObject frame = CreateGlassPanel(overlay.transform, "FramePanel", new Vector2(850f, 620f), Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), borderColors, darkBg);

        CreateText(frame.transform, "Title", "[ BİLİŞSEL KİŞİLİK PROFİLİ AYARLARI (OCEAN) ]", 20, TextAlignmentOptions.Center, terminalCyan, new Vector2(800f, 40f), new Vector2(0f, -20f), font);

        GameObject cols = new GameObject("Columns");
        cols.transform.SetParent(frame.transform, false);
        RectTransform colsRt = cols.AddComponent<RectTransform>();
        colsRt.anchorMin = Vector2.zero;
        colsRt.anchorMax = Vector2.one;
        colsRt.anchoredPosition = new Vector2(0f, -25f);
        colsRt.sizeDelta = new Vector2(-40f, -140f);

        // Üç karakter için ayrı ayar kolonları oluşturur
        Transform colAhmet = CreateSettingsColumn(cols.transform, new Vector2(-260f, 0f), "Ahmet Amca (Kırılgan)", font);
        Transform colAyse = CreateSettingsColumn(cols.transform, new Vector2(0f, 0f), "Ayşe Teyze (Sosyal)", font);
        Transform colMehmet = CreateSettingsColumn(cols.transform, new Vector2(260f, 0f), "Mehmet Amca (Dengeli)", font);

        // Ahmet Amca OCEAN Slider'ları
        manager.ahmetO = CreateEditorSlider(colAhmet, "Deneyime Açıklık", 0.3f, font);
        manager.ahmetO.transform.parent.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 160f);
        manager.ahmetC = CreateEditorSlider(colAhmet, "Sorumluluk", 0.4f, font);
        manager.ahmetC.transform.parent.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 90f);
        manager.ahmetE = CreateEditorSlider(colAhmet, "Dışadönüklük", 0.2f, font);
        manager.ahmetE.transform.parent.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 20f);
        manager.ahmetA = CreateEditorSlider(colAhmet, "Uyumluluk", 0.3f, font);
        manager.ahmetA.transform.parent.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -50f);
        manager.ahmetN = CreateEditorSlider(colAhmet, "Nevrotiklik", 0.8f, font);
        manager.ahmetN.transform.parent.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -120f);

        // Ayşe Teyze OCEAN Slider'ları
        manager.ayseO = CreateEditorSlider(colAyse, "Deneyime Açıklık", 0.7f, font);
        manager.ayseO.transform.parent.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 160f);
        manager.ayseC = CreateEditorSlider(colAyse, "Sorumluluk", 0.6f, font);
        manager.ayseC.transform.parent.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 90f);
        manager.ayseE = CreateEditorSlider(colAyse, "Dışadönüklük", 0.8f, font);
        manager.ayseE.transform.parent.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 20f);
        manager.ayseA = CreateEditorSlider(colAyse, "Uyumluluk", 0.9f, font);
        manager.ayseA.transform.parent.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -50f);
        manager.ayseN = CreateEditorSlider(colAyse, "Nevrotiklik", 0.3f, font);
        manager.ayseN.transform.parent.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -120f);

        // Mehmet Amca OCEAN Slider'ları
        manager.mehmetO = CreateEditorSlider(colMehmet, "Deneyime Açıklık", 0.5f, font);
        manager.mehmetO.transform.parent.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 160f);
        manager.mehmetC = CreateEditorSlider(colMehmet, "Sorumluluk", 0.7f, font);
        manager.mehmetC.transform.parent.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 90f);
        manager.mehmetE = CreateEditorSlider(colMehmet, "Dışadönüklük", 0.5f, font);
        manager.mehmetE.transform.parent.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 20f);
        manager.mehmetA = CreateEditorSlider(colMehmet, "Uyumluluk", 0.6f, font);
        manager.mehmetA.transform.parent.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -50f);
        manager.mehmetN = CreateEditorSlider(colMehmet, "Nevrotiklik", 0.4f, font);
        manager.mehmetN.transform.parent.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -120f);

        // Panel Kontrol Butonları (Kaydet, Varsayılan, İptal)
        GameObject controls = new GameObject("Controls");
        controls.transform.SetParent(frame.transform, false);
        RectTransform ctrlRt = controls.AddComponent<RectTransform>();
        ctrlRt.anchorMin = new Vector2(0f, 0f);
        ctrlRt.anchorMax = new Vector2(1f, 0f);
        ctrlRt.pivot = new Vector2(0.5f, 0f);
        ctrlRt.anchoredPosition = new Vector2(0f, 25f);
        ctrlRt.sizeDelta = new Vector2(-80f, 50f);

        CreateSettingsButton(controls.transform, new Vector2(-220f, 0f), "Btn_Save", "[ KAYDET VE KAPAT ]", terminalGreen, font);
        CreateSettingsButton(controls.transform, new Vector2(0f, 0f), "Btn_Reset", "[ VARSAYILAN ]", terminalCyan, font);
        CreateSettingsButton(controls.transform, new Vector2(220f, 0f), "Btn_Cancel", "[ İPTAL ]", Color.white, font);

        // Başlangıçta Kişilik Ayarları panelini gizler
        overlay.SetActive(false);

        // 5. Değişikliklerin kaydedilmesi için sahneyi ve objeleri kirli (dirty) olarak işaretler ve sahneyi kaydeder.
        EditorUtility.SetDirty(manager);
        
        bool saveSuccess = EditorSceneManager.SaveScene(scene);
        if (saveSuccess)
        {
            Debug.Log("<color=green>BAKED SUCCESS:</color> Main Menu UI elements have been fully baked and compiled permanently in scene hierarchy!");
        }
        else
        {
            Debug.LogError("Error: Scene could not be saved.");
        }
    }

    #region Helper Factories
    private static GameObject CreateGlassPanel(Transform parent, string name, Vector2 size, Vector2 anchoredPos, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Color borderColor, Color bgColor)
    {
        GameObject outer = new GameObject(name);
        outer.transform.SetParent(parent, false);
        RectTransform outerRt = outer.AddComponent<RectTransform>();
        outerRt.anchorMin = anchorMin;
        outerRt.anchorMax = anchorMax;
        outerRt.pivot = pivot;
        outerRt.anchoredPosition = anchoredPos;
        outerRt.sizeDelta = size;
        
        Image borderImg = outer.AddComponent<Image>();
        borderImg.color = borderColor;

        GameObject inner = new GameObject("InnerGlass");
        inner.transform.SetParent(outer.transform, false);
        RectTransform innerRt = inner.AddComponent<RectTransform>();
        innerRt.anchorMin = Vector2.zero;
        innerRt.anchorMax = Vector2.one;
        innerRt.offsetMin = new Vector2(2f, 2f);
        innerRt.offsetMax = new Vector2(-2f, -2f);
        Image innerImg = inner.AddComponent<Image>();
        innerImg.color = bgColor;

        return outer;
    }

    private static TextMeshProUGUI CreateText(Transform parent, string name, string content, int fontSize, TextAlignmentOptions align, Color color, Vector2 size, Vector2 anchoredPos, TMP_FontAsset font)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = size;
        rt.anchoredPosition = anchoredPos;
        
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.font = font;
        tmp.fontSize = fontSize;
        tmp.alignment = align;
        tmp.color = color;
        tmp.text = content;
        return tmp;
    }

    private static void CreateBakedButton(Transform parent, Vector2 anchoredPosition, string goName, string title, string sub, TMP_FontAsset font)
    {
        GameObject btnGo = new GameObject("Btn_" + goName);
        btnGo.transform.SetParent(parent, false);
        RectTransform rt = btnGo.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = new Vector2(460f, 55f);

        Image btnImg = btnGo.AddComponent<Image>();
        btnImg.color = new Color(0f, 0f, 0f, 0.2f);

        Button button = btnGo.AddComponent<Button>();
        button.targetGraphic = btnImg;
        Navigation nav = button.navigation;
        nav.mode = Navigation.Mode.None;
        button.navigation = nav;

        // Glow image
        GameObject glow = new GameObject("Glow");
        glow.transform.SetParent(btnGo.transform, false);
        RectTransform glowRt = glow.AddComponent<RectTransform>();
        glowRt.anchorMin = Vector2.zero;
        glowRt.anchorMax = Vector2.one;
        glowRt.sizeDelta = Vector2.zero;
        Image glowImg = glow.AddComponent<Image>();
        glowImg.color = new Color(0f, 1f, 1f, 0.08f);
        glow.SetActive(false);

        // Arrow text
        GameObject arrowGo = new GameObject("Arrow");
        arrowGo.transform.SetParent(btnGo.transform, false);
        RectTransform arrowRt = arrowGo.AddComponent<RectTransform>();
        arrowRt.anchorMin = new Vector2(0f, 0.5f);
        arrowRt.anchorMax = new Vector2(0f, 0.5f);
        arrowRt.pivot = new Vector2(0f, 0.5f);
        arrowRt.anchoredPosition = new Vector2(10f, 0f);
        arrowRt.sizeDelta = new Vector2(30f, 30f);
        TextMeshProUGUI arrowTxt = arrowGo.AddComponent<TextMeshProUGUI>();
        arrowTxt.font = font;
        arrowTxt.fontSize = 18;
        arrowTxt.color = terminalGreen;
        arrowTxt.text = ">";
        arrowGo.SetActive(false);

        // Button label
        GameObject textGo = new GameObject("Text");
        textGo.transform.SetParent(btnGo.transform, false);
        RectTransform txtRt = textGo.AddComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.sizeDelta = new Vector2(-60f, 0f);
        TextMeshProUGUI btnTxt = textGo.AddComponent<TextMeshProUGUI>();
        btnTxt.font = font;
        btnTxt.fontSize = 14;
        btnTxt.color = Color.white;
        btnTxt.alignment = TextAlignmentOptions.Left;
        btnTxt.lineSpacing = 5;
        btnTxt.text = $"[ {title} ]\n<color=#668899><size=11>{sub}</size></color>";

        btnGo.AddComponent<EventTrigger>();
    }

    private static TextMeshProUGUI CreateCharacterStatusRow(Transform parent, Vector2 anchoredPosition, Sprite sprite, string label, TMP_FontAsset font)
    {
        GameObject rowGo = CreateGlassPanel(parent, "Row_" + label.Replace(" ", "_"), new Vector2(520f, 45f), anchoredPosition, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Color(0f, 1f, 1f, 0.1f), darkBg);

        // Mini sprite image
        GameObject imgGo = new GameObject("SpriteImage");
        imgGo.transform.SetParent(rowGo.transform, false);
        RectTransform imgRt = imgGo.AddComponent<RectTransform>();
        imgRt.anchorMin = new Vector2(0f, 0.5f);
        imgRt.anchorMax = new Vector2(0f, 0.5f);
        imgRt.pivot = new Vector2(0f, 0.5f);
        imgRt.anchoredPosition = new Vector2(15f, 0f);
        imgRt.sizeDelta = new Vector2(32f, 32f);
        Image img = imgGo.AddComponent<Image>();
        img.sprite = sprite;
        img.preserveAspect = true;

        // Status label
        GameObject labelGo = new GameObject("StatusLabel");
        labelGo.transform.SetParent(rowGo.transform, false);
        RectTransform labelRt = labelGo.AddComponent<RectTransform>();
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.pivot = new Vector2(0f, 0.5f);
        labelRt.anchoredPosition = new Vector2(65f, 0f);
        labelRt.sizeDelta = new Vector2(-120f, 0f);

        TextMeshProUGUI statusLabel = labelGo.AddComponent<TextMeshProUGUI>();
        statusLabel.font = font;
        statusLabel.fontSize = 14;
        statusLabel.color = Color.white;
        statusLabel.alignment = TextAlignmentOptions.Left;
        statusLabel.text = $"[ {label}:  Sorgulanıyor ]";

        return statusLabel;
    }

    private static Transform CreateSettingsColumn(Transform parent, Vector2 anchoredPos, string charName, TMP_FontAsset font)
    {
        GameObject colGo = new GameObject("Col_" + charName.Replace(" ", "_").Replace("(", "").Replace(")", ""));
        colGo.transform.SetParent(parent, false);
        RectTransform colRt = colGo.AddComponent<RectTransform>();
        colRt.anchorMin = new Vector2(0.5f, 0f);
        colRt.anchorMax = new Vector2(0.5f, 1f);
        colRt.pivot = new Vector2(0.5f, 0.5f);
        colRt.anchoredPosition = anchoredPos;
        colRt.sizeDelta = new Vector2(250f, 0f);

        // Header name
        CreateText(colGo.transform, "ColHeader", charName, 13, TextAlignmentOptions.Center, terminalGreen, new Vector2(250f, 30f), new Vector2(0f, 195f), font);

        return colGo.transform;
    }

    private static Slider CreateEditorSlider(Transform parent, string label, float defaultVal, TMP_FontAsset font)
    {
        GameObject container = new GameObject(label + "_SliderContainer");
        container.transform.SetParent(parent, false);
        RectTransform containerRt = container.AddComponent<RectTransform>();
        containerRt.sizeDelta = new Vector2(240f, 50f);

        GameObject sliderGo = new GameObject("Slider");
        sliderGo.transform.SetParent(container.transform, false);
        RectTransform sliderRt = sliderGo.AddComponent<RectTransform>();
        sliderRt.anchorMin = new Vector2(0f, 0f);
        sliderRt.anchorMax = new Vector2(1f, 0f);
        sliderRt.pivot = new Vector2(0.5f, 0f);
        sliderRt.anchoredPosition = new Vector2(0f, 8f);
        sliderRt.sizeDelta = new Vector2(0f, 14f);

        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(sliderGo.transform, false);
        RectTransform bgRt = bg.AddComponent<RectTransform>();
        bgRt.anchorMin = new Vector2(0f, 0.4f);
        bgRt.anchorMax = new Vector2(1f, 0.6f);
        bgRt.sizeDelta = Vector2.zero;
        Image bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.1f, 0.15f, 0.22f, 0.9f);

        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderGo.transform, false);
        RectTransform faRt = fillArea.AddComponent<RectTransform>();
        faRt.anchorMin = new Vector2(0f, 0.4f);
        faRt.anchorMax = new Vector2(1f, 0.6f);
        faRt.sizeDelta = Vector2.zero;

        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        RectTransform fillRt = fill.AddComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = new Vector2(defaultVal, 1f);
        fillRt.sizeDelta = Vector2.zero;
        Image fillImg = fill.AddComponent<Image>();
        fillImg.color = terminalCyan;

        GameObject handleArea = new GameObject("Handle Area");
        handleArea.transform.SetParent(sliderGo.transform, false);
        RectTransform haRt = handleArea.AddComponent<RectTransform>();
        haRt.anchorMin = Vector2.zero;
        haRt.anchorMax = Vector2.one;
        haRt.sizeDelta = Vector2.zero;

        GameObject handle = new GameObject("Handle");
        handle.transform.SetParent(handleArea.transform, false);
        RectTransform handleRt = handle.AddComponent<RectTransform>();
        handleRt.anchorMin = new Vector2(defaultVal, 0.5f);
        handleRt.anchorMax = new Vector2(defaultVal, 0.5f);
        handleRt.pivot = new Vector2(0.5f, 0.5f);
        handleRt.sizeDelta = new Vector2(12f, 20f);
        Image handleImg = handle.AddComponent<Image>();
        handleImg.color = Color.white;

        Slider slider = sliderGo.AddComponent<Slider>();
        slider.fillRect = fillRt;
        slider.handleRect = handleRt;
        slider.targetGraphic = handleImg;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = defaultVal;

        // Value text
        CreateText(container.transform, "Label", $"{label}: {defaultVal:F2}", 11, TextAlignmentOptions.Left, new Color(0.8f, 0.85f, 0.9f), new Vector2(240f, 20f), Vector2.zero, font);

        return slider;
    }

    private static void CreateSettingsButton(Transform parent, Vector2 anchoredPos, string goName, string text, Color activeColor, TMP_FontAsset font)
    {
        GameObject btnGo = new GameObject(goName);
        btnGo.transform.SetParent(parent, false);
        RectTransform rt = btnGo.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = new Vector2(180f, 40f);

        Image img = btnGo.AddComponent<Image>();
        img.color = new Color(activeColor.r, activeColor.g, activeColor.b, 0.12f);

        Button button = btnGo.AddComponent<Button>();
        button.targetGraphic = img;
        Navigation nav = button.navigation;
        nav.mode = Navigation.Mode.None;
        button.navigation = nav;

        Outline outline = btnGo.AddComponent<Outline>();
        outline.effectColor = new Color(activeColor.r, activeColor.g, activeColor.b, 0.4f);
        outline.effectDistance = new Vector2(1f, 1f);

        CreateText(btnGo.transform, "Text", text, 12, TextAlignmentOptions.Center, activeColor, Vector2.zero, Vector2.zero, font);

        btnGo.AddComponent<EventTrigger>();
    }
    #endregion
}
