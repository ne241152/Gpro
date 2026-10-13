using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public int hp = 2;
    public GameObject expPrefab;

    public void TakeDamage(int damage)
    {
        hp -= damage;

        if (hp <= 0)
        {
            // 倒されたらその場に経験値をドロップして消滅
            Instantiate(expPrefab, transform.position, Quaternion.identity);
            Destroy(gameObject);
        }
    }
}