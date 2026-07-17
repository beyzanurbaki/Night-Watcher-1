using UnityEngine;

// Sokak lambalarının gece aktif olup parlamasını, gündüz ise sönmesini sağlayan;
// gerekirse kod tarafında prosedürel olarak ışık parlaması (radial glow) sprite'ı üreten sınıftır.
public class StreetLightController : MonoBehaviour
{
    [Header("Bulb Glow Settings (Fallback)")]
    public Vector3 bulbOffset = new Vector3(1.2f, 3.3f, 0f);
    public Vector3 bulbScale = new Vector3(0.6f, 0.6f, 1f);
    public Color bulbColor = new Color(1f, 0.95f, 0.7f, 0.7f);

    [Header("Ground Glow Settings (Fallback)")]
    public Vector3 groundOffset = new Vector3(1.2f, 0.2f, 0f);
    public Vector3 groundScale = new Vector3(2.5f, 1.5f, 1f);
    public Color groundColor = new Color(1f, 0.9f, 0.5f, 0.25f);

    private GameObject bulbGlowObject;
    private GameObject groundGlowObject;
    private SpriteRenderer mainRenderer;

    void Start()
    {
        mainRenderer = GetComponent<SpriteRenderer>();
        
        // Hiyerarşide önceden el ile oluşturulmuş kalıcı ışık nesnelerini arar
        Transform bulbT = transform.Find("BulbGlow");
        if (bulbT != null)
        {
            bulbGlowObject = bulbT.gameObject;
        }

        Transform groundT = transform.Find("GroundGlow");
        if (groundT != null)
        {
            groundGlowObject = groundT.gameObject;
        }

        // Eğer sahne editöründe el ile oluşturulmamışlarsa, kod tarafında otomatik üretir (Fallback)
        if (bulbGlowObject == null || groundGlowObject == null)
        {
            InitializeLightGlows();
        }
        else
        {
            // Zaman yöneticisine göre ışıkların anlık görünürlüğünü senkronize eder
            if (TimeManager.Instance != null)
            {
                bool isNight = TimeManager.Instance.isNightActive;
                bulbGlowObject.SetActive(isNight);
                groundGlowObject.SetActive(isNight);
            }
        }
    }

    void Update()
    {
        // Zaman yöneticisinden anlık durumu alıp lambanın aktifliğini günceller
        if (TimeManager.Instance != null)
        {
            bool isNight = TimeManager.Instance.isNightActive;
            
            if (bulbGlowObject != null && bulbGlowObject.activeSelf != isNight)
            {
                bulbGlowObject.SetActive(isNight);
            }
            
            if (groundGlowObject != null && groundGlowObject.activeSelf != isNight)
            {
                groundGlowObject.SetActive(isNight);
            }
        }
    }

    // Kod tarafında ışık parlaması nesnelerini (ampul ve zemin aydınlatması) oluşturan metot
    void InitializeLightGlows()
    {
        bool isFlipped = mainRenderer != null && mainRenderer.flipX;
        float xBulb = isFlipped ? -bulbOffset.x : bulbOffset.x;
        float xGround = isFlipped ? -groundOffset.x : groundOffset.x;

        // Fener sprite'ını kaynak olarak aramaya çalışır, yoksa prosedürel radyal sprite üretir
        Sprite glowSprite = FindGlowSprite();
        if (glowSprite == null)
        {
            glowSprite = CreateRadialGlowSprite();
        }

        // Ampul parlamasını oluşturur
        if (bulbGlowObject == null)
        {
            bulbGlowObject = CreateGlowChild("BulbGlow", new Vector3(xBulb, bulbOffset.y, bulbOffset.z), bulbScale, bulbColor, glowSprite, 14);
        }

        // Zemin parlamasını oluşturur
        if (groundGlowObject == null)
        {
            groundGlowObject = CreateGlowChild("GroundGlow", new Vector3(xGround, groundOffset.y, groundOffset.z), groundScale, groundColor, glowSprite, 13);
        }

        if (TimeManager.Instance != null)
        {
            bool isNight = TimeManager.Instance.isNightActive;
            bulbGlowObject.SetActive(isNight);
            groundGlowObject.SetActive(isNight);
        }
    }

    // Işık parıltısı için yeni bir alt nesne (child) oluşturup SpriteRenderer bağlayan yardımcı metot
    GameObject CreateGlowChild(string name, Vector3 localPos, Vector3 localScale, Color color, Sprite sprite, int sortingOrder)
    {
        GameObject glowGo = new GameObject(name);
        glowGo.transform.SetParent(this.transform);
        glowGo.transform.localPosition = localPos;
        glowGo.transform.localRotation = Quaternion.identity;
        glowGo.transform.localScale = localScale;

        SpriteRenderer sr = glowGo.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        
        // Ana lamba görseliyle aynı katmanda render edilmesini sağlar
        if (mainRenderer != null)
        {
            sr.sortingLayerID = mainRenderer.sortingLayerID;
            sr.sortingLayerName = mainRenderer.sortingLayerName;
        }
        else
        {
            sr.sortingLayerName = "Default";
        }
        sr.sortingOrder = sortingOrder;

        return glowGo;
    }

    // Oyuncunun fenerinde kullanılan ışık sprite'ını referans olarak bulmaya çalışan fonksiyon
    Sprite FindGlowSprite()
    {
        PlayerController player = FindObjectOfType<PlayerController>();
        if (player != null && player.flashlight != null)
        {
            SpriteRenderer sr = player.flashlight.GetComponent<SpriteRenderer>();
            if (sr == null)
            {
                sr = player.flashlight.GetComponentInChildren<SpriteRenderer>();
            }
            if (sr != null && sr.sprite != null)
            {
                return sr.sprite;
            }
        }
        return null;
    }

    // Bilgisayarda anlık olarak (Procedural) piksel piksel yumuşak geçişli bir radyal parıltı dokusu ve sprite'ı oluşturur.
    Sprite CreateRadialGlowSprite()
    {
        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float maxDist = size / 2f;
        
        // Merkezden uzaklaştıkça opaklığı azaltarak dairesel ışık efekti üretir
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                float t = Mathf.Clamp01(dist / maxDist);
                float alpha = Mathf.SmoothStep(1f, 0f, t); // Yumuşak geçiş formülü
                
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }
}
