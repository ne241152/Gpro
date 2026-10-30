using UnityEngine;

public class DroneWeapon : MonoBehaviour
{
    public Transform player;
    public GameObject bulletPrefab;

    public float radius = 3f;
    public float rotateSpeed = 60f;
    public float bulletSpeed = 20f;
    public float playerWidthRatio = 1f / 3f;
    public Sprite leftSprite;
    public Sprite rightSprite;
    public float visualOpacity = 1f;

    private SpriteRenderer droneRenderer;

    void Start()
    {
        nextAttackInterval = RandomAttackInterval();
        droneRenderer = GetComponent<SpriteRenderer>();
        if (player == null || droneRenderer == null || droneRenderer.sprite == null) return;
        Transform visual = player.Find("WizardSprite");
        SpriteRenderer playerRenderer = visual != null ? visual.GetComponent<SpriteRenderer>() : null;
        if (playerRenderer == null) return;

        float width = droneRenderer.sprite.bounds.size.x;
        if (width > 0f) {
            float scale = playerRenderer.bounds.size.x * playerWidthRatio / width;
            transform.localScale = new Vector3(scale, scale, 1f);
        }
    }

    //攻撃系
    public float minAttackInterval = 0.6f;
    public float maxAttackInterval = 1.4f;
    private float attackTimer = 0f;
    private float nextAttackInterval;

    float RandomAttackInterval()
    {
        float minimum = Mathf.Max(0.05f, minAttackInterval);
        return Random.Range(minimum, Mathf.Max(minimum, maxAttackInterval));
    }

    float angle;

    void Update()
    {
        if (player == null) return;

        angle += rotateSpeed * Time.deltaTime;

        float x = Mathf.Cos(angle * Mathf.Deg2Rad) * radius;
        float y = Mathf.Sin(angle * Mathf.Deg2Rad) * radius;

        transform.position = player.position + new Vector3(x, y, 0);

        attackTimer += Time.deltaTime;
        if (attackTimer >= nextAttackInterval)
        {
            FireDrone();
            attackTimer = 0f;
            nextAttackInterval = RandomAttackInterval();
        }
    }

    void FireDrone()//transform.positionがDroneになっている
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        GameObject nearest = null;
        float minDist = Mathf.Infinity;
        foreach (GameObject e in enemies)
        {
            float dist = Vector2.Distance(transform.position, e.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = e;
            }
        }
        if (nearest == null || bulletPrefab == null)
            return;

        Vector2 dir =(nearest.transform.position - transform.position).normalized;
        if (droneRenderer != null) {
            Sprite facing = dir.x < 0 ? leftSprite : rightSprite;
            if (facing != null) droneRenderer.sprite = facing;
        }
        GameObject b = Instantiate(bulletPrefab, transform.position, Quaternion.identity);
        SpectralAppearance.Apply(b, visualOpacity);
        b.GetComponent<Rigidbody2D>().linearVelocity = dir * bulletSpeed;
        b.GetComponent<Bullet>().damage = 1;
    }
}
