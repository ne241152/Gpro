using UnityEngine;

public class Experience : MonoBehaviour
{
    [Min(0f)] public float attractionRadius = 3f;
    [Min(0f)] public float attractionSpeed = 10f;

    private WizardPlayerController target;
    private bool isAttracting;
    private bool collected;

    void Start()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
            target = playerObject.GetComponent<WizardPlayerController>();
    }

    void Update()
    {
        if (collected || target == null || target.hp <= 0 || Time.timeScale == 0f) return;

        Vector2 offset = target.transform.position - transform.position;
        if (offset.sqrMagnitude <= attractionRadius * attractionRadius)
            isAttracting = true;
        if (!isAttracting) return;

        // 移動速度の強化後も追いつける速さにする。
        float speed = Mathf.Max(attractionSpeed, target.EffectiveMoveSpeed * 1.5f);
        Vector3 destination = new Vector3(target.transform.position.x,
            target.transform.position.y, transform.position.z);
        transform.position = Vector3.MoveTowards(transform.position, destination, speed * Time.deltaTime);

        if (((Vector2)(destination - transform.position)).sqrMagnitude <= 0.04f)
            Collect(target);
    }

    void OnTriggerEnter2D(Collider2D col)
    {
        if (col.CompareTag("Player")) {
            // 新しい WizardPlayerController を取得して経験値を加算
            WizardPlayerController player = col.GetComponent<WizardPlayerController>();
            if (player != null) {
                Collect(player);
            }
        }
    }

    void Collect(WizardPlayerController player)
    {
        if (collected || player.hp <= 0) return;
        collected = true;
        player.AddExp(1);
        Destroy(gameObject);
    }
}
