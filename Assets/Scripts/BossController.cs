using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BossController : MonoBehaviour
{

    [Header("First Phase")]
    public float firstPhaseMaxHp = 150f;

    [Header("SecondSetting")]
    public float secondPhaseMaxHp = 250f;
    public float hpRecoverySpeed = 50f;

    [Header("AppearanceSetting")]
    public Color secondPhaseColor = Color.magenta;
    public Vector3 secondPhaseScale = new Vector3(2.5f, 2.5f, 1f);

    [Header("HPBarColor")]
    public Color firstPhaseHpColor = Color.red;
    private Image hpFillImage;

    private EnemyHealth enemyHealth;
    private SpriteRenderer spriteRenderer;
    private bool secondPhase;
    private bool transitioning;

    private Vector3 firstPhaseScale;
    private Color firstPhaseColor;
    private Slider hpSlider;

    void Awake()
    {
        enemyHealth = GetComponent<EnemyHealth>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        firstPhaseScale = transform.localScale;

        if (spriteRenderer != null)
            firstPhaseColor = spriteRenderer.color;

        GameObject sliderObject = GameObject.Find(
            "Canvas/GameHUDPanel/BossHPSlider"
        );

        if (sliderObject != null)
        {
            hpSlider = sliderObject.GetComponent<Slider>();

            Transform fillTransform =
                sliderObject.transform.Find("Fill Area/Fill");

            if (fillTransform != null)
            {
                hpFillImage = fillTransform.GetComponent<Image>();
            }

            if (hpFillImage != null)
                hpFillImage.color = firstPhaseHpColor;
        }
        else
        {
            Debug.LogWarning("BossHPSliderが見つかりません");
        }
    }

    void Update()
    {
        if (hpSlider == null || enemyHealth == null) return;

        float maxHp = secondPhase ? secondPhaseMaxHp : firstPhaseMaxHp;
        hpSlider.value = enemyHealth.hp / maxHp * 100f;
    }

    public bool BeginSecondPhase()
    {
        if (secondPhase || transitioning)
            return false;

        transitioning = true;
        enemyHealth.SetInvulnerable(true);

        StartCoroutine(EnterSecondPhase());

        return true;
    }

    IEnumerator EnterSecondPhase()
    {
        // 第一形態のHPが0になった状態で少し待つ
        yield return new WaitForSeconds(0.5f);

        // 第二形態の見た目に変更
        secondPhase = true;

        if (hpFillImage != null)
        hpFillImage.color = secondPhaseColor;

        if (spriteRenderer != null)
            spriteRenderer.color = secondPhaseColor;

        transform.localScale = secondPhaseScale;

        // HPを0から徐々に回復
        enemyHealth.SetHP(0f);

        while (enemyHealth.hp < secondPhaseMaxHp)
        {
            float nextHp = Mathf.MoveTowards(
                enemyHealth.hp,
                secondPhaseMaxHp,
                hpRecoverySpeed * Time.deltaTime
            );

            enemyHealth.SetHP(nextHp);

            yield return null;
        }

        // HP回復完了後、再び攻撃を受け付ける
        enemyHealth.SetHP(secondPhaseMaxHp);
        enemyHealth.SetInvulnerable(false);

        transitioning = false;

        Debug.Log("ボスが第二形態になりました！");
    }
}
