using UnityEngine;

public class Explosion : MonoBehaviour
{
    public float radius = 3f;
    public int damage = 10;

    void Start()
    {
        transform.localScale = new Vector3(radius * 2, radius * 2, 1);

        Collider2D[] cols = Physics2D.OverlapCircleAll(transform.position, radius);
        foreach (Collider2D c in cols)
        {
            if (c.CompareTag("Enemy"))
            {
                EnemyHealth enemy = c.GetComponent<EnemyHealth>();
                if (enemy != null)
                {
                    enemy.TakeDamage(damage);
                }
            }
        }
        Destroy(gameObject, 0.3f);
    }
}