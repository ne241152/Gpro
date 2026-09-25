using UnityEngine;
using System.Collections.Generic;

public class StageManager : MonoBehaviour
{
    [Header("プレイヤーの割り当て")]
    public Transform player;

    [Header("背景プレハブ")]
    public GameObject normalChunkPrefab;
    public GameObject rareChunkPrefab;

    [Header("チャンク（背景1枚）のサイズ")]
    public float chunkWidth = 10f;  // ★横幅（新しく追加）
    public float chunkHeight = 8f;  // 縦幅

    [Header("生成設定")]
    public int viewDistance = 2; // プレイヤーの周囲何マス分を常に表示するか
    public float rareRate = 0.1f;

    // どの座標（マス目）に背景を置いたかを記憶する辞書
    private Dictionary<Vector2Int, GameObject> activeChunks = new Dictionary<Vector2Int, GameObject>();

    void Start()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
        UpdateChunks();
    }

    void Update()
    {
        if (player == null) return;
        
        // プレイヤーが移動するたびに背景を更新
        UpdateChunks();
    }

    void UpdateChunks()
    {
        // プレイヤーが現在いる「マス目（グリッド座標）」を計算
        int currentChunkX = Mathf.RoundToInt(player.position.x / chunkWidth);
        int currentChunkY = Mathf.RoundToInt(player.position.y / chunkHeight);

        // 今回表示しておくべきマスのリスト
        List<Vector2Int> chunksToKeep = new List<Vector2Int>();

        // プレイヤーの周囲（上下左右）に背景を敷き詰める
        for (int x = -viewDistance; x <= viewDistance; x++)
        {
            for (int y = -viewDistance; y <= viewDistance; y++)
            {
                Vector2Int chunkPos = new Vector2Int(currentChunkX + x, currentChunkY + y);
                chunksToKeep.Add(chunkPos);

                // まだその座標に背景が生成されていなければ、新しく作る
                if (!activeChunks.ContainsKey(chunkPos))
                {
                    SpawnChunk(chunkPos);
                }
            }
        }

        // プレイヤーから離れすぎた古い背景を削除（メモリのパンクを防ぐ）
        List<Vector2Int> chunksToRemove = new List<Vector2Int>();
        foreach (var chunk in activeChunks)
        {
            if (!chunksToKeep.Contains(chunk.Key))
            {
                Destroy(chunk.Value); // 古い背景を消す
                chunksToRemove.Add(chunk.Key);
            }
        }

        // 記憶からも消去
        foreach (var pos in chunksToRemove)
        {
            activeChunks.Remove(pos);
        }
    }

    void SpawnChunk(Vector2Int gridPos)
    {
        GameObject prefab;

        // スタート地点 (0,0) は必ずノーマルステージにする
        if (gridPos.x == 0 && gridPos.y == 0)
        {
            prefab = normalChunkPrefab;
        }
        else
        {
            // 設定した確率（デフォルト0.1＝10%）でレアステージを出す
            prefab = Random.value < rareRate ? rareChunkPrefab : normalChunkPrefab;
        }

        // マス目の座標を、実際のUnity上の座標（ワールド座標）に変換
        Vector3 spawnPos = new Vector3(gridPos.x * chunkWidth, gridPos.y * chunkHeight, 0);
        
        // 背景を生成し、Hierarchyが散らからないようにStageManagerの子オブジェクトにする
        GameObject newChunk = Instantiate(prefab, spawnPos, Quaternion.identity);
        newChunk.transform.SetParent(this.transform);
        
        // 記憶しておく
        activeChunks.Add(gridPos, newChunk);
    }
}