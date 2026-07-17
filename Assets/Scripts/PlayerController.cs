using UnityEngine;

// Oyuncunun hareketlerini, çevreyle (NPC'lerle) olan fiziksel etkileşimini ve fener kontrolünü sağlayan sınıftır.
public class PlayerController : MonoBehaviour
{
    [Header("Hareket Ayarlari")]
    public float moveSpeed = 5f;

    [Header("Etkilesim Ayarlari")]
    public float interactionRange = 1.5f; // NPC etkileşimi için algılama yarıçapı
    public LayerMask npcLayer;           // Sadece NPC katmanındaki nesneleri algılamak için maske

    [Header("Fener")]
    public GameObject flashlight;

    private Rigidbody2D rb;
    private Vector2 movement;
    private GameObject nearbyNPC;

    void Start()
    {
        // Hedef FPS sınırını belirler ve dikey senkronizasyonu kapatır
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        // W,A,S,D veya yön tuşlarından yatay ve dikey hareket girdilerini alır
        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");

        // Yakındaki NPC'leri tarar
        CheckNearbyNPC();

        // Eğer yakınlarda bir NPC varsa ve E tuşuna basılırsa etkileşim menüsünü açar
        if (Input.GetKeyDown(KeyCode.E) && nearbyNPC != null)
        {
            Interact();
        }

        // L tuşuna basarak feneri açar/kapatır
        if (Input.GetKeyDown(KeyCode.L))
        {
            if (flashlight != null)
            {
                flashlight.SetActive(!flashlight.activeSelf);
            }
        }
    }

    void FixedUpdate()
    {
        // Fizik tabanlı hareketi FixedUpdate altında rigidbody yardımıyla gerçekleştirir (hız dalgalanmalarını engeller)
        rb.MovePosition(rb.position + movement.normalized * moveSpeed * Time.fixedDeltaTime);
    }

    // Oyuncunun etrafında dairesel bir alan tarayarak yakındaki NPC nesnelerini bulur (Physics2D OverlapCircle)
    void CheckNearbyNPC()
    {
        Collider2D hit = Physics2D.OverlapCircle(transform.position, interactionRange, npcLayer);

        if (hit != null)
        {
            nearbyNPC = hit.gameObject;
        }
        else
        {
            nearbyNPC = null;
        }
    }

    // Arayüz yöneticisine etkileşime girilen NPC nesnesini göndererek menüyü tetikler
    void Interact()
    {
        UIManager.Instance.ShowInteractionMenu(nearbyNPC);
    }

    // Unity Editöründe etkileşim menzilini görselleştirmek için çizim yapar (sarı çember)
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }
}