
using UnityEngine;

public class EnemyAttack : MonoBehaviour
{
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
            playerHealth.TakeDamage(damage);
            attackTimer = 0f;
        }
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

                playerHealth.TakeDamage(damage);

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