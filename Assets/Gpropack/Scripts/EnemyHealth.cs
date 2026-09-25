using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    // ★追加：敵の種類をインスペクターで選べるようにする
    public enum EnemyType { Zombie, Bat, None }
    public EnemyType type = EnemyType.Zombie; 

    public int hp = 2;
    public GameObject expPrefab; 

    [Header("移動スピード")]
    public float moveSpeed = 2.5f;

    private Transform player;
    private SpriteRenderer spriteRenderer;

    void Start()
    {
        // 画面内にいる "Player" タグがついたオブジェクトを探す
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }

        // 左右反転用にSpriteRendererを取得
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    void Update()
    {
        // ゲームが止まっている時やプレイヤーがいない時は動かない
        if (Time.timeScale == 0 || player == null) return;

        // プレイヤーの方向へジリジリ移動する
        transform.position = Vector2.MoveTowards(transform.position, player.position, moveSpeed * Time.deltaTime);

        // プレイヤーの位置に応じて左右の向きを変える
        if (spriteRenderer != null)
        {
            if (player.position.x < transform.position.x)
            {
                spriteRenderer.flipX = true;  // プレイヤーが左にいれば左を向く
            }
            else if (player.position.x > transform.position.x)
            {
                spriteRenderer.flipX = false; // プレイヤーが右にいれば右を向く
            }
        }
    }

    public void TakeDamage(int damage)
    {
        hp -= damage;

        if (hp <= 0)
        {
            if (expPrefab != null)
            {
                Instantiate(expPrefab, transform.position, Quaternion.identity);
            }
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            WizardPlayerController player = collision.GetComponent<WizardPlayerController>();

            if (player != null)
            {
                // ★追加：この敵が「Zombie」で、かつプレイヤーが「Passkey（無効化能力）」を持っていたら
                if (type == EnemyType.Zombie && player.hasPasskey)
                {
                    // 攻撃を無効化！（下の TakeDamage を実行せずにここで処理を終わる）
                    return; 
                }

                player.TakeDamage(10);
            }
        }
    }
}