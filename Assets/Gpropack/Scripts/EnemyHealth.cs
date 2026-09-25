using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public int hp = 2;

    public void TakeDamage(int damage)
    {
        hp -= damage;

        if (hp <= 0)
        {
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            PlayerHealth health = collision.GetComponent<PlayerHealth>();

            if (health != null)
            {
            health.TakeDamage(10);
            }
    }
    }
}