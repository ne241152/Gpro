using UnityEngine;

public class Bullet : MonoBehaviour
{
    public int damage = 1;

    void Start() { Destroy(gameObject, 3.0f); } // 画面外に飛んでいった弾を3秒後に自動消滅

    void OnTriggerEnter2D(Collider2D col)
    {
        // 当たった相手がEnemyタグを持っていたら
        if (col.CompareTag("Enemy")) {
            col.GetComponent<EnemyHealth>().TakeDamage(damage); // 敵にdamageを与える
            Destroy(gameObject); // 弾自身は消える
        }
    }
}