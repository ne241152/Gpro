using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 勇者の通常攻撃：炎の鞭。
// 向いている側に斬撃を出し、少し遅れて「その時点で向いている方向の逆」にも出す。
// 斬撃の範囲にいる敵全員に、1回の斬撃につき1回ずつダメージを与える。
public class HeroWhipAttack : BasicAttack
{
    [Header("エフェクト画像")]
    public Sprite rightSlashSprite;
    public Sprite leftSlashSprite;

    [Header("攻撃設定")]
    [Tooltip("攻撃間隔の倍率（コントローラーの攻撃間隔 × この値）")]
    public float intervalMultiplier = 2.5f;
    [Tooltip("威力の倍率（攻撃力 × この値）")]
    public float damageScale = 2f;
    [Tooltip("反対側の斬撃を出すまでの時間（秒）")]
    public float secondSlashDelay = 0.15f;
    [Tooltip("エフェクトが消えるまでの時間（秒）")]
    public float effectDuration = 0.25f;
    [Tooltip("当たり判定の大きさ（エフェクトの見た目に対する倍率）")]
    public Vector2 hitboxScale = new Vector2(0.9f, 0.6f);
    [Tooltip("キャラクターからの距離（エフェクトの横幅に対する割合。大きいほど外側に出る）")]
    public float forwardOffset = 0.25f;
    [Tooltip("上下の位置（エフェクトの高さに対する割合。マイナスで下に出る）")]
    public float verticalOffset = 0f;

    protected override float IntervalMultiplier => intervalMultiplier;

    public override void Fire(Vector3 origin, float opacity, float damageMultiplier = 1f)
    {
        if (player == null) return;
        StartCoroutine(SlashRoutine(origin, opacity, damageMultiplier));
    }

    IEnumerator SlashRoutine(Vector3 origin, float opacity, float damageMultiplier)
    {
        // 斬撃を出す位置は、本体の位置からのずれとして持っておく
        // （反対側の斬撃も、移動後の本体に合わせて出すため）
        Vector3 offset = origin - transform.position;

        // 1発目：今向いている方向に出す
        Slash(transform.position + offset, player.IsFacingLeft, opacity, damageMultiplier);

        yield return new WaitForSeconds(secondSlashDelay);

        if (this == null || player == null) yield break;

        // 2発目：その時点で向いている方向の逆に出す
        // （1発目と2発目の間に振り向けば、2発とも同じ方向に出せる）
        Slash(transform.position + offset, !player.IsFacingLeft, opacity, damageMultiplier);
    }

    void Slash(Vector3 origin, bool toLeft, float opacity, float damageMultiplier)
    {
        Sprite sprite = toLeft ? leftSlashSprite : rightSlashSprite;
        if (sprite == null) return;

        Vector3 scale = transform.lossyScale;
        Vector2 size = new Vector2(
            sprite.bounds.size.x * Mathf.Abs(scale.x),
            sprite.bounds.size.y * Mathf.Abs(scale.y));

        // 斬撃の内側の端が本体から forwardOffset ぶん離れるように、横にずらして出す
        float dir = toLeft ? -1f : 1f;
        Vector3 center = origin + new Vector3(
            dir * size.x * (0.5f + forwardOffset), size.y * verticalOffset, 0f);

        SpawnEffect(sprite, center, scale, opacity);
        DealDamage(center, Vector2.Scale(size, hitboxScale), damageMultiplier);
    }

    void SpawnEffect(Sprite sprite, Vector3 position, Vector3 scale, float opacity)
    {
        GameObject effect = new GameObject("HeroWhipSlash");
        effect.transform.position = position;
        effect.transform.localScale = new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), 1f);

        SpriteRenderer sr = effect.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = new Color(1f, 1f, 1f, opacity);

        // プレイヤーの画像より手前に表示する
        Transform playerSprite = transform.Find("WizardSprite");
        SpriteRenderer playerSR = playerSprite != null ? playerSprite.GetComponent<SpriteRenderer>() : null;
        if (playerSR != null)
        {
            sr.sortingLayerID = playerSR.sortingLayerID;
            sr.sortingOrder = playerSR.sortingOrder + 1;
        }

        StartCoroutine(FadeAndDestroy(sr, opacity));
    }

    IEnumerator FadeAndDestroy(SpriteRenderer sr, float opacity)
    {
        float t = 0f;
        while (t < effectDuration && sr != null)
        {
            t += Time.deltaTime;
            sr.color = new Color(1f, 1f, 1f, opacity * (1f - t / effectDuration));
            yield return null;
        }
        if (sr != null) Destroy(sr.gameObject);
    }

    void DealDamage(Vector3 center, Vector2 size, float damageMultiplier)
    {
        float damage = player.EffectiveAttackPower * damageScale * damageMultiplier;
        HashSet<EnemyHealth> hit = new HashSet<EnemyHealth>();

        foreach (Collider2D col in Physics2D.OverlapBoxAll(center, size, 0f))
        {
            if (!col.CompareTag("Enemy")) continue;
            EnemyHealth enemy = col.GetComponentInParent<EnemyHealth>();
            if (enemy == null || !hit.Add(enemy)) continue;
            enemy.TakeDamage(damage);
        }
    }

    // シーン上で当たり判定の大きさを確認するための表示（選択時のみ）
    void OnDrawGizmosSelected()
    {
        if (rightSlashSprite == null) return;
        Vector3 scale = transform.lossyScale;
        Vector2 size = new Vector2(
            rightSlashSprite.bounds.size.x * Mathf.Abs(scale.x),
            rightSlashSprite.bounds.size.y * Mathf.Abs(scale.y));
        Vector2 box = Vector2.Scale(size, hitboxScale);
        Gizmos.color = Color.red;
        float shift = size.x * (0.5f + forwardOffset);
        float lift = size.y * verticalOffset;
        Gizmos.DrawWireCube(transform.position + new Vector3(shift, lift, 0f), box);
        Gizmos.DrawWireCube(transform.position + new Vector3(-shift, lift, 0f), box);
    }
}
