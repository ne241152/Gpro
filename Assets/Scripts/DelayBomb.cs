using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DelayBomb : MonoBehaviour
{
    public int damage = 10;
    public float cycleDuration = 6f;
    public float flightDuration = 1f;
    public float explosionRadius = 3f;
    public float arcHeight = 3f;
    public float clockWidth = 1.4f;
    public float clockHeadGap = 0.15f;
    public float explosionVisualScale = 1.4f;
    public Sprite[] flightFrames;
    public Sprite[] targetFrames;
    public Sprite[] blastFrames;
    public Sprite[] clockFrames;

    private Transform owner;
    private SpriteRenderer clock;
    private SpriteRenderer ownerSprite;
    private Material effectMaterial;
    private int effectSortingLayer;

    public void Initialize(Transform player)
    {
        owner = player;
    }

    void Start()
    {
        if (owner == null) {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) owner = player.transform;
        }
        if (owner == null) { Destroy(gameObject); return; }
        // The prefab's old placeholder is replaced by separate clock / projectile effects.
        SpriteRenderer placeholder = GetComponent<SpriteRenderer>();
        if (placeholder != null) {
            effectMaterial = placeholder.sharedMaterial;
            effectSortingLayer = placeholder.sortingLayerID;
            placeholder.enabled = false;
        }
        if (!HasSprites(clockFrames) || !HasSprites(flightFrames) ||
            !HasSprites(targetFrames) || !HasSprites(blastFrames)) {
            Debug.LogError("SMSキャノン: 画像の参照が不足しています。DelayBombの各Framesを確認してください。", this);
            enabled = false;
            return;
        }
        Transform visual = owner.Find("WizardSprite");
        if (visual != null) ownerSprite = visual.GetComponent<SpriteRenderer>();
        clock = CreateEffect("SMSClock", 110);
        SetFrame(clock, clockFrames, 0);
        SetWidth(clock, clockWidth);
        Debug.Log("SMSキャノン起動: 時計6コマ、6秒周期、残り1秒で着弾位置を固定します。", this);
        StartCoroutine(AttackCycle());
    }

    void LateUpdate()
    {
        if (owner == null) { Destroy(gameObject); return; }
        if (clock != null) {
            float top = ownerSprite != null ? ownerSprite.bounds.max.y : owner.position.y + 1f;
            float centerX = ownerSprite != null ? ownerSprite.bounds.center.x : owner.position.x;
            float halfHeight = clock.bounds.extents.y;
            clock.transform.position = new Vector3(centerX, top + halfHeight + clockHeadGap, owner.position.z);
        }
    }

    IEnumerator AttackCycle()
    {
        while (owner != null) {
            float duration = Mathf.Max(1f, cycleDuration);
            float travel = Mathf.Clamp(flightDuration, 0.1f, duration);
            float elapsed = 0f;
            float lockTime = duration - travel;
            while (elapsed < lockTime) {
                SetFrame(clock, clockFrames, Mathf.FloorToInt(elapsed));
                yield return null;
                elapsed += Time.deltaTime;
            }
            SetFrame(clock, clockFrames, 5);

            // Snapshot once at T-1s. The target is never followed after this point.
            Vector3? destination = FindTargetPosition();
            if (destination.HasValue) {
                Vector3 end = destination.Value;
                Vector3 start = owner.position;
                Vector3 control = (start + end) * 0.5f + Vector3.up * arcHeight;
                SpriteRenderer marker = CreateEffect("SMSTarget", 15);
                SpriteRenderer shot = CreateEffect("SMSProjectile", 105);
                SetFrame(marker, targetFrames, 0);
                SetWidth(marker, explosionRadius * 2f);
                marker.transform.position = end;
                SetFrame(shot, flightFrames, flightFrames.Length - 1);
                SetWidth(shot, 2.5f);

                float flight = 0f;
                while (flight < travel) {
                    float t = Mathf.Clamp01(flight / travel);
                    shot.transform.position = Bezier(start, control, end, t);
                    Vector3 tangent = 2f * ((1f - t) * (control - start) + t * (end - control));
                    shot.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg);
                    SetFrame(shot, flightFrames, Mathf.FloorToInt(t * flightFrames.Length));
                    SetFrame(marker, targetFrames, Mathf.FloorToInt(t * targetFrames.Length));
                    yield return null;
                    flight += Time.deltaTime;
                }
                Destroy(shot.gameObject);
                Destroy(marker.gameObject);
                Explode(end);
            } else {
                // No enemy at the lock time: skip the shot, preserving the six-second cycle.
                yield return new WaitForSeconds(travel);
            }
        }
    }

    Vector3? FindTargetPosition()
    {
        float nearestDistance = float.PositiveInfinity;
        Vector3? nearest = null;
        foreach (GameObject enemy in GameObject.FindGameObjectsWithTag("Enemy")) {
            float distance = (enemy.transform.position - owner.position).sqrMagnitude;
            if (distance < nearestDistance) {
                nearestDistance = distance;
                nearest = enemy.transform.position;
            }
        }
        return nearest;
    }

    void Explode(Vector3 position)
    {
        var hit = new HashSet<EnemyHealth>();
        foreach (Collider2D collider in Physics2D.OverlapCircleAll(position, explosionRadius)) {
            EnemyHealth enemy = collider.GetComponentInParent<EnemyHealth>();
            if (enemy != null && enemy.CompareTag("Enemy") && hit.Add(enemy)) enemy.TakeDamage(damage);
        }
        StartCoroutine(BlastAnimation(position));
    }

    IEnumerator BlastAnimation(Vector3 position)
    {
        SpriteRenderer blast = CreateEffect("SMSExplosion", 106);
        blast.transform.position = position;
        SetFrame(blast, blastFrames, 4);
        SetWidth(blast, explosionRadius * 2f * explosionVisualScale);
        for (int i = 0; i < blastFrames.Length; i++) {
            SetFrame(blast, blastFrames, i);
            yield return new WaitForSeconds(0.06f);
        }
        Destroy(blast.gameObject);
    }

    SpriteRenderer CreateEffect(string name, int order)
    {
        GameObject effect = new GameObject(name);
        effect.transform.SetParent(transform, false);
        SpriteRenderer renderer = effect.AddComponent<SpriteRenderer>();
        if (effectMaterial != null) renderer.sharedMaterial = effectMaterial;
        renderer.sortingLayerID = effectSortingLayer;
        renderer.sortingOrder = order;
        return renderer;
    }

    static bool HasSprites(Sprite[] frames)
    {
        if (frames == null || frames.Length == 0) return false;
        foreach (Sprite frame in frames) {
            if (frame == null) return false;
        }
        return true;
    }

    static void SetFrame(SpriteRenderer renderer, Sprite[] frames, int index)
    {
        if (renderer != null && frames != null && frames.Length > 0)
            renderer.sprite = frames[Mathf.Clamp(index, 0, frames.Length - 1)];
    }

    static void SetWidth(SpriteRenderer renderer, float width)
    {
        if (renderer.sprite == null) return;
        float scale = width / renderer.sprite.bounds.size.x;
        renderer.transform.localScale = new Vector3(scale, scale, 1f);
    }

    static Vector3 Bezier(Vector3 start, Vector3 control, Vector3 end, float t)
    {
        return (1f - t) * (1f - t) * start + 2f * (1f - t) * t * control + t * t * end;
    }
}
