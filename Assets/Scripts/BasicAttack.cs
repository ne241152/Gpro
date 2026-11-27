using UnityEngine;

// キャラクターごとの通常攻撃の共通ルール。
// プレイヤーに付けると、攻撃間隔（CurrentInterval）ごとに Fire が呼ばれる。
// 新しいキャラクターの通常攻撃は、このクラスを継承して Fire を実装する。
// 分身（SpectralCompanion）も Fire を呼んで、本体の通常攻撃をまねする。
public abstract class BasicAttack : MonoBehaviour
{
    protected WizardPlayerController player;
    private float timer = 0f;

    // コントローラーの攻撃間隔（gatlingInterval）に掛ける倍率。攻撃ごとに変えられる
    protected virtual float IntervalMultiplier => 1f;

    // 実際の攻撃間隔。攻撃速度アップなどの強化は gatlingInterval を通して反映される
    public float CurrentInterval =>
        (player != null ? player.gatlingInterval : 0.45f) * IntervalMultiplier;

    protected virtual void Awake()
    {
        player = GetComponent<WizardPlayerController>();
    }

    protected virtual void Update()
    {
        if (player == null || Time.timeScale == 0) return;

        timer += Time.deltaTime;
        if (timer >= CurrentInterval)
        {
            Fire(transform.position, 1f);
            timer = 0f;
        }
    }

    // origin から攻撃する。opacity は見た目の透明度、damageMultiplier は威力の倍率
    public abstract void Fire(Vector3 origin, float opacity, float damageMultiplier = 1f);
}
