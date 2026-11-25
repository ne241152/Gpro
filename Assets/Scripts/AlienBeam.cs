
using UnityEngine;
using System.Collections.Generic;

public class AlienBeam : MonoBehaviour
{
    [Header("ビームの表示時間")]
    public float lifeTime = 0.5f;

    [Header("攻撃力")]
    public int damage = 3;
    public float damageMultiplier = 1f;

    // 攻撃済みの敵を記録する
    private HashSet<EnemyHealth> hitEnemies =
        new HashSet<EnemyHealth>();

    void Start()
    {
        // 一定時間が経過したらビームを消す
        Destroy(gameObject, lifeTime);
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        TryHit(collision);
    }

    void OnTriggerStay2D(Collider2D collision)
    {
        TryHit(collision);
    }

    void TryHit(Collider2D collision)
    {
        if (!collision.CompareTag("Enemy")) return;

        EnemyHealth enemyHealth =
            collision.GetComponentInParent<EnemyHealth>();

        if (enemyHealth == null) return;

        // 同じ敵には1回だけダメージを与える
        if (hitEnemies.Contains(enemyHealth)) return;

        hitEnemies.Add(enemyHealth);

        enemyHealth.TakeDamage(damage * damageMultiplier);
    }
}
