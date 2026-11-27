using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 宇宙人の通常攻撃：範囲内で一番近い敵に稲妻を放ち、そこから 1 → 2 → 4 → 8 体と確率で伝播させる。
// 稲妻は相手の方向に向き、距離に合わせて伸び縮みする。当たり判定は見た目の稲妻に沿った細い帯。
// 攻撃力・攻撃速度の強化と分身スキルが効く。
// （試し用シーンの AlienPlayer は、別の AlienAutoAttack と AlienBeam を使っている）
public class AlienBeamAttack : BasicAttack
{
    [Header("稲妻の画像")]
    [Tooltip("伸びていく順のコマ。左端が発射位置（Pivot を左端中央にしておく）")]
    public Sprite[] boltFrames;
    [Tooltip("各コマで稲妻の先端がどこまで届いているか（最後のコマを 1 とした割合）。当たり判定の長さに使う")]
    public float[] frameReach = { 0.22f, 0.36f, 0.54f, 0.6f, 0.64f, 0.69f, 1f };
    public Material boltMaterial;
    public int sortingOrder = 10;

    [Header("攻撃設定")]
    [Tooltip("攻撃間隔の倍率（コントローラーの攻撃間隔 × この値。0.45 × 1.78 ≒ 0.8秒）")]
    public float intervalMultiplier = 1.78f;
    [Tooltip("ダメージの基準にする攻撃力。この攻撃力のときに damage の値になる")]
    public float referenceAttackPower = 3f;
    public float damage = 5.5f;
    [Tooltip("最初に狙う敵を探す範囲")]
    public float detectRadius = 5.5f;

    [Header("伝播")]
    [Tooltip("1体から次に伝わる数（2 なら 1 → 2 → 4）")]
    public int branchCount = 2;
    [Tooltip("最初の1体を含めた段の数")]
    public int chainStages = 4;
    [Tooltip("当たった敵から、次の敵を探す範囲")]
    public float chainRadius = 4f;
    [Tooltip("次の敵へ伝わる確率（1本ごとに判定）。1つ目が 1段目→2段目、2つ目が 2段目→3段目、3つ目が 3段目→4段目")]
    public float[] chainChances = { 0.9f, 0.8f, 0.6f };

    [Header("見た目")]
    [Tooltip("1コマの表示時間")]
    public float frameDuration = 0.008f;
    [Tooltip("伸びきったあと、そのまま表示しておく時間")]
    public float holdDuration = 0.06f;
    [Tooltip("消えるまでの時間")]
    public float fadeDuration = 0.08f;
    [Tooltip("稲妻の太さの下限と上限。普段は長さに合わせて太さが決まる")]
    public Vector2 thicknessRange = new Vector2(0.8f, 1.6f);
    [Tooltip("当たり判定の太さ（見た目の太さに対する割合）")]
    public float hitboxWidthRatio = 0.35f;

    [Header("攻撃の中心位置の補正")]
    public Vector2 attackCenterOffset = new Vector2(0.2f, 0f);

    protected override float IntervalMultiplier => intervalMultiplier;

    // 表示中の稲妻（攻撃が無効になったときに消すため）
    private readonly List<GameObject> activeBolts = new List<GameObject>();

    public override void Fire(Vector3 origin, float opacity, float damageMultiplier = 1f)
    {
        if (player == null || boltFrames == null || boltFrames.Length == 0) return;

        Vector3 center = origin + (Vector3)attackCenterOffset;
        List<EnemyHealth> first = FindNearest(center, detectRadius, 1,
            new HashSet<EnemyHealth>(), new HashSet<EnemyHealth>());
        if (first.Count == 0) return;

        // 攻撃力が上がった分だけダメージも上げる
        float powerRate = referenceAttackPower > 0f
            ? player.EffectiveAttackPower / referenceAttackPower : 1f;
        float hitDamage = damage * powerRate * damageMultiplier;

        StartCoroutine(ChainRoutine(center, first[0], opacity, hitDamage));
    }

