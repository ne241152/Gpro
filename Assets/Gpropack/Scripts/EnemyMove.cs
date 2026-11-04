using UnityEngine;

public class EnemyMove : MonoBehaviour
{
    [Header("追いかけるプレイヤー")]
    public Transform player;
    [System.NonSerialized] public GameManager spawnManager;

    [Header("移動速度")]
    public float moveSpeed = 1.0f;

    private Rigidbody2D rb;
    private Collider2D bodyCollider;
    private Collider2D playerCollider;

    // プレイヤーに接触しているか
    private bool touchingPlayer = false;

    [Header("移動のカクカク感")]
    public float moveInterval = 0.05f;

    [Header("敵同士の押し合い")]
    public float separationDistance = 3.6f;
    public float separationStrength = 4f;
    public float separationCheckInterval = 0.05f;
    [Min(0f)] public float enemySpacing = 2.6f;

    [Header("遠く離れた敵の整理")]
    [Min(0f)] public float despawnDistance = 27f;
    [Min(0f)] public float despawnDelay = 5f;

    private float distantTimer;

    private readonly Collider2D[] nearbyColliders = new Collider2D[128];
    private Vector2 separation;
    private float chaseClearance = 1f;
    private float separationTimer;
    private int separationId;
    private static int nextSeparationId;

    private float moveTimer = 0f;
    private Vector2 knockbackVelocity;
    private float knockbackRemaining;

    public void ApplyKnockback(Vector2 direction, float distance, float duration)
    {
        if (duration <= 0f) return;
        knockbackVelocity = direction.normalized * distance / duration;
        knockbackRemaining = duration;
        moveTimer = 0f;
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<Collider2D>();
        separationId = ++nextSeparationId;
        separationTimer = Random.Range(0f, Mathf.Max(0.02f, separationCheckInterval));
    }

    void Start()
    {
        if (player == null)
        {
            GameObject playerObject =
                GameObject.FindGameObjectWithTag("Player");

            if (playerObject != null)
            {
                player = playerObject.transform;
            }
            else
            {
                Debug.LogWarning(
                    "EnemyMove: Playerタグのオブジェクトが見つかりません！"
                );
            }
        }
    }

    void LateUpdate()
    {
        if (player == null) return;
        Vector2 offset = transform.position - player.position;
        if (offset.sqrMagnitude < despawnDistance * despawnDistance)
        {
            distantTimer = 0f;
            return;
        }

        distantTimer += Time.deltaTime;
        if (distantTimer >= despawnDelay)
        {
            // 経験値を落とす死亡とは分け、追いつけなくなった敵だけ即補充する。
            gameObject.SetActive(false);
            if (spawnManager != null) spawnManager.ReplaceDistantEnemy(gameObject);
            Destroy(gameObject);
        }
    }

    void FixedUpdate()
    {
        if (rb != null && knockbackRemaining > 0f) {
            float step = Mathf.Min(Time.fixedDeltaTime, knockbackRemaining);
            MoveWithoutPushingPlayer(knockbackVelocity * step);
            knockbackRemaining -= step;
            return;
        }

        if (player == null || rb == null)
            return;

        moveTimer += Time.fixedDeltaTime;
        separationTimer -= Time.fixedDeltaTime;

        // 一定時間経つまでは動かさない
        if (moveTimer < moveInterval)
            return;

        if (separationTimer <= 0f) {
            UpdateSeparation();
            separationTimer = Mathf.Max(0.02f, separationCheckInterval);
        }

        Vector2 direction =
            ((Vector2)player.position - rb.position).normalized;

        // Allow sideways separation even while touching the player, without speeding up pursuit.
        float effectiveSpeed = moveSpeed * (spawnManager != null ? spawnManager.EnemyMoveSpeedMultiplier : 1f);
        Vector2 chase = touchingPlayer ? Vector2.zero : direction * effectiveSpeed * chaseClearance;
        Vector2 velocity = chase + separation * effectiveSpeed * Mathf.Max(0f, separationStrength);
        velocity = Vector2.ClampMagnitude(velocity, Mathf.Max(0f, effectiveSpeed));
        MoveWithoutPushingPlayer(velocity * moveTimer);

        moveTimer = 0f;

    }

    void MoveWithoutPushingPlayer(Vector2 displacement)
    {
        if (player != null && bodyCollider != null)
        {
            if (playerCollider == null) playerCollider = player.GetComponent<Collider2D>();
            if (playerCollider != null && playerCollider.enabled && bodyCollider.enabled)
            {
                ColliderDistance2D gap = bodyCollider.Distance(playerCollider);
                if (gap.isValid)
                {
                    // 接触面の軸を敵から主人公へ向け、接線方向の移動は残す。
                    Vector2 towardPlayer = gap.normal;
                    Vector2 centerDelta = playerCollider.bounds.center - bodyCollider.bounds.center;
                    if (Vector2.Dot(towardPlayer, centerDelta) < 0f) towardPlayer = -towardPlayer;
                    float inwardMovement = Vector2.Dot(displacement, towardPlayer);
                    float allowedMovement = Mathf.Max(0f, gap.distance);
                    if (inwardMovement > allowedMovement)
                        displacement -= towardPlayer * (inwardMovement - allowedMovement);

                    // 主人公の移動などで重なった場合は敵側を外へ戻す。
                    if (gap.isOverlapped)
                        displacement += towardPlayer * gap.distance;
                }
            }
        }
        rb.MovePosition(rb.position + displacement);
    }

    void UpdateSeparation()
    {
        separation = Vector2.zero;
        chaseClearance = 1f;
        if (separationDistance <= 0f || separationStrength <= 0f) return;
        // Ignore trigger projectiles and pickups; the fixed buffer bounds work in dense crowds.
        ContactFilter2D filter = new ContactFilter2D { useTriggers = false };
        int count = Physics2D.OverlapCircle(rb.position, separationDistance, filter, nearbyColliders);
        Vector2 chaseDirection = player != null
            ? ((Vector2)player.position - rb.position).normalized : Vector2.zero;
        for (int i = 0; i < count; i++) {
            Collider2D other = nearbyColliders[i];
            Rigidbody2D otherBody = other.attachedRigidbody;
            if (otherBody == null || otherBody == rb || !otherBody.CompareTag("Enemy")) continue;
            EnemyMove neighbor = otherBody.GetComponent<EnemyMove>();
            if (neighbor == null) continue;
            Vector2 away = rb.position - otherBody.position;
            float distance = away.magnitude;
            if (distance >= separationDistance) continue;
            // 前列へ追突し続けないよう、敵が進行方向を塞ぐほど追跡を弱める。
            if (distance <= 0.001f || Vector2.Dot(-away / distance, chaseDirection) > 0.5f)
                chaseClearance = Mathf.Min(chaseClearance,
                    Mathf.Clamp01((distance - enemySpacing) / Mathf.Max(0.1f, separationDistance - enemySpacing)));
            if (distance > 0.001f) {
                away /= distance;
            } else {
                // An exactly overlapping pair must receive opposite, repeatable directions.
                int low = Mathf.Min(separationId, neighbor.separationId);
                int high = Mathf.Max(separationId, neighbor.separationId);
                int hash = unchecked(low * 73856093 ^ high * 19349663);
                float angle = (hash & 0x7fffffff) % 360 * Mathf.Deg2Rad;
                away = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                if (separationId < neighbor.separationId) away = -away;
            }
            separation += away * (1f - distance / separationDistance);
        }
        separation = Vector2.ClampMagnitude(separation, 1f);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            touchingPlayer = true;
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            touchingPlayer = false;
        }
    }
}
