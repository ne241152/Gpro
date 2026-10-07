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
        currentHp -= damage;
        currentHp = Mathf.Clamp(currentHp, 0, maxHp);

        UpdateHPBar();

        // ダメージを受けたとき赤く光らせる
        if (spriteRenderer != null)
        {
            StopCoroutine(nameof(DamageFlash));
            StartCoroutine(DamageFlash());
        }

        if (currentHp <= 0)
        {
            Debug.Log("Game Over");
            gameObject.SetActive(false);
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