    // 段ごとに稲妻を出し、伸びきったら当たった敵から次の敵へ伝える
    IEnumerator ChainRoutine(Vector3 start, EnemyHealth firstTarget, float opacity, float hitDamage)
    {
        // hit：ダメージを与えた敵（同じ敵には1回だけ）。claimed：狙うと決めた敵
        HashSet<EnemyHealth> hit = new HashSet<EnemyHealth>();
        HashSet<EnemyHealth> claimed = new HashSet<EnemyHealth> { firstTarget };

        List<Vector3> froms = new List<Vector3> { start };
        List<EnemyHealth> targets = new List<EnemyHealth> { firstTarget };
        float growTime = frameDuration * boltFrames.Length;

        for (int stage = 0; stage < chainStages && targets.Count > 0; stage++)
        {
            // 狙った時点の敵の位置に向けて出す
            List<Vector3> ends = new List<Vector3>();
            for (int i = 0; i < targets.Count; i++)
            {
                Vector3 to = targets[i] != null ? targets[i].transform.position : froms[i];
                ends.Add(to);
                StartCoroutine(BoltRoutine(froms[i], to, targets[i], opacity, hitDamage, hit));
            }

            yield return new WaitForSeconds(growTime);
            if (this == null) yield break;
            if (stage + 1 >= chainStages) break;

            // 当たった敵（倒れていたら当たった位置）から、近くの敵へ伝える
            List<Vector3> nextFroms = new List<Vector3>();
            List<EnemyHealth> nextTargets = new List<EnemyHealth>();
            for (int i = 0; i < targets.Count; i++)
            {
                Vector3 from = targets[i] != null ? targets[i].transform.position : ends[i];
                foreach (EnemyHealth next in FindNearest(from, chainRadius, branchCount, claimed, hit))
                {
                    // 伝わるかどうかは1本ごとに運で決める
                    if (Random.value >= ChainChance(stage)) continue;
                    claimed.Add(next);
                    nextFroms.Add(from);
                    nextTargets.Add(next);
                }
            }
            froms = nextFroms;
            targets = nextTargets;
        }
    }

    // 1本の稲妻：コマ送りで伸ばし、見えている部分に当たり判定を出し、最後に薄くして消す
    IEnumerator BoltRoutine(Vector3 from, Vector3 to, EnemyHealth target,
        float opacity, float hitDamage, HashSet<EnemyHealth> hit)
    {
        Vector2 diff = to - from;
        float length = diff.magnitude;
        if (length < 0.05f)
        {
            HitTarget(target, hitDamage, hit);
            yield break;
        }

        Vector2 dir = diff / length;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        // 最後のコマの大きさを基準に、長さを相手までの距離に合わせる。
        // 太さは長さに比例させつつ、短すぎ・太すぎにならないようにする
        Vector2 spriteSize = boltFrames[boltFrames.Length - 1].bounds.size;
        float thickness = Mathf.Clamp(length * spriteSize.y / spriteSize.x,
            thicknessRange.x, thicknessRange.y);
        float hitboxWidth = thickness * hitboxWidthRatio;

        GameObject bolt = new GameObject("AlienBolt");
        bolt.transform.position = from;
        bolt.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        bolt.transform.localScale = new Vector3(length / spriteSize.x, thickness / spriteSize.y, 1f);
        activeBolts.Add(bolt);

        SpriteRenderer sr = bolt.AddComponent<SpriteRenderer>();
        sr.sprite = boltFrames[0];
        if (boltMaterial != null) sr.sharedMaterial = boltMaterial;
        sr.sortingOrder = sortingOrder;
        Color color = new Color(1f, 1f, 1f, Mathf.Clamp01(opacity));
        sr.color = color;

        // 伸びていく間：今のコマで先端が届いている所までに当たり判定を出す
        float growTime = frameDuration * boltFrames.Length;
        float t = 0f;
        while (t < growTime)
        {
            int frame = Mathf.Min(boltFrames.Length - 1, (int)(t / Mathf.Max(0.001f, frameDuration)));
            sr.sprite = boltFrames[frame];
            DamageAlong(from, dir, length * Reach(frame), hitboxWidth, angle, hitDamage, hit);
            yield return null;
            if (bolt == null) yield break;
            t += Time.deltaTime;
        }

        // 伸びきったら、狙った敵には帯から少しずれていても必ず当てる
        sr.sprite = boltFrames[boltFrames.Length - 1];
        HitTarget(target, hitDamage, hit);

        // 表示している間は当たり判定を残す
        t = 0f;
        while (t < holdDuration)
        {
            DamageAlong(from, dir, length, hitboxWidth, angle, hitDamage, hit);
            yield return null;
            if (bolt == null) yield break;
            t += Time.deltaTime;
        }

        // 消える間は当たり判定なし
        t = 0f;
        while (t < fadeDuration)
        {
            color.a = Mathf.Clamp01(opacity) * (1f - t / fadeDuration);
            sr.color = color;
            yield return null;
            if (bolt == null) yield break;
            t += Time.deltaTime;
        }

        activeBolts.Remove(bolt);
        Destroy(bolt);
    }

