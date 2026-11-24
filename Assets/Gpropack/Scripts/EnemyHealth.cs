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
    private bool invulnerable;//第二形態移行中の無敵

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
        if (defeated || invulnerable || damage <= 0) return;
        GameManager spawnManager = movement != null ? movement.spawnManager : null;
        if (spawnManager != null && spawnManager.IsOneHitKillActive)
            hp = 0f;
        else
            hp -= damage;

        if (spriteRenderer != null) {
            spriteRenderer.color = new Color(1f, 0.45f, 0.45f, originalColor.a);
            flashRemaining = flashDuration;
        }

        if (hp <= 0)
        {
            //ボスは通常の死亡処理を行わず、第二形態へ移行する
            BossController boss = GetComponent<BossController>();
            if (boss != null && boss.BeginSecondPhase())
            {
                invulnerable = true;
                return;
            }

            //第二形態のボスを倒したらクリア画面を表示する
            if (boss != null)
            {
                UIManager uiManager = FindFirstObjectByType<UIManager>();

                if (uiManager != null)
                {
                    uiManager.ShowResult(true);
                }
            }

            defeated = true;
            // 被弾表示だけ短く残し、死亡時点で移動・接触ダメージ・衝突を止める。
            if (movement != null) movement.enabled = false;
            EnemyAttack attack = GetComponent<EnemyAttack>();
            if (attack != null) attack.enabled = false;
            foreach (Collider2D body in GetComponentsInChildren<Collider2D>()) body.enabled = false;
            Rigidbody2D rigidBody = GetComponent<Rigidbody2D>();
            if (rigidBody != null) rigidBody.simulated = false;
            if (expPrefab != null) Instantiate(expPrefab, transform.position, Quaternion.identity);
            Destroy(gameObject, Mathf.Max(0.01f, flashDuration));
            return;
        }

        if (movement != null && movement.player != null) {
            BossController boss = GetComponent<BossController>();

            // ボス以外にだけヒットバックを適用
            if(boss == null)
            {
                Vector2 direction = transform.position - movement.player.position;
                movement.ApplyKnockback(direction, knockbackDistance, knockbackDuration);
            }
        }
    }

    //第二形態への移行中はダメージを受け付けない
    public void SetInvulnerable(bool value)
    {
        invulnerable = value;
    }

    //ボスのHPを設定する
    public void SetHP(float value)
    {
        hp = value;
    }
}
