using UnityEngine;

// 宇宙人の通常攻撃：周りの敵がいる方向（上下左右）にビームを出す。
// AlienAutoAttack（試し用シーンの AlienPlayer で使用）と同じ動きを、
// BasicAttack の仕組みで作ったもの。攻撃力・攻撃速度の強化と分身スキルが効く。
public class AlienBeamAttack : BasicAttack
{
    [Header("4方向のビームPrefab")]
    public GameObject beamRightPrefab;
    public GameObject beamLeftPrefab;
    public GameObject beamBackPrefab;
    public GameObject beamFrontPrefab;

    [Header("攻撃設定")]
    [Tooltip("攻撃間隔の倍率（コントローラーの攻撃間隔 × この値。0.45 × 1.78 ≒ 0.8秒）")]
    public float intervalMultiplier = 1.78f;
    [Tooltip("ダメージの基準にする攻撃力。この攻撃力のときにビーム本来のダメージになる")]
    public float referenceAttackPower = 3f;
    public float detectRadius = 5.5f;

    [Header("ビームの発生位置")]
    public float beamOffset = 2.5f;

    [Header("攻撃の中心位置の補正")]
    public Vector2 attackCenterOffset = new Vector2(0.2f, 0f);

    protected override float IntervalMultiplier => intervalMultiplier;

    public override void Fire(Vector3 origin, float opacity, float damageMultiplier = 1f)
    {
        if (player == null) return;

        Vector3 center = origin + (Vector3)attackCenterOffset;

        bool attackRight = false;
        bool attackLeft = false;
        bool attackBack = false;
        bool attackFront = false;

        foreach (GameObject enemy in GameObject.FindGameObjectsWithTag("Enemy"))
        {
            Vector2 direction = enemy.transform.position - center;

            // 攻撃範囲外の敵は無視
            if (direction.magnitude > detectRadius) continue;

            // 縦と横のうち、離れている方の向きを攻撃する
            if (Mathf.Abs(direction.x) >= Mathf.Abs(direction.y))
            {
                if (direction.x >= 0) attackRight = true;
                else attackLeft = true;
            }
            else
            {
                if (direction.y >= 0) attackBack = true;
                else attackFront = true;
            }
        }

        // 攻撃力が上がった分だけビームのダメージも上げる
        float powerRate = referenceAttackPower > 0f
            ? player.EffectiveAttackPower / referenceAttackPower : 1f;
        float multiplier = powerRate * damageMultiplier;

        // 敵がいる方向すべてにビームを出す
        if (attackRight) SpawnBeam(beamRightPrefab, center, Vector3.right, opacity, multiplier);
        if (attackLeft) SpawnBeam(beamLeftPrefab, center, Vector3.left, opacity, multiplier);
        if (attackBack) SpawnBeam(beamBackPrefab, center, Vector3.up, opacity, multiplier);
        if (attackFront) SpawnBeam(beamFrontPrefab, center, Vector3.down, opacity, multiplier);
    }

    void SpawnBeam(GameObject prefab, Vector3 center, Vector3 direction,
        float opacity, float multiplier)
    {
        if (prefab == null) return;

        GameObject beam = Instantiate(prefab, center + direction * beamOffset, Quaternion.identity);

        AlienBeam alienBeam = beam.GetComponent<AlienBeam>();
        if (alienBeam != null) alienBeam.damageMultiplier = multiplier;

        // 分身が出すビームは薄く表示する
        SpectralAppearance.Apply(beam, opacity);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position + (Vector3)attackCenterOffset, detectRadius);
    }
}
