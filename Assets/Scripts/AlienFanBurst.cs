using System.Collections.Generic;
using UnityEngine;

// 宇宙人の範囲攻撃：一定時間ごとに、最後に動いた方向（上下左右）へ扇形の稲妻を出し続ける。
// 向きは発動した瞬間のまま固定し、位置は宇宙人についていく。
// 出ている間は一定間隔で、扇形の範囲にいる敵にダメージを与える。攻撃力の強化が効く（分身は出さない）。
public class AlienFanBurst : MonoBehaviour
{
    [Header("画像（Pivot を扇の出発点に合わせておく）")]
    public Sprite startFrame;
    [Tooltip("出ている間に切り替えて表示するコマ")]
    public Sprite[] loopFrames;
    public Sprite endFrame;
    public Material material;
    public int sortingOrder = 10;
    [Tooltip("見た目の大きさの倍率")]
    public float visualScale = 1.5f;

    [Header("タイミング")]
    [Tooltip("発動してから次に発動するまでの時間")]
    public float cooldown = 4f;
    [Tooltip("稲妻が出ている時間（ダメージがある時間）")]
    public float activeDuration = 1.5f;
    [Tooltip("出始めのコマを表示する時間")]
    public float startDuration = 0.08f;
    [Tooltip("出ている間に、コマを切り替える間隔")]
    public float loopFrameDuration = 0.07f;
    [Tooltip("コマを切り替えるたびに、ランダムで上下反転して形の変化を増やす")]
    public bool randomFlip = true;
    [Tooltip("消え際の時間（ダメージなし）。0 にすると、ダメージが終わると同時に消える")]
    public float endDuration = 0.25f;

    [Header("攻撃")]
    [Tooltip("攻撃力が referenceAttackPower のときの、1回あたりのダメージ")]
    public float damage = 2f;
    public float referenceAttackPower = 3f;
    [Tooltip("同じ敵にダメージを与える間隔。触れた瞬間に1回当たり、触れている間はこの間隔ごとに当たる")]
    public float tickInterval = 0.3f;
    [Tooltip("扇の出発点から、届く距離")]
    public float reach = 8f;
    [Tooltip("扇の開き（中心から片側への角度）")]
    public float halfAngle = 35f;
    [Tooltip("プレイヤーの位置から、宇宙人の画像の中心までのずれ")]
    public Vector2 bodyCenterOffset = new Vector2(0.32f, 0.3f);
    [Tooltip("宇宙人の画像の中心から、扇の出発点までの距離（左右に出すとき）")]
    public float startOffset = 1f;
    [Tooltip("宇宙人の画像の中心から、扇の出発点までの距離（上下に出すとき。宇宙人は縦長なので左右より長くする）")]
    public float verticalStartOffset = 1.25f;

    private WizardPlayerController player;
    private GameObject visual;
    private SpriteRenderer mainRenderer;
    private SpriteRenderer endRenderer;

    private float cooldownTimer;
    private float elapsed;
    private float frameTimer;
    // 敵ごとに、次にダメージを与えてよい時刻（今回の発動の中での経過時間）
    private readonly Dictionary<EnemyHealth, float> nextHitTime = new Dictionary<EnemyHealth, float>();
    private int frameIndex = -1;
    private bool active;
    private bool ending;
    private Vector2 direction = Vector2.right;

    void Awake()
    {
        player = GetComponent<WizardPlayerController>();
    }

    void Update()
    {
        if (player == null || Time.timeScale == 0f) return;

        cooldownTimer += Time.deltaTime;
        if (!active && !ending && cooldownTimer >= cooldown)
        {
            cooldownTimer = 0f;
            Activate();
        }

        if (active)
        {
            elapsed += Time.deltaTime;
            DealDamage();
            UpdateFrames();

            if (elapsed >= activeDuration)
            {
                active = false;
                elapsed = 0f;
                if (endDuration > 0f && endFrame != null)
                {
                    ending = true;
                    endRenderer.sprite = endFrame;
                }
                else if (visual != null)
                {
                    visual.SetActive(false);
                }
            }
        }
        else if (ending)
        {
            elapsed += Time.deltaTime;
            UpdateEnding();
            if (elapsed >= endDuration)
            {
                ending = false;
                if (visual != null) visual.SetActive(false);
            }
        }
    }

    void LateUpdate()
    {
        // 位置だけ宇宙人についていく（向きは発動した瞬間のまま）
        if (visual != null && visual.activeSelf) visual.transform.position = Origin();
    }

