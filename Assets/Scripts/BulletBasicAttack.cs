using UnityEngine;

// 魔法使いの通常攻撃：一番近い敵に弾を撃つ。
// 弾のプレハブと攻撃力はコントローラー（WizardPlayerController）の設定を使う。
public class BulletBasicAttack : BasicAttack
{
    public override void Fire(Vector3 origin, float opacity, float damageMultiplier = 1f)
    {
        if (player == null || player.bulletPrefab == null) return;

        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        GameObject nearest = null;
        float minDist = Mathf.Infinity;

        foreach (var e in enemies) {
            float dist = Vector2.Distance(origin, e.transform.position);
            if (dist < minDist) { minDist = dist; nearest = e; }
        }

        if (nearest != null) {
            GameObject b = Instantiate(player.bulletPrefab, origin, Quaternion.identity);
            SpectralAppearance.Apply(b, opacity);
            b.GetComponent<Bullet>().damage = player.EffectiveAttackPower * damageMultiplier;
            Vector2 dir = (nearest.transform.position - origin).normalized;
            b.GetComponent<Rigidbody2D>().linearVelocity = dir * 10f;
        }
    }
}
