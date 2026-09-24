using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public int maxHp = 100;
    public int currentHp;

    public Transform hpFill;

    private Vector3 originalScale;

    void Start()
    {
        currentHp = maxHp;

        if (hpFill != null)
        {
            originalScale = hpFill.localScale;
            UpdateHPBar();
        }
    }

    public void TakeDamage(int damage)
    {
        currentHp -= damage;
        currentHp = Mathf.Clamp(currentHp, 0, maxHp);

        UpdateHPBar();

        if (currentHp <= 0)
        {
            Debug.Log("Game Over");
            gameObject.SetActive(false);
        }
    }

    void UpdateHPBar()
    {
        if (hpFill == null) return;

        float hpRate = (float)currentHp / maxHp;
        hpFill.localScale = new Vector3(originalScale.x * hpRate, originalScale.y, originalScale.z);
    }
}