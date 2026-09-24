using UnityEngine;

public class Experience : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D col)
    {
        if (col.CompareTag("Player")) {
            // 新しい WizardPlayerController を取得して経験値を加算
            WizardPlayerController player = col.GetComponent<WizardPlayerController>();
            if (player != null) {
                player.AddExp(1);
                Destroy(gameObject);
            }
        }
    }
}