    void Activate()
    {
        direction = SnapToFourDirections(player.LastMoveDirection);
        EnsureVisual();

        visual.SetActive(true);
        visual.transform.position = Origin();
        visual.transform.rotation = Quaternion.Euler(0f, 0f,
            Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        visual.transform.localScale = Vector3.one * visualScale;

        active = true;
        ending = false;
        elapsed = 0f;
        nextHitTime.Clear();
        frameTimer = 0f;
        frameIndex = -1;

        mainRenderer.sprite = startFrame != null ? startFrame : FirstLoopFrame();
        mainRenderer.flipY = false;
        SetAlpha(mainRenderer, 1f);
        SetAlpha(endRenderer, 0f);

        // 出た瞬間にも1回ダメージを与える
        DealDamage();
    }

    // 出始めのコマを少し表示してから、持続のコマを切り替え続ける
    void UpdateFrames()
    {
        if (elapsed < startDuration || loopFrames == null || loopFrames.Length == 0) return;

        frameTimer += Time.deltaTime;
        if (frameIndex >= 0 && frameTimer < loopFrameDuration) return;
        frameTimer = 0f;

        // 同じコマが続かないように選ぶ
        int next = Random.Range(0, loopFrames.Length);
        if (loopFrames.Length > 1 && next == frameIndex) next = (next + 1) % loopFrames.Length;
        frameIndex = next;
        mainRenderer.sprite = loopFrames[frameIndex];
        mainRenderer.flipY = randomFlip && Random.value < 0.5f;
    }

    // 持続のコマを薄くしながら、消え際のコマを重ねて表示し、そのあと消え際のコマも薄くする
    void UpdateEnding()
    {
        float t = endDuration > 0f ? Mathf.Clamp01(elapsed / endDuration) : 1f;
        SetAlpha(mainRenderer, 1f - Mathf.Clamp01(t / 0.5f));
        float peak = 0.3f;
        SetAlpha(endRenderer, t < peak ? t / peak : 1f - (t - peak) / (1f - peak));
    }

    // 毎フレーム、扇に触れている敵を調べる。触れた瞬間に1回当て、
    // 触れ続けている敵には tickInterval ごとに当てる（敵ごとに間隔を数える）
    void DealDamage()
    {
        Vector2 origin = Origin();
        float powerRate = referenceAttackPower > 0f
            ? player.EffectiveAttackPower / referenceAttackPower : 1f;
        float hitDamage = damage * powerRate;

        HashSet<EnemyHealth> checkedThisFrame = new HashSet<EnemyHealth>();
        // 敵の体の大きさのぶん、少し広めに探す
        foreach (Collider2D col in Physics2D.OverlapCircleAll(origin, reach + 2f))
        {
            if (!col.CompareTag("Enemy")) continue;
            EnemyHealth enemy = col.GetComponentInParent<EnemyHealth>();
            if (enemy == null || checkedThisFrame.Contains(enemy)) continue;
            if (!TouchesFan(col, origin)) continue;

            checkedThisFrame.Add(enemy);
            if (nextHitTime.TryGetValue(enemy, out float next) && elapsed < next) continue;

            nextHitTime[enemy] = elapsed + tickInterval;
            enemy.TakeDamage(hitDamage);
        }
    }

    // 敵の体（当たり判定を円とみなす）の一部でも扇に入っていれば true
    bool TouchesFan(Collider2D col, Vector2 origin)
    {
        Bounds b = col.bounds;
        Vector2 toEnemy = (Vector2)b.center - origin;
        float radius = Mathf.Max(b.extents.x, b.extents.y);
        float dist = toEnemy.magnitude;

        if (dist <= radius) return true;            // 出発点に重なっている
        if (dist - radius > reach) return false;    // 届く距離より遠い

        // 扇の角度に、体の大きさで広がるぶんを足して判定する
        float extra = Mathf.Asin(Mathf.Clamp01(radius / dist)) * Mathf.Rad2Deg;
        return Vector2.Angle(direction, toEnemy) <= halfAngle + extra;
    }

    Vector2 Origin()
    {
        return OriginFor(direction);
    }

    // 宇宙人の画像の中心から、向きに合わせた距離だけ離した位置
    Vector2 OriginFor(Vector2 dir)
    {
        float distance = Mathf.Abs(dir.y) > Mathf.Abs(dir.x) ? verticalStartOffset : startOffset;
        return (Vector2)transform.position + bodyCenterOffset + dir * distance;
    }

    static Vector2 SnapToFourDirections(Vector2 v)
    {
        if (v == Vector2.zero) return Vector2.right;
        if (Mathf.Abs(v.x) >= Mathf.Abs(v.y)) return v.x >= 0f ? Vector2.right : Vector2.left;
        return v.y >= 0f ? Vector2.up : Vector2.down;
    }

    Sprite FirstLoopFrame()
    {
        return loopFrames != null && loopFrames.Length > 0 ? loopFrames[0] : null;
    }

    void EnsureVisual()
    {
        if (visual != null) return;

        // プレイヤーの子にすると、プレイヤーの大きさの影響を受けるので別に作る
        visual = new GameObject("AlienFanBurst");
        mainRenderer = CreateRenderer("Main", sortingOrder);
        endRenderer = CreateRenderer("End", sortingOrder);
    }

    SpriteRenderer CreateRenderer(string name, int order)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(visual.transform, false);
        SpriteRenderer sr = child.AddComponent<SpriteRenderer>();
        if (material != null) sr.sharedMaterial = material;
        sr.sortingOrder = order;
        return sr;
    }

    static void SetAlpha(SpriteRenderer sr, float alpha)
    {
        if (sr == null) return;
        Color c = sr.color;
        c.a = Mathf.Clamp01(alpha);
        sr.color = c;
    }

    void OnDisable()
    {
        active = false;
        ending = false;
        cooldownTimer = 0f;
        if (visual != null) visual.SetActive(false);
    }

    void OnDestroy()
    {
        if (visual != null) Destroy(visual);
    }

    void OnDrawGizmosSelected()
    {
        Vector2 dir = Application.isPlaying ? direction : Vector2.right;
        Vector3 origin = OriginFor(dir);
        Gizmos.color = Color.yellow;
        float baseAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        Vector3 prev = origin;
        for (int i = 0; i <= 12; i++)
        {
            float a = (baseAngle - halfAngle + 2f * halfAngle * i / 12f) * Mathf.Deg2Rad;
            Vector3 p = origin + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * reach;
            Gizmos.DrawLine(prev, p);
            prev = p;
        }
        Gizmos.DrawLine(prev, origin);
    }
}
