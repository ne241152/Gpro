using UnityEngine;

public class Bullet : MonoBehaviour
{
    public int damage = 1;
    public Sprite[] animationFrames;
    public float frameInterval = 0.06f;
    public int loopStartFrame = 5;

    private SpriteRenderer spriteRenderer;
    private int frameIndex;
    private float frameTimer;

    void Start()
    {
        Destroy(gameObject, 3.0f);
        spriteRenderer = GetComponent<SpriteRenderer>();
        Rigidbody2D body = GetComponent<Rigidbody2D>();
        if (body != null && body.linearVelocity.sqrMagnitude > 0f) {
            body.rotation = Mathf.Atan2(body.linearVelocity.y, body.linearVelocity.x) * Mathf.Rad2Deg;
        }
        if (spriteRenderer != null && animationFrames != null && animationFrames.Length > 0) {
            spriteRenderer.sprite = animationFrames[0];
        }
    }

    void Update()
    {
        if (spriteRenderer == null || animationFrames == null || animationFrames.Length < 2) return;

        frameTimer += Time.deltaTime;
        float interval = Mathf.Max(0.01f, frameInterval);
        while (frameTimer >= interval) {
            frameTimer -= interval;
            frameIndex++;
            if (frameIndex >= animationFrames.Length) {
                frameIndex = Mathf.Clamp(loopStartFrame, 0, animationFrames.Length - 1);
            }
            spriteRenderer.sprite = animationFrames[frameIndex];
        }
    }

    void OnTriggerEnter2D(Collider2D col)
    {
        // 当たった相手がEnemyタグを持っていたら
        if (col.CompareTag("Enemy")) {
            col.GetComponent<EnemyHealth>().TakeDamage(damage); // 敵にdamageを与える
            Destroy(gameObject); // 弾自身は消える
        }
    }
}
