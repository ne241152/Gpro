using System.Collections.Generic;
using UnityEngine;

public class RiskBasedScanner : MonoBehaviour
{
    public float scanRadius = 14f;
    public int dangerThreshold = 5;
    public float bonusPercent = 30f;
    public float scanInterval = 10f;
    public float expansionDuration = 0.4f;
    public float holdDuration = 0.1f;
    public float sweepDuration = 0.9f;
    public float fadeDuration = 0.25f;
    public float resultHoldDuration = 0.15f;
    [SerializeField] private int currentEnemyCount;

    public int EnemyCount => currentEnemyCount;
    public bool IsHighRisk => currentEnemyCount >= Mathf.Max(1, dangerThreshold);
    public float MoveMultiplier => isActiveAndEnabled && hasScanResult && !IsHighRisk ? 1f + bonusPercent / 100f : 1f;
    public float AttackMultiplier => isActiveAndEnabled && hasScanResult && IsHighRisk ? 1f + bonusPercent / 100f : 1f;
    public float DamageMultiplier => isActiveAndEnabled && hasScanResult && IsHighRisk ? Mathf.Clamp01(1f - bonusPercent / 100f) : 1f;

    private WizardPlayerController owner;
    private SpriteRenderer ring;
    private Sprite scanSprite;
    private Sprite highRiskSprite;
    private Sprite lowRiskSprite;
    private SpriteRenderer sweep;
    private Sprite sweepSprite;
    private const float RingFramePixels = 750f;
    private const float RingRadiusPixels = 325f;
    private const float BeamCorePixels = 901f;
    private bool hasScanResult;
    private bool cycleScanned;
    private readonly HashSet<EnemyHealth> countedEnemies = new HashSet<EnemyHealth>();
    private float pulseTimer;

