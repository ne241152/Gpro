using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyMove), typeof(SpriteRenderer))]
public class EnemyFacing : MonoBehaviour
{
    [Header("方向別Sprite（左右が未設定なら正面を使用）")]
    public Sprite frontSprite;
    public Sprite leftSprite;
    public Sprite rightSprite;

    [Header("正面を向くX座標の許容差")]
    [Min(0f)] public float frontThreshold = 0.1f;

    private EnemyMove movement;
    private SpriteRenderer spriteRenderer;

    void Awake()
    {
        movement = GetComponent<EnemyMove>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (frontSprite == null) frontSprite = spriteRenderer.sprite;
    }

    void LateUpdate()
    {
        Sprite nextSprite = frontSprite;
        Transform player = movement.player;
        if (player != null)
        {
            float deltaX = player.position.x - transform.position.x;
            float threshold = Mathf.Max(0f, frontThreshold);
            if (deltaX < -threshold && leftSprite != null)
                nextSprite = leftSprite;
            else if (deltaX > threshold && rightSprite != null)
                nextSprite = rightSprite;
        }

        if (nextSprite != null) spriteRenderer.sprite = nextSprite;
    }
}
