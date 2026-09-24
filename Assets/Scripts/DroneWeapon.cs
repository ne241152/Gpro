using UnityEngine;

public class DroneWeapon : MonoBehaviour
{
    public Transform player;
    public GameObject bulletPrefab;

    public float radius = 2f;
    public float rotateSpeed = 120f;

    //攻撃系
    public float attackInterval = 1.0f;//何秒ごとに撃つか
    private float attackTimer = 0f;

    float angle;

    void Update()
    {
        if (player == null) return;

        angle += rotateSpeed * Time.deltaTime;

        float x = Mathf.Cos(angle * Mathf.Deg2Rad) * radius;
        float y = Mathf.Sin(angle * Mathf.Deg2Rad) * radius;

        transform.position = player.position + new Vector3(x, y, 0);

        attackTimer += Time.deltaTime;
        if (attackTimer >= attackInterval)
        {
            FireDrone();
            attackTimer = 0f;
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
        if (nearest == null)
            return;

        GameObject b = Instantiate(bulletPrefab, transform.position, Quaternion.identity);
        Vector2 dir =(nearest.transform.position - transform.position).normalized;
        b.GetComponent<Rigidbody2D>().linearVelocity = dir * 10f;
        b.GetComponent<Bullet>().damage = 1;
    }
}