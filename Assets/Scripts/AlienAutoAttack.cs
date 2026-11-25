
using UnityEngine;

public class AlienAutoAttack : MonoBehaviour
{
    [Header("4方向のビームPrefab")]
    public GameObject beamRightPrefab;
    public GameObject beamLeftPrefab;
    public GameObject beamBackPrefab;
    public GameObject beamFrontPrefab;

    [Header("攻撃設定")]
    public float attackInterval = 0.8f;
    public float detectRadius = 5f;

    [Header("ビームの発生位置")]
    public float beamOffset = 2.5f;

    private float timer = 0f;

    [Header("攻撃の中心位置の補正")]
    public Vector2 attackCenterOffset = Vector2.zero;

    Vector3 AttackCenter =>
        transform.position + (Vector3)attackCenterOffset;

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= attackInterval)
        {
            timer = 0f;
            Attack();
        }
    }

    void Attack()
    {
        GameObject[] enemies =
            GameObject.FindGameObjectsWithTag("Enemy");

        bool attackRight = false;
        bool attackLeft = false;
        bool attackBack = false;
        bool attackFront = false;

        foreach (GameObject enemy in enemies)
        {
            Vector2 direction =
                enemy.transform.position - AttackCenter;

            // 攻撃範囲外の敵は無視
            if (direction.magnitude > detectRadius)
                continue;

            // 左右方向の判定
            if (Mathf.Abs(direction.x) >= Mathf.Abs(direction.y))
            {
                if (direction.x >= 0)
                    attackRight = true;
                else
                    attackLeft = true;
            }
            // 上下方向の判定
            else
            {
                if (direction.y >= 0)
                    attackBack = true;
                else
                    attackFront = true;
            }
        }

        // 敵がいる方向すべてにビームを発射
        if (attackRight)
            SpawnBeam(beamRightPrefab, Vector3.right);

        if (attackLeft)
            SpawnBeam(beamLeftPrefab, Vector3.left);

        if (attackBack)
            SpawnBeam(beamBackPrefab, Vector3.up);

        if (attackFront)
            SpawnBeam(beamFrontPrefab, Vector3.down);
    }

   void SpawnBeam(GameObject prefab, Vector3 direction)
    {
        if (prefab == null)
            return;

        Vector3 spawnPosition =
            AttackCenter + direction * beamOffset;

        Instantiate(prefab, spawnPosition, Quaternion.identity);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(AttackCenter, detectRadius);
    }
}
