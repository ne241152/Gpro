using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    // 既存Sceneとの互換性のため旧フィールドを保持します。batPrefabは未使用です。
    public GameObject zombiePrefab; 
    public GameObject batPrefab;
    [Header("生成する敵（空なら従来のZombie Prefabを使用）")]
    public GameObject[] enemyPrefabs;

    [System.Serializable]
    public class EnemySpawnPhase
    {
        [Min(0f)] public float startTime;
        public GameObject[] prefabs;
    }

    [Header("開始からの経過秒ごとの敵（未設定なら上の配列を使用）")]
    public EnemySpawnPhase[] spawnPhases;

    public TextMeshProUGUI timerText;
    public UIManager uiManager; 

    public float timeLimit = 45f; // 制限時間（秒）

    private Transform player;
    private float spawnTimer = 0f;
    private float gameTime; 

    void Start() {
        gameTime = timeLimit;
        FindPlayer();
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

    void SpawnEnemy()
    {
        GameObject prefab = SelectEnemyPrefab();
        if (prefab == null) return;

        Vector2 spawnPos = (Vector2)player.position + Random.insideUnitCircle.normalized * 26f;
        GameObject enemy = Instantiate(prefab, spawnPos, Quaternion.identity);
        // 見た目と移動で同じPlayerを参照します。
        if (enemy.TryGetComponent<EnemyMove>(out var movement))
            movement.player = player;
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

    GameObject SelectEnemyPrefab()
    {
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
