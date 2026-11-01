using UnityEngine;

public class Meteor : MonoBehaviour
{
    public float fallSpeed = 6f;
    public float lifeTime = 2f;
    public int damage = 3;
    public float damageMultiplier = 1f;
    [Min(0f)] public float hitDelay = 0.2f;

    private float elapsedTime;

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        transform.position += Vector3.down * fallSpeed * Time.deltaTime;
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        TryHit(collision);
    }

    void OnTriggerStay2D(Collider2D collision)
    {
        TryHit(collision);
    }

    void FixedUpdate()
    {
        elapsedTime += Time.fixedDeltaTime;
    }

    private bool hasHit;

    void TryHit(Collider2D collision)
    {
        if (hasHit || elapsedTime < hitDelay) return;
        if (!collision.CompareTag("Enemy")) return;

        hasHit = true;
        EnemyHealth enemyHealth = collision.GetComponent<EnemyHealth>();
        if (enemyHealth != null)
        {
            enemyHealth.TakeDamage(damage * damageMultiplier);
        }

        Destroy(gameObject);
    }
}