    // stage 段目から次の段へ伝わる確率（設定がなければ必ず伝わる）
    float ChainChance(int stage)
    {
        if (chainChances == null || stage >= chainChances.Length) return 1f;
        return Mathf.Clamp01(chainChances[stage]);
    }

    float Reach(int frame)
    {
        if (frameReach == null || frame >= frameReach.Length) return 1f;
        return Mathf.Clamp01(frameReach[frame]);
    }

    void DamageAlong(Vector2 from, Vector2 dir, float reach, float width, float angle,
        float hitDamage, HashSet<EnemyHealth> hit)
    {
        if (reach <= 0f) return;
        Vector2 boxCenter = from + dir * (reach * 0.5f);
        foreach (Collider2D col in Physics2D.OverlapBoxAll(boxCenter, new Vector2(reach, width), angle))
        {
            if (!col.CompareTag("Enemy")) continue;
            EnemyHealth enemy = col.GetComponentInParent<EnemyHealth>();
            if (enemy == null || !hit.Add(enemy)) continue;
            enemy.TakeDamage(hitDamage);
        }
    }

    void HitTarget(EnemyHealth target, float hitDamage, HashSet<EnemyHealth> hit)
    {
        if (target == null || !hit.Add(target)) return;
        target.TakeDamage(hitDamage);
    }

    // position から radius 以内で近い順に、まだ狙っていない・当たっていない敵を count 体まで探す
    List<EnemyHealth> FindNearest(Vector3 position, float radius, int count,
        HashSet<EnemyHealth> claimed, HashSet<EnemyHealth> hit)
    {
        List<EnemyHealth> candidates = new List<EnemyHealth>();
        foreach (GameObject enemyObject in GameObject.FindGameObjectsWithTag("Enemy"))
        {
            EnemyHealth enemy = enemyObject.GetComponentInParent<EnemyHealth>();
            if (enemy == null || claimed.Contains(enemy) || hit.Contains(enemy)
                || candidates.Contains(enemy)) continue;
            if (Vector2.Distance(position, enemy.transform.position) > radius) continue;
            candidates.Add(enemy);
        }

        candidates.Sort((a, b) =>
            Vector2.Distance(position, a.transform.position)
                .CompareTo(Vector2.Distance(position, b.transform.position)));
        if (candidates.Count > count) candidates.RemoveRange(count, candidates.Count - count);
        return candidates;
    }

    void OnDisable()
    {
        StopAllCoroutines();
        foreach (GameObject bolt in activeBolts)
        {
            if (bolt != null) Destroy(bolt);
        }
        activeBolts.Clear();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position + (Vector3)attackCenterOffset, detectRadius);
    }
}
