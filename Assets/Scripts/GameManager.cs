using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    // 既存Sceneとの互換性のため旧フィールドを保持します。batPrefabは未使用です。
    public GameObject zombiePrefab; 
    public GameObject batPrefab;
    [Header("生成する敵（空なら従来のZombie Prefabを使用）")]
    public GameObject[] enemyPrefabs;

    [Header("一度に2体生成する敵")]
    public GameObject doubleSpawnPrefab;

    [System.Serializable]
    public class EnemySpawnPhase
    {
        [Min(0f)] public float startTime;
        public GameObject[] prefabs;
        public int[] populationWeights;
        [Tooltip("0なら共通の上限を使用")]
        [Min(0)] public int maxActiveEnemies;
    }

    [Header("開始からの経過秒ごとの敵（未設定なら上の配列を使用）")]
    public EnemySpawnPhase[] spawnPhases;

    public TextMeshProUGUI timerText;
    public UIManager uiManager; 

    public float timeLimit = 120f; // 制限時間（秒）
    [Header("同時に出現できる敵数")]
    [Min(0)] public int maxActiveEnemies = 180;

    private readonly List<GameObject> activeEnemies = new List<GameObject>();
    private Transform player;
    private float spawnTimer = 0f;
    private float gameTime; 

    void Start() {
        gameTime = timeLimit;
        FindPlayer();
        activeEnemies.AddRange(GameObject.FindGameObjectsWithTag("Enemy"));
        foreach (GameObject enemy in activeEnemies)
            if (enemy.TryGetComponent<EnemyMove>(out var movement)) movement.spawnManager = this;
    }

    void Update()
    {
        if (player == null) {
            FindPlayer();
            return; 
        }

        // 足し算から引き算（カウントダウン）に変更
        gameTime -= Time.deltaTime;
        spawnTimer += Time.deltaTime;

        if (timerText != null) {
            // Mathf.CeilToInt で切り上げ表示（0.1秒でも残っていれば 1s と表示）
            int remainingSeconds = Mathf.Max(0, Mathf.CeilToInt(gameTime));
            timerText.text = $"{remainingSeconds / 60:00}:{remainingSeconds % 60:00}";
        }

        // 敵の出現頻度を2倍に変更（元は0.5秒ごと）。
        if (gameTime > 0f && spawnTimer >= 0.25f) {
            SpawnEnemy();
            spawnTimer = 0f;
        }

        // 0秒以下になったらゲームクリア！
        if (gameTime <= 0f) {
            gameTime = 0f; 
            if (timerText != null) timerText.text = "Game Clear!"; 
            
            if (uiManager != null) {
                uiManager.ShowResult(true); 
            } else {
                Time.timeScale = 0;
            }
            enabled = false;
        }
    }

    void FindPlayer()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) {
            player = p.transform;
        }
    }

    public void ReplaceDistantEnemy(GameObject enemy)
    {
        // 遠距離での削除だけを補充する。通常の死亡からは呼ばない。
        if (!activeEnemies.Remove(enemy)) return;
        if (!isActiveAndEnabled || gameTime <= 0f || Time.timeScale <= 0f || player == null) return;
        SpawnEnemy(1);
    }

    void SpawnEnemy(int maxSpawnCount = int.MaxValue)
    {
        // Reuse the tracked list instead of searching the whole scene on every spawn.
        activeEnemies.RemoveAll(enemy => enemy == null);
        EnemySpawnPhase phase = GetActivePhase();
        int populationLimit = phase != null && phase.maxActiveEnemies > 0
            ? phase.maxActiveEnemies : maxActiveEnemies;
        int remainingSlots = Mathf.Max(0, populationLimit - activeEnemies.Count);
        if (remainingSlots == 0) return;

        GameObject prefab = SelectEnemyPrefab(phase, populationLimit, out int typeSlots);
        if (prefab == null) return;

        int spawnCount = Mathf.Min(prefab == doubleSpawnPrefab ? 8 : 2, remainingSlots, typeSlots);
        spawnCount = Mathf.Min(spawnCount, maxSpawnCount);
        for (int i = 0; i < spawnCount; i++)
        {
            Vector2 spawnPos = (Vector2)player.position + Random.insideUnitCircle.normalized * 26f;
            GameObject enemy = Instantiate(prefab, spawnPos, Quaternion.identity);
            activeEnemies.Add(enemy);
            // 見た目と移動で同じPlayerを参照します。
            if (enemy.TryGetComponent<EnemyMove>(out var movement))
            {
                movement.player = player;
                movement.spawnManager = this;
            }
        }
    }
    GameObject[] GetSpawnPrefabs(float elapsedTime)
    {
        // 時間帯の指定がない既存Sceneは従来どおり生成します。
        if (spawnPhases == null || spawnPhases.Length == 0)
            return enemyPrefabs;

        EnemySpawnPhase activePhase = null;
        foreach (EnemySpawnPhase phase in spawnPhases)
        {
            if (phase == null || phase.startTime > elapsedTime) continue;
            if (activePhase == null || phase.startTime >= activePhase.startTime)
                activePhase = phase;
        }
        return activePhase != null ? activePhase.prefabs : null;
    }

    EnemySpawnPhase GetActivePhase()
    {
        EnemySpawnPhase activePhase = null;
        if (spawnPhases != null)
        {
            foreach (EnemySpawnPhase phase in spawnPhases)
                if (phase != null && phase.startTime <= timeLimit - gameTime
                    && (activePhase == null || phase.startTime >= activePhase.startTime))
                    activePhase = phase;
        }
        return activePhase;
    }

    GameObject SelectEnemyPrefab(EnemySpawnPhase activePhase, int populationLimit, out int typeSlots)
    {
        typeSlots = populationLimit;
        if (activePhase != null && activePhase.prefabs != null
            && activePhase.populationWeights != null
            && activePhase.populationWeights.Length == activePhase.prefabs.Length)
        {
            int totalWeight = 0;
            foreach (int weight in activePhase.populationWeights) totalWeight += Mathf.Max(0, weight);
            if (totalWeight <= 0) return null;

            // 累積割合で丸め、種類ごとの上限の合計を全体上限に合わせる。
            int cumulativeWeight = 0;
            int previousCap = 0;
            int totalVacancies = 0;
            GameObject selectedPrefab = null;
            for (int i = 0; i < activePhase.prefabs.Length; i++)
            {
                cumulativeWeight += Mathf.Max(0, activePhase.populationWeights[i]);
                int cumulativeCap = Mathf.RoundToInt((float)populationLimit * cumulativeWeight / totalWeight);
                int cap = cumulativeCap - previousCap;
                previousCap = cumulativeCap;
                GameObject candidate = activePhase.prefabs[i];
                if (candidate == null) continue;

                int alive = 0;
                foreach (GameObject enemy in activeEnemies)
                    if (enemy.name == candidate.name || enemy.name == candidate.name + "(Clone)") alive++;
                int vacancies = Mathf.Max(0, cap - alive);
                if (vacancies == 0) continue;
                totalVacancies += vacancies;
                if (Random.Range(0, totalVacancies) < vacancies)
                {
                    selectedPrefab = candidate;
                    typeSlots = vacancies;
                }
            }
            return selectedPrefab;
        }

        // カウントダウン表示ではなく、開始から何秒経ったかで切り替えます。
        GameObject[] candidates = GetSpawnPrefabs(timeLimit - gameTime);
        int count = 0;
        if (candidates != null)
        {
            foreach (GameObject prefab in candidates)
                if (prefab != null) count++;
        }

        // 時間帯を設定した場合は、候補が空でも別の種類を生成しません。
        if (count == 0)
            return spawnPhases == null || spawnPhases.Length == 0 ? zombiePrefab : null;

        int index = Random.Range(0, count);
        foreach (GameObject prefab in candidates)
        {
            if (prefab == null) continue;
            if (index-- == 0) return prefab;
        }
        return null;
    }
}
