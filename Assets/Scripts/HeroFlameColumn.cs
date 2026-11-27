using System.Collections.Generic;
using UnityEngine;

// 勇者の固有スキル：炎の列。
// 一定時間ごとに、勇者の位置から炎の波を上下に1回ずつ打ち出して縦長の列を作り、
// 列が出ている間はその範囲の敵に少しずつダメージを与え続ける（上下の敵への対策）。
// 粒は2種類：波の先頭で細い弧を作る炎と、列の途中に止まって燃え残る炎。
// 粒は画面の上端・下端ぎりぎりまで進んで急に減速し、その後はゆっくり漂いながら消える。
// 当たり判定も、波の広がりに合わせて勇者から上下に伸びていく。
// 炎は Particle System でまとめて描画するので、粒が多くても重くなりにくい。
public class HeroFlameColumn : MonoBehaviour
{
    [Header("炎の粒の画像（元の色のまま使い、透明度と大きさだけ変える）")]
    public Texture2D flameSheet;
    [Tooltip("粒の材質。ライトの影響を受けない Sprite-Unlit-Default を使うと色がはっきり出る")]
    public Material particleMaterial;
    [Tooltip("画像の横のマス数")]
    public int sheetColumns = 4;
    [Tooltip("画像の縦のマス数")]
    public int sheetRows = 2;

    [Header("技の時間")]
    [Tooltip("炎の列が出ている時間（秒）。この間ダメージが続く")]
    public float activeDuration = 2f;
    [Tooltip("炎が消えてから次に出るまでの時間（秒）")]
    public float cooldown = 3f;

    [Header("範囲（ワールド座標の大きさ）")]
    [Tooltip("横幅。勇者1人分がおよそ1.7")]
    public float columnWidth = 4f;
    [Tooltip("画面の上端・下端から、どれだけ内側で止めるか")]
    public float edgeMargin = 0.8f;
    [Tooltip("カメラが見つからないときに使う、勇者から上下の端までの距離")]
    public float fallbackReach = 11f;
    [Tooltip("波の先頭が止まる位置まで進むのにかかる時間（秒）。当たり判定もこの時間で伸びる")]
    public float expandTime = 0.8f;

    [Header("ダメージ")]
    [Tooltip("1回あたりの威力の倍率（攻撃力 × この値）")]
    public float damageScale = 0.3f;
    [Tooltip("ダメージを与える間隔（秒）")]
    public float tickInterval = 0.3f;

    [Header("炎の波")]
    [Tooltip("先頭の弧の曲がり具合（0でまっすぐ。大きいほど列の端の炎が手前で止まる）")]
    [Range(0f, 1f)] public float arcCurvature = 0.2f;
    [Tooltip("粒が出る場所の高さ（勇者の位置を中心とした帯の高さ）")]
    public float spawnBandHeight = 0.8f;
    [Tooltip("急減速した後、外側へ漂う速さ")]
    public float driftSpeed = 0.5f;
    [Tooltip("急減速した後、横にふらつく速さ")]
    public float sideDriftSpeed = 0.3f;

    [Header("先頭の弧を作る炎")]
    [Tooltip("上向き・下向きそれぞれに出す数")]
    public int frontCount = 50;
    public Vector2 frontSize = new Vector2(0.7f, 1.1f);
    [Tooltip("弧の線の太さ（止まる位置のばらつき。先頭が止まる位置に対する割合）")]
    [Range(0f, 1f)] public float frontThickness = 0.035f;

    [Header("燃え残る炎")]
    [Tooltip("上向き・下向きそれぞれに出す数")]
    public int remainCount = 45;
    public Vector2 remainSize = new Vector2(0.6f, 1f);
    [Tooltip("止まる位置（先頭が止まる位置に対する割合）")]
    public Vector2 remainReach = new Vector2(0.08f, 0.85f);

    [Header("炎の粒の見た目")]
    [Tooltip("出てから消えるまでの色と透明度の変化。画像の色に掛け合わせるので、白のままなら元の色で表示される")]
    public Gradient colorOverLife = DefaultGradient();

    private WizardPlayerController player;
    private GameObject column;
    private ParticleSystem particles;
    private ParticleSystem.Particle[] buffer;
    private float timer;
    private float tickTimer;
    private bool active;

    void Awake()
    {
        player = GetComponent<WizardPlayerController>();
    }

    void Start()
    {
        CreateColumn();
        // 最初は休んでいる状態から始め、少し待ってから出す
        timer = 0f;
        active = false;
    }

    void OnDisable()
    {
        if (particles != null) particles.Clear();
        active = false;
        timer = 0f;
    }

    void OnDestroy()
    {
        if (column != null) Destroy(column);
    }

