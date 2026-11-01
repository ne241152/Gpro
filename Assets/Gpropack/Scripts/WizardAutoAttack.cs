using UnityEngine;

public class WizardAutoAttack : MonoBehaviour
{
    public GameObject meteorPrefab;
    public float attackInterval = 1.0f;
    public float detectRadius = 26f;
    public float visualOpacity = 1f;
    public float damageMultiplier = 1f;
    [System.NonSerialized] public WizardPlayerController attackSource;

    void Awake()
    {
        attackSource = GetComponent<WizardPlayerController>();
    }

    private float timer = 0f;

    void Update()
    {
        GameObject[] targets = FindEnemiesInRange();

        if (targets.Length == 0)
        {
            timer = 0f;
            return;
        }

        timer += Time.deltaTime;

        if (timer >= attackInterval)
        {
            timer = 0f;

            // 画面内の敵から重複なしでランダムに最大8体を選ぶ。
            int targetCount = Mathf.Min(8, targets.Length);
            for (int i = 0; i < targetCount; i++)
            {
                int selected = Random.Range(i, targets.Length);
                GameObject target = targets[selected];
                targets[selected] = targets[i];
                targets[i] = target;
                SpawnMeteor(target);
            }
        }
    }

    GameObject[] FindEnemiesInRange()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        Camera gameCamera = Camera.main;
        if (gameCamera == null) return System.Array.Empty<GameObject>();

        return System.Array.FindAll(enemies, enemy =>
        {
            if (Vector2.Distance(transform.position, enemy.transform.position) > detectRadius)
                return false;

            Vector3 viewport = gameCamera.WorldToViewportPoint(enemy.transform.position);
            return viewport.z > 0f && viewport.x >= 0f && viewport.x <= 1f
                && viewport.y >= 0f && viewport.y <= 1f;
        });
    }

    void SpawnMeteor(GameObject target)
    {
        if (meteorPrefab == null || target == null) return;

        Vector3 spawnPos = target.transform.position + new Vector3(0, 3f, 0);
        GameObject meteor = Instantiate(meteorPrefab, spawnPos, Quaternion.identity);
        Meteor projectile = meteor.GetComponent<Meteor>();
        if (projectile != null) projectile.damageMultiplier = damageMultiplier
            * (attackSource != null ? attackSource.RiskAttackMultiplier : 1f);
        SpectralAppearance.Apply(meteor, visualOpacity);
    }
}
