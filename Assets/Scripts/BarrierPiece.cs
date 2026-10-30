using System.Collections.Generic;
using UnityEngine;

public class BarrierPiece : MonoBehaviour
{
    [Header("バリア設定")]
    public int durability = 5;
    public float contactDamage = 2f;
    public float knockbackDistance = 0.9f;
    public float knockbackDuration = 0.12f;
    public float hitCooldown = 0.5f;
    [Range(0f, 1f)] public float opacity = 0.3f;
    public float hitFlashDuration = 0.22f;

    private LinkBarrierController parentBarrier;
    private SpriteRenderer visual;
    private Sprite shieldSprite;
    private SpriteRenderer hitGlow;
    private float flashRemaining;
    private readonly Dictionary<EnemyHealth, float> nextHitTimes = new Dictionary<EnemyHealth, float>();

    public void Initialize(LinkBarrierController parent)
    {
        parentBarrier = parent;
        visual = GetComponent<SpriteRenderer>();
    }

    public void ConfigureVisual(Texture2D texture, Rect sourceRect, bool horizontal)
    {
        if (texture == null || visual == null) return;
        Rect rect = new Rect(sourceRect.x * texture.width / 1333f, sourceRect.y * texture.height / 1180f,
            sourceRect.width * texture.width / 1333f, sourceRect.height * texture.height / 1180f);
        shieldSprite = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        visual.sprite = shieldSprite;
        visual.color = new Color(1f, 1f, 1f, opacity);
        visual.sortingOrder = 20;
        GameObject glow = new GameObject("ShieldHitGlow");
        glow.transform.SetParent(transform, false);
        glow.transform.localScale = new Vector3(1.06f, 1.12f, 1f);
        hitGlow = glow.AddComponent<SpriteRenderer>();
        hitGlow.sprite = shieldSprite;
        hitGlow.sharedMaterial = visual.sharedMaterial;
        hitGlow.sortingLayerID = visual.sortingLayerID;
        hitGlow.sortingOrder = 21;
        hitGlow.enabled = false;
        Vector2 size = shieldSprite.bounds.size;
        Vector2 target = horizontal ? new Vector2(14f, 2.4f) : new Vector2(2.4f, 14f);
        transform.localScale = new Vector3(target.x / size.x, target.y / size.y, 1f);
        BoxCollider2D hitbox = GetComponent<BoxCollider2D>();
        if (hitbox != null) {
            hitbox.size = size;
            hitbox.offset = Vector2.zero;
        }
    }

    void Update()
    {
        if (flashRemaining <= 0f || parentBarrier == null || parentBarrier.IsBroken) return;
        flashRemaining -= Time.deltaTime;
        float flash = Mathf.Clamp01(flashRemaining / Mathf.Max(0.01f, hitFlashDuration));
        if (visual != null) visual.color = new Color(1f, 1f, 1f, Mathf.Lerp(opacity, 1f, flash));
        if (hitGlow != null) {
            hitGlow.enabled = flash > 0f;
            hitGlow.color = new Color(0.7f, 1f, 1f, flash * 0.7f);
        }
    }

    void OnCollisionEnter2D(Collision2D collision) { Hit(collision.gameObject); }
    void OnCollisionStay2D(Collision2D collision) { Hit(collision.gameObject); }

    void Hit(GameObject other)
    {
        if (parentBarrier == null || !parentBarrier.IsProtecting) return;
        EnemyHealth enemy = other.GetComponentInParent<EnemyHealth>();
        if (enemy == null || !enemy.CompareTag("Enemy") || enemy.hp <= 0f) return;
        if (nextHitTimes.TryGetValue(enemy, out float next) && Time.time < next) return;
        nextHitTimes[enemy] = Time.time + Mathf.Max(0.05f, hitCooldown);

        enemy.TakeDamage(contactDamage);
        EnemyMove movement = enemy.GetComponent<EnemyMove>();
        if (movement != null && enemy.hp > 0f) {
            Vector2 direction = enemy.transform.position - parentBarrier.transform.position;
            movement.ApplyKnockback(direction, knockbackDistance, knockbackDuration);
        }
        if (visual != null) visual.color = Color.white;
        flashRemaining = Mathf.Max(0.01f, hitFlashDuration);
        if (hitGlow != null) {
            hitGlow.enabled = true;
            hitGlow.color = new Color(0.7f, 1f, 1f, 0.7f);
        }
        durability--;
        if (durability <= 0) parentBarrier.BreakAll();
    }

    void OnDestroy()
    {
        if (shieldSprite != null) Destroy(shieldSprite);
    }
}