    void Update()
    {
        if (player == null || column == null || Time.timeScale == 0) return;

        timer += Time.deltaTime;

        if (active)
        {
            tickTimer += Time.deltaTime;
            if (tickTimer >= tickInterval)
            {
                tickTimer = 0f;
                DealDamage(CurrentHeight());
            }

            if (timer >= activeDuration)
            {
                active = false;
                timer = 0f;
            }
        }
        else if (timer >= cooldown)
        {
            active = true;
            timer = 0f;
            EmitWave(1f);
            EmitWave(-1f);
            // 出た瞬間にも1回当てる（このときは勇者のまわりだけ）
            tickTimer = 0f;
            DealDamage(CurrentHeight());
        }
    }

    void LateUpdate()
    {
        // 炎の列は勇者に付いてくる
        if (column != null) column.transform.position = transform.position;
        MoveParticles();
    }

    // 勇者から画面の上端（下端）ぎりぎりまでの距離。カメラは勇者を中心に追いかけている
    float Reach()
    {
        Camera cam = Camera.main;
        float reach = cam != null && cam.orthographic ? cam.orthographicSize : fallbackReach;
        return Mathf.Max(0.5f, reach - edgeMargin);
    }

    // 0〜1 の進み具合を、最初は速く、最後に急に減速するように変換する
    static float EaseOut(float t)
    {
        float u = 1f - Mathf.Clamp01(t);
        return 1f - u * u * u * u * u;
    }

    // 当たり判定の高さ。出始めは勇者のまわりだけで、expandTime かけて上下の画面端まで伸びる
    float CurrentHeight()
    {
        float rate = expandTime > 0f ? EaseOut(timer / expandTime) : 1f;
        return Mathf.Max(spawnBandHeight, Reach() * 2f * rate);
    }

    void DealDamage(float height)
    {
        float damage = player.EffectiveAttackPower * damageScale;
        HashSet<EnemyHealth> hit = new HashSet<EnemyHealth>();
        Vector2 size = new Vector2(columnWidth, height);

        foreach (Collider2D col in Physics2D.OverlapBoxAll(transform.position, size, 0f))
        {
            if (!col.CompareTag("Enemy")) continue;
            EnemyHealth enemy = col.GetComponentInParent<EnemyHealth>();
            if (enemy == null || !hit.Add(enemy)) continue;
            enemy.TakeDamage(damage);
        }
    }

    // 炎の波を1回打ち出す。direction：1 で上向き、-1 で下向き。
    // 粒の動きは MoveParticles で毎フレーム決めるので、ここでは出す位置と種類だけを決める
    void EmitWave(float direction)
    {
        if (particles == null) return;
        EmitKind(direction, frontCount, frontSize, true);
        EmitKind(direction, remainCount, remainSize, false);
    }

    void EmitKind(float direction, int count, Vector2 size, bool front)
    {
        ParticleSystem.EmitParams emit = new ParticleSystem.EmitParams();
        emit.applyShapeToPosition = false;
        emit.velocity = Vector3.zero;

        for (int i = 0; i < count; i++)
        {
            // 列の幅の中で横の位置を決める。上向きの粒は勇者より少し上、下向きは少し下から出す
            float x = Random.Range(-0.5f, 0.5f) * columnWidth;
            float y = direction * Random.Range(0.01f, 0.5f) * spawnBandHeight;

            emit.position = new Vector3(x, y, 0f);
            // 粒の種類を乱数の偶数・奇数で持たせる（偶数：先頭の弧、奇数：燃え残る炎）
            uint seed = (uint)Random.Range(0, int.MaxValue) & ~1u;
            emit.randomSeed = front ? seed : seed | 1u;
            // 技が終わるころに消えるようにする（少しずつずらして、一斉に消えないようにする）
            emit.startLifetime = Mathf.Max(0.1f, activeDuration - Random.Range(0f, 0.3f));
            emit.startSize = Random.Range(size.x, size.y);
            particles.Emit(emit, 1);
        }
    }

