using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public float hp = 15f;
    public GameObject expPrefab;
    public float knockbackDistance = 0.18f;
    public float knockbackDuration = 0.08f;
    public float flashDuration = 0.1f;

    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private EnemyMove movement;
    private float flashRemaining;
    private bool defeated;

    void Awake()
    {
        movement = GetComponent<EnemyMove>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null) originalColor = spriteRenderer.color;
    }

    void Update()
    {
        if (flashRemaining <= 0f) return;
        flashRemaining -= Time.deltaTime;
        if (flashRemaining <= 0f && spriteRenderer != null) spriteRenderer.color = originalColor;
    }

    void OnDisable()
    {
        flashRemaining = 0f;
        if (spriteRenderer != null) spriteRenderer.color = originalColor;
    }

    public void TakeDamage(float damage)
    {
        if (defeated || damage <= 0) return;
        hp -= damage;

        if (hp <= 0)
        {
            defeated = true;
            // 倒されたらその場に経験値をドロップして消滅
            Instantiate(expPrefab, transform.position, Quaternion.identity);
            Destroy(gameObject);
            return;
        }

        if (spriteRenderer != null) {
            spriteRenderer.color = new Color(1f, 0.45f, 0.45f, originalColor.a);
            flashRemaining = flashDuration;
        }
        if (movement != null && movement.player != null) {
            Vector2 direction = transform.position - movement.player.position;
            movement.ApplyKnockback(direction, knockbackDistance, knockbackDuration);
        }
    }
}