    public void Initialize(WizardPlayerController player, Texture2D texture, Texture2D resultTexture)
    {
        owner = player;
        if (texture == null) {
            Debug.LogError("リスクベース: Risk Scan Textureが未設定です。", this);
            return;
        }

        // Use only the circular HUD on the right, excluding the horizontal beam.
        float scaleX = texture.width / 1774f;
        float scaleY = texture.height / 887f;
        Rect rect = new Rect(988f * scaleX, 69f * scaleY, RingFramePixels * scaleX, RingFramePixels * scaleY);
        scanSprite = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        scanSprite.name = "RiskScanRing";
        if (resultTexture != null) {
            float resultX = resultTexture.width / 1774f;
            float resultY = resultTexture.height / 887f;
            highRiskSprite = Sprite.Create(resultTexture,
                new Rect(35f * resultX, 49f * resultY, 790f * resultX, 790f * resultY),
                new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            lowRiskSprite = Sprite.Create(resultTexture,
                new Rect(945f * resultX, 49f * resultY, 790f * resultX, 790f * resultY),
                new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        }

        GameObject effect = new GameObject("RiskScanEffect");
        effect.transform.SetParent(transform, false);
        ring = effect.AddComponent<SpriteRenderer>();
        ring.sprite = scanSprite;
        Transform playerVisual = transform.Find("WizardSprite");
        SpriteRenderer playerRenderer = playerVisual != null ? playerVisual.GetComponent<SpriteRenderer>() : null;
        if (playerRenderer != null) {
            ring.sharedMaterial = playerRenderer.sharedMaterial;
            ring.sortingLayerID = playerRenderer.sortingLayerID;
        }
        ring.sortingOrder = 5;
        GameObject sweepObject = new GameObject("RiskScanSweep");
        sweepObject.transform.SetParent(transform, false);
        sweep = sweepObject.AddComponent<SpriteRenderer>();
        // Pivot at the left end of the bright line, rather than the surrounding glow.
        Rect beamRect = new Rect(42f * scaleX, 401f * scaleY, 942f * scaleX, 85f * scaleY);
        sweepSprite = Sprite.Create(texture, beamRect, new Vector2(35f / 942f, 42f / 85f),
            100f, 0, SpriteMeshType.FullRect);
        sweepSprite.name = "RiskScanBeam";
        sweep.sprite = sweepSprite;
        sweep.sharedMaterial = ring.sharedMaterial;
        sweep.sortingLayerID = ring.sortingLayerID;
        sweep.sortingOrder = ring.sortingOrder + 1;
        UpdatePulse();
    }

    void Update()
    {
        if (owner == null || Time.timeScale <= 0f) return;
        pulseTimer += Time.deltaTime;
        float scanEnd = Mathf.Max(0.01f, expansionDuration) + Mathf.Max(0f, holdDuration)
            + Mathf.Max(0.01f, sweepDuration);
        if (!cycleScanned && pulseTimer >= scanEnd) {
            Scan();
            cycleScanned = true;
            hasScanResult = true;
        }
        if (pulseTimer >= Mathf.Max(scanInterval, scanEnd + Mathf.Max(0f, resultHoldDuration) + Mathf.Max(0.01f, fadeDuration))) {
            pulseTimer = 0f;
            cycleScanned = false;
        }
        UpdatePulse();
    }

    void Scan()
    {
        countedEnemies.Clear();
        foreach (Collider2D collider in Physics2D.OverlapCircleAll(transform.position, Mathf.Max(0.1f, scanRadius))) {
            EnemyHealth enemy = collider.GetComponentInParent<EnemyHealth>();
            if (enemy != null && enemy.hp > 0f && enemy.CompareTag("Enemy")) countedEnemies.Add(enemy);
        }
        currentEnemyCount = countedEnemies.Count;
    }

    void UpdatePulse()
    {
        if (ring == null || scanSprite == null) return;
        float growEnd = Mathf.Max(0.01f, expansionDuration);
        float sweepStart = growEnd + Mathf.Max(0f, holdDuration);
        float scanEnd = sweepStart + Mathf.Max(0.01f, sweepDuration);
        float fadeStart = scanEnd + Mathf.Max(0f, resultHoldDuration);
        float end = fadeStart + Mathf.Max(0.01f, fadeDuration);
        ring.enabled = pulseTimer < end;
        if (sweep != null) sweep.enabled = pulseTimer >= sweepStart && pulseTimer < scanEnd;
        if (!ring.enabled) return;
        bool showResult = cycleScanned && pulseTimer >= scanEnd;
        Sprite resultSprite = IsHighRisk ? highRiskSprite : lowRiskSprite;
        ring.sprite = showResult && resultSprite != null ? resultSprite : scanSprite;
        float expansion = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(pulseTimer / growEnd));
        // Match the bright outer circle to the scan radius, excluding its glow / margin.
        float diameter = scanRadius * 2f * Mathf.Lerp(0.15f, 1f, expansion)
            * (showResult && resultSprite != null ? 790f / 700f : RingFramePixels / (RingRadiusPixels * 2f));
        Vector3 parentScale = transform.lossyScale;
        ring.transform.localScale = new Vector3(
            diameter / (ring.sprite.bounds.size.x * Mathf.Max(0.0001f, Mathf.Abs(parentScale.x))),
            diameter / (ring.sprite.bounds.size.y * Mathf.Max(0.0001f, Mathf.Abs(parentScale.y))), 1f);
        float fadeIn = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(pulseTimer / (growEnd * 0.3f)));
        float fadeOut = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(fadeStart, end, pulseTimer));
        ring.color = new Color(1f, 1f, 1f, (showResult ? 0.45f : 0.24f) * fadeIn * fadeOut);
        if (sweep != null && sweep.enabled) {
            float progress = Mathf.Clamp01((pulseTimer - sweepStart) / Mathf.Max(0.01f, sweepDuration));
            float angle = 90f - 360f * progress;
            sweep.transform.position = transform.position;
            sweep.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            float beamWorldWidth = scanRadius * 942f / BeamCorePixels;
            float beamScale = beamWorldWidth / sweepSprite.bounds.size.x;
            sweep.transform.localScale = new Vector3(
                beamScale / Mathf.Max(0.0001f, Mathf.Abs(parentScale.x)),
                beamScale / Mathf.Max(0.0001f, Mathf.Abs(parentScale.y)), 1f);
            sweep.color = new Color(1f, 1f, 1f, 0.65f * fadeOut);
        }
    }

    void OnDisable()
    {
        if (ring != null) ring.enabled = false;
        if (sweep != null) sweep.enabled = false;
    }

    void OnDestroy()
    {
        if (ring != null) Destroy(ring.gameObject);
        if (sweep != null) Destroy(sweep.gameObject);
        if (sweepSprite != null) Destroy(sweepSprite);
        if (scanSprite != null) Destroy(scanSprite);
        if (highRiskSprite != null) Destroy(highRiskSprite);
        if (lowRiskSprite != null) Destroy(lowRiskSprite);
    }
}
