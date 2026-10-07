using UnityEngine;

public class EnemyMove : MonoBehaviour
{
    [Header("追いかけるプレイヤー")]
    public Transform player;

    [Header("移動速度")]
    public float moveSpeed = 1.0f;

    private Rigidbody2D rb;

    // プレイヤーに接触しているか
    private bool touchingPlayer = false;

    [Header("移動のカクカク感")]
    public float moveInterval = 0.05f;

    private float moveTimer = 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        if (player == null)
        {
            GameObject playerObject =
                GameObject.FindGameObjectWithTag("Player");

            if (playerObject != null)
            {
                player = playerObject.transform;
            }
            else
            {
                Debug.LogWarning(
                    "EnemyMove: Playerタグのオブジェクトが見つかりません！"
                );
            }
        }
    }

    void FixedUpdate()
    {
        if (player == null || rb == null)
            return;

        // プレイヤーに接触している間は前進しない
        if (touchingPlayer)
            return;

        moveTimer += Time.fixedDeltaTime;

        // 一定時間経つまでは動かさない
        if (moveTimer < moveInterval)
            return;

        Vector2 direction =
            ((Vector2)player.position - rb.position).normalized;

        Vector2 nextPosition =
            rb.position +
            direction * moveSpeed * moveTimer;

        rb.MovePosition(nextPosition);

        moveTimer = 0f;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            touchingPlayer = true;
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            touchingPlayer = false;
        }
    }
}