using UnityEngine;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    public int maxHp = 100;
    public int currentHp;

    public Transform hpFill;

    [Header("ダメージ演出")]
    public float flashDuration = 0.12f;

    private Vector3 originalScale;
    private Vector3 originalPosition;

    private SpriteRenderer spriteRenderer;
    private Color originalColor;

    void Start()
    {
        currentHp = maxHp;

        if (hpFill != null)
        {
            originalScale = hpFill.localScale;
            originalPosition = hpFill.localPosition;

            UpdateHPBar();
        }

        // 魔法使いのSpriteRendererを取得
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
    }

public void TakeDamage(int damage)
{
    var controller = GetComponent<WizardPlayerController>();

    if (controller == null)
    {
        Debug.LogError("WizardPlayerControllerが見つかりません");
        return;
    }

    //バリアが有効ならプレイヤーへのダメージを防ぐ
    LinkBarrierController barrier =
        GetComponentInChildren<LinkBarrierController>();

    if (barrier != null)
    {
        Debug.Log("連携バリアがダメージを防ぎました");
        return;
    }

    int beforeHp = controller.hp;

    // HPの減少・バー更新・死亡判定を一本化
    controller.TakeDamage(damage);

    // 既存フィールドも同期
    currentHp = controller.hp;
    maxHp = controller.maxHp;
    UpdateHPBar();
    

    // 被弾演出は維持
    if (controller.hp < beforeHp && spriteRenderer != null)
    {
        StopCoroutine(nameof(DamageFlash));
        StartCoroutine(DamageFlash());
    }
}

    IEnumerator DamageFlash()
    {
        // 一瞬赤くする
        spriteRenderer.color = Color.red;

        yield return new WaitForSeconds(flashDuration);

        // 元の色に戻す
        spriteRenderer.color = originalColor;
    }

    void UpdateHPBar()
    {
        if (hpFill == null) return;

        float hpRate = (float)currentHp / maxHp;

        // HPに合わせて横幅を縮める
        hpFill.localScale = new Vector3(
            originalScale.x * hpRate,
            originalScale.y,
            originalScale.z
        );

        // 左端を固定したまま右側から減らす
        float lostWidth =
            originalScale.x - (originalScale.x * hpRate);

        hpFill.localPosition = new Vector3(
            originalPosition.x - lostWidth / 2f,
            originalPosition.y,
            originalPosition.z
        );
    }
}