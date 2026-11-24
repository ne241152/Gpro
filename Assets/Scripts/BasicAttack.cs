using UnityEngine;

// キャラクターごとの通常攻撃の共通ルール。
// プレイヤーに付けると、コントローラーの攻撃間隔（gatlingInterval）ごとに Fire が呼ばれる。
// 新しいキャラクターの通常攻撃は、このクラスを継承して Fire を実装する。
// 分身（SpectralCompanion）も Fire を呼んで、本体の通常攻撃をまねする。
public abstract class BasicAttack : MonoBehaviour
{
    protected WizardPlayerController player;
    private float timer = 0f;

    protected virtual void Awake()
    {
        player = GetComponent<WizardPlayerController>();
    }

    protected virtual void Update()
    {
        if (player == null || Time.timeScale == 0) return;

        timer += Time.deltaTime;
        if (timer >= player.gatlingInterval)
        {
            Fire(transform.position, 1f);
            timer = 0f;
        }
    }

    // origin から攻撃する。opacity は見た目の透明度、damageMultiplier は威力の倍率
    public abstract void Fire(Vector3 origin, float opacity, float damageMultiplier = 1f);
}
