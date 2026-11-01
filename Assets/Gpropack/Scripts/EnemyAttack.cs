using UnityEngine;

public class EnemyAttack : MonoBehaviour
{
    public enum EnemyType
    {
        Phishing, //フィッシングの敵
        Normal,  //これからの敵用に一旦置いてます
        Zombie
    }

    [Header("敵の種類")]
    public EnemyType enemyType = EnemyType.Normal;

    [Header("攻撃力")]
    public int damage = 10;

    [Header("攻撃間隔（秒）")]
    public float attackInterval = 1.0f;
    [Min(0f)] public float contactTolerance = 0.08f;

    private float attackTimer = 0f;
    private PlayerHealth playerHealth;
    private Collider2D bodyCollider;
    private Collider2D playerCollider;
    private bool touchingPlayer = false;

    void Start()
    {
        bodyCollider = GetComponent<Collider2D>();
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject == null) return;
        playerHealth = playerObject.GetComponentInParent<PlayerHealth>();
        playerCollider = playerObject.GetComponent<Collider2D>();
    }

    void Update()
    {
        if (Time.timeScale == 0f) return;
        bool inContact = false;
        if (playerHealth != null && bodyCollider != null && playerCollider != null
            && bodyCollider.enabled && playerCollider.enabled)
        {
            ColliderDistance2D gap = bodyCollider.Distance(playerCollider);
            inContact = gap.isValid && gap.distance <= contactTolerance;
        }
        if (!inContact)
        {
            touchingPlayer = false;
            attackTimer = 0f;
            return;
        }
        if (!touchingPlayer)
        {
            touchingPlayer = true;
            attackTimer = 0f;
            AttackPlayer();
            return;
        }

        attackTimer += Time.deltaTime;

        // 攻撃間隔が経過したらダメージ
        if (attackTimer >= attackInterval)
        {
            AttackPlayer();
            attackTimer = 0f;
        }
    }

    // プレイヤーにダメージを与える処理
    void AttackPlayer()
    {
        if (playerHealth == null) return;

        WizardPlayerController player =
            playerHealth.GetComponent<WizardPlayerController>();

        if (player == null) return;

        // フィッシング型の敵で、パスキー取得済みなら攻撃を無効化
        if (enemyType == EnemyType.Phishing && player.hasPasskey)
        {
            Debug.Log("パスキーによってフィッシング攻撃を無効化しました");
            return;
        }

        // 通常のダメージ処理
        playerHealth.TakeDamage(damage);
    }

}
