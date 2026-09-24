using UnityEngine;

public class WizardAutoAttack : MonoBehaviour
{
    public GameObject meteorPrefab;
    public float attackInterval = 1.0f;
    public float detectRadius = 6f;

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

            foreach (GameObject target in targets)
            {
                SpawnMeteor(target);
            }
        }
    }

    GameObject[] FindEnemiesInRange()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

        return System.Array.FindAll(enemies, enemy =>
            Vector2.Distance(transform.position, enemy.transform.position) <= detectRadius
        );
    }

    void SpawnMeteor(GameObject target)
    {
        if (meteorPrefab == null || target == null) return;

        Vector3 spawnPos = target.transform.position + new Vector3(0, 3f, 0);
        Instantiate(meteorPrefab, spawnPos, Quaternion.identity);
    }
}