    // 粒の位置を毎フレーム決める。
    // 先頭の弧の炎は expandTime かけて画面端ぎりぎりまで進み、列の端ほど少し手前で止まる（弧の形）。
    // 燃え残る炎は途中で止まり、列の中を埋める。
    void MoveParticles()
    {
        if (particles == null) return;

        int count = particles.particleCount;
        if (count == 0) return;
        if (buffer == null || buffer.Length < particles.main.maxParticles)
        {
            buffer = new ParticleSystem.Particle[particles.main.maxParticles];
        }
        count = particles.GetParticles(buffer);

        float reach = Reach();
        float halfWidth = Mathf.Max(0.01f, columnWidth * 0.5f);

        for (int i = 0; i < count; i++)
        {
            ParticleSystem.Particle p = buffer[i];
            Vector3 pos = p.position;

            // 出た位置の上下で、上向きの粒か下向きの粒かを判断する（粒は横には動かさない）
            float direction = pos.y >= 0f ? 1f : -1f;
            float side = Mathf.Clamp(pos.x / halfWidth, -1f, 1f);

            // 粒ごとに決まった乱数（0〜1）で、止まる位置を決める
            bool front = (p.randomSeed & 1u) == 0u;
            float r = ((p.randomSeed >> 1) % 10007u) / 10007f;
            float stop = reach * (1f - arcCurvature * side * side);
            stop *= front
                ? 1f - frontThickness * r
                : Mathf.Lerp(remainReach.x, remainReach.y, r);

            float elapsed = p.startLifetime - p.remainingLifetime;
            float progress = expandTime > 0f ? EaseOut(elapsed / expandTime) : 1f;
            // 急減速した後も、外側へゆっくり漂い、横にも少しふらつく
            float after = Mathf.Max(0f, elapsed - expandTime * 0.6f);
            float drift = driftSpeed * after;
            float sway = (((p.randomSeed / 7u) % 1000u) / 1000f * 2f - 1f) * sideDriftSpeed;

            // 向きが変わらないよう、勇者の位置ちょうど（0）にはしない
            pos.y = direction * Mathf.Max(0.01f, stop * progress + drift);
            if (after > 0f) pos.x += sway * Time.deltaTime;
            p.position = pos;
            p.velocity = Vector3.zero;
            buffer[i] = p;
        }

        particles.SetParticles(buffer, count);
    }

    // 炎の粒を出す Particle System を作る。
    // 勇者の子にすると勇者の縮小（0.2倍）の影響を受けるので、別のオブジェクトにして位置だけ合わせる
    void CreateColumn()
    {
        column = new GameObject("HeroFlameColumn");
        column.transform.position = transform.position;

        particles = column.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = particles.main;
        main.loop = true;
        main.playOnAwake = false;
        main.startSpeed = 0f;
        main.startColor = Color.white;
        main.gravityModifier = 0f;
        // 粒は列（勇者）と一緒に動く
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = 500;

        // 粒はスクリプトから1つずつ出すので、自動で出す設定はオフにする
        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 0f;
        ParticleSystem.ShapeModule shape = particles.shape;
        shape.enabled = false;

        ParticleSystem.ColorOverLifetimeModule color = particles.colorOverLifetime;
        color.enabled = true;
        color.color = new ParticleSystem.MinMaxGradient(colorOverLife);

        // 出た直後に少し膨らみ、消えるにつれて小さくなる
        ParticleSystem.SizeOverLifetimeModule size = particles.sizeOverLifetime;
        size.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve(
            new Keyframe(0f, 0.7f), new Keyframe(0.1f, 1f), new Keyframe(0.5f, 0.8f), new Keyframe(1f, 0.2f));
        size.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        // 画像のマスから、粒ごとにランダムで1つ選ぶ（寿命の間は同じ絵のまま）
        ParticleSystem.TextureSheetAnimationModule sheet = particles.textureSheetAnimation;
        sheet.enabled = true;
        sheet.mode = ParticleSystemAnimationMode.Grid;
        sheet.numTilesX = Mathf.Max(1, sheetColumns);
        sheet.numTilesY = Mathf.Max(1, sheetRows);
        sheet.animation = ParticleSystemAnimationType.WholeSheet;
        sheet.frameOverTime = new ParticleSystem.MinMaxCurve(0f, 0.999f);
        sheet.cycleCount = 1;

        // 見た目：炎の画像を貼った材質を使い、勇者より奥に表示する
        ParticleSystemRenderer renderer = column.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        SpriteRenderer playerSR = FindPlayerRenderer();
        Material source = particleMaterial != null ? particleMaterial
            : (playerSR != null ? playerSR.sharedMaterial : null);
        if (source != null)
        {
            Material material = new Material(source);
            if (flameSheet != null) material.mainTexture = flameSheet;
            renderer.sharedMaterial = material;
        }
        if (playerSR != null)
        {
            renderer.sortingLayerID = playerSR.sortingLayerID;
            renderer.sortingOrder = playerSR.sortingOrder - 1;
        }

        particles.Play();
    }

    SpriteRenderer FindPlayerRenderer()
    {
        Transform sprite = transform.Find("WizardSprite");
        if (sprite != null) return sprite.GetComponent<SpriteRenderer>();
        return GetComponentInChildren<SpriteRenderer>();
    }

    static Gradient DefaultGradient()
    {
        Gradient g = new Gradient();
        g.SetKeys(
            new[]
            {
                // 色は画像の元の色のまま（白を掛けても色は変わらない）
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f),
            },
            new[]
            {
                // 出た瞬間から見えるようにし、寿命の15%から薄くなり始め、
                // 半分を過ぎる頃には薄い状態にする
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 0.15f),
                new GradientAlphaKey(0.35f, 0.45f),
                new GradientAlphaKey(0.12f, 0.75f),
                new GradientAlphaKey(0f, 1f),
            });
        return g;
    }

    // シーン上で範囲を確認するための表示（選択時のみ）
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireCube(transform.position, new Vector3(columnWidth, Reach() * 2f, 0f));
    }
}
