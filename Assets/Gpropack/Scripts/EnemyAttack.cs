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

    private float attackTimer = 0f;
    private PlayerHealth playerHealth;
    private bool touchingPlayer = false;

    void Update()
    {
        // プレイヤーに触れていなければ攻撃しない
        if (!touchingPlayer || playerHealth == null)
            return;

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

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            playerHealth =
                collision.gameObject.GetComponentInParent<PlayerHealth>();

            if (playerHealth != null)
            {
                Debug.Log("敵がプレイヤーに接触！攻撃します");

                touchingPlayer = true;

                AttackPlayer();

                attackTimer = 0f;
            }
            else
            {
            Debug.LogWarning("PlayerHealthが見つかりません！");
            }
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            touchingPlayer = false;
            playerHealth = null;
            attackTimer = 0f;
        }
    }
}