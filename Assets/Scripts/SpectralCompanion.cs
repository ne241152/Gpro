using UnityEngine;

// A visual / attack companion, deliberately without Player tag, colliders,
// Rigidbody2D, health, experience collection, or a second player controller.
public class SpectralCompanion : MonoBehaviour
{
    public Vector2 followOffset = new Vector2(4f, 0f);
    [Range(0f, 1f)] public float opacity = 0.6f;

    private WizardPlayerController source;
    private SpriteRenderer sourceRenderer;
    private SpriteRenderer visual;
    private WizardAutoAttack sourceMeteor;
    private WizardAutoAttack meteor;
    private GameObject drone;
    private GameObject smsCannon;
    private float attackTimer;

    public void Initialize(WizardPlayerController player)
    {
        source = player;
        transform.position = source.transform.position + (Vector3)followOffset;
        transform.localScale = source.transform.lossyScale;
        Transform sourceVisual = source.transform.Find("WizardSprite");
        if (sourceVisual != null) sourceRenderer = sourceVisual.GetComponent<SpriteRenderer>();
        if (sourceRenderer == null) {
            Debug.LogError("分身: WizardSpriteを取得できません。", this);
            Destroy(gameObject);
            return;
        }

        GameObject sprite = new GameObject("WizardSprite");
        sprite.transform.SetParent(transform, false);
        sprite.transform.localPosition = sourceVisual.localPosition;
        sprite.transform.localRotation = sourceVisual.localRotation;
        sprite.transform.localScale = sourceVisual.localScale;
        visual = sprite.AddComponent<SpriteRenderer>();
        visual.sharedMaterial = sourceRenderer.sharedMaterial;
        visual.sortingLayerID = sourceRenderer.sortingLayerID;
        visual.sortingOrder = sourceRenderer.sortingOrder;
        visual.sprite = sourceRenderer.sprite;
        visual.color = new Color(1f, 1f, 1f, opacity);

        sourceMeteor = source.GetComponent<WizardAutoAttack>();
        if (sourceMeteor != null) {
            meteor = gameObject.AddComponent<WizardAutoAttack>();
            SyncMeteor();
        }
        SyncWeapons();
    }

    void Update()
    {
        if (source == null) { Destroy(gameObject); return; }
        // UI pauses already stop game time; do not attack or spawn weapons in menus.
        bool active = source.isActiveAndEnabled;
        if (visual != null) visual.enabled = active;
        if (meteor != null) meteor.enabled = active && sourceMeteor != null && sourceMeteor.enabled;
        if (!active || Time.timeScale <= 0f) return;

        SyncMeteor();
        SyncWeapons();
        attackTimer += Time.deltaTime;
        if (attackTimer >= Mathf.Max(0.01f, source.gatlingInterval)) {
            attackTimer = 0f;
            source.FireGatlingFrom(transform.position, opacity);
        }
    }

    void LateUpdate()
    {
        if (source == null || visual == null || !source.isActiveAndEnabled || Time.timeScale <= 0f) return;
        Vector3 target = source.transform.position + (Vector3)followOffset;
        transform.position = Vector3.MoveTowards(transform.position, target,
            Mathf.Max(0f, source.EffectiveMoveSpeed) * 1.25f * Time.deltaTime);
        transform.localScale = source.transform.lossyScale;
        visual.sprite = sourceRenderer.sprite;
        visual.flipX = sourceRenderer.flipX;
        visual.flipY = sourceRenderer.flipY;
    }

    void SyncMeteor()
    {
        if (sourceMeteor == null || meteor == null) return;
        meteor.meteorPrefab = sourceMeteor.meteorPrefab;
        meteor.attackInterval = sourceMeteor.attackInterval;
        meteor.detectRadius = sourceMeteor.detectRadius;
        meteor.visualOpacity = opacity;
        meteor.enabled = sourceMeteor.enabled;
    }

    void SyncWeapons()
    {
        if (source.HasDrone && drone == null) drone = source.SpawnDroneFor(transform, opacity);
        if (source.HasSmsCannon && smsCannon == null) smsCannon = source.SpawnSmsCannonFor(transform, opacity);
    }

    void OnDestroy()
    {
        if (drone != null) Destroy(drone);
        if (smsCannon != null) Destroy(smsCannon);
    }
}

// Called once when each projectile is spawned; changing animation frames keeps the tint.
public static class SpectralAppearance
{
    public static void Apply(GameObject instance, float opacity)
    {
        if (opacity >= 1f) return;
        foreach (SpriteRenderer renderer in instance.GetComponentsInChildren<SpriteRenderer>(true)) {
            Color color = renderer.color;
            color.a *= Mathf.Clamp01(opacity);
            renderer.color = color;
        }
    }
}
