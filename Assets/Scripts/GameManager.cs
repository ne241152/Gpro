using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    // ※インスペクターのエラーを防ぐため batPrefab の枠は残していますが、コード上では使いません
    public GameObject zombiePrefab; 
    public GameObject batPrefab;
    public TextMeshProUGUI timerText;
    public UIManager uiManager; 

    public float timeLimit = 45f; // 制限時間を1分（60秒）に設定

    private Transform player;
    private float spawnTimer = 0f;
    private float gameTime; 

    void Start() {
        gameTime = timeLimit; // スタート時に60秒をセットする
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
            timerText.text = "Time: " + Mathf.CeilToInt(gameTime).ToString() + "s";
        }

        if (spawnTimer >= 0.5f) {
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
        Vector2 spawnPos = (Vector2)player.position + Random.insideUnitCircle.normalized * 10f;
        
        // 余計な分岐をなくし、常にセットされた敵（コウモリ）を湧かせる
        Instantiate(zombiePrefab, spawnPos, Quaternion.identity);
    }
}