using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class PCStageManager : MonoBehaviour
{
    public Transform player;
    public GameObject normalChunkPrefab;

    public int loadRange = 2;

    private Dictionary<Vector2Int, GameObject> chunks
        = new Dictionary<Vector2Int, GameObject>();

    private int chunkWidth;
    private int chunkHeight;

    private Vector2Int currentPlayerChunk;

    void Start()
    {
        Tilemap tilemap =
            normalChunkPrefab.GetComponentInChildren<Tilemap>();

        if (tilemap == null)
        {
            Debug.LogError("Tilemapが見つかりません！");
            enabled = false;
            return;
        }

        tilemap.CompressBounds();

        chunkWidth = tilemap.cellBounds.size.x;
        chunkHeight = tilemap.cellBounds.size.y;

        Debug.Log(
            $"チャンクサイズ → 横:{chunkWidth} 縦:{chunkHeight}"
        );

        if (chunkWidth <= 0 || chunkHeight <= 0)
        {
            Debug.LogError("チャンクサイズが0以下です！");
            enabled = false;
            return;
        }

        if (player == null)
        {
            Debug.LogError("Playerが設定されていません！");
            enabled = false;
            return;
        }

        // 最初にプレイヤーがいるチャンクを記録
        currentPlayerChunk = GetPlayerChunk();

        // プレイヤーの周囲を生成
        UpdateChunks();
    }

    void Update()
    {
        if (player == null)
            return;

        Vector2Int newPlayerChunk = GetPlayerChunk();

        Debug.Log(
            "Player座標: " + player.position +
            " / 判定チャンク: " + newPlayerChunk
        );

        // プレイヤーが別のチャンクへ移動したとき
        if (newPlayerChunk != currentPlayerChunk)
        {
            Debug.Log(
                "★ チャンク移動！ " +
                currentPlayerChunk +
                " → " +
                newPlayerChunk
            );

            currentPlayerChunk = newPlayerChunk;

            UpdateChunks();
        }
    }

    Vector2Int GetPlayerChunk()
    {
        // Prefabの原点がチャンク中央なのでRoundを使用
        int x = Mathf.RoundToInt(
            player.position.x / chunkWidth
        );

        int y = Mathf.RoundToInt(
            player.position.y / chunkHeight
        );

        return new Vector2Int(x, y);
    }

    void UpdateChunks()
    {
        // プレイヤーを中心に周囲のチャンクを生成
        for (int x = -loadRange; x <= loadRange; x++)
        {
            for (int y = -loadRange; y <= loadRange; y++)
            {
                Vector2Int coord =
                    currentPlayerChunk +
                    new Vector2Int(x, y);

                CreateChunkIfNeeded(coord);
            }
        }

        // 遠くなったチャンクを削除
        List<Vector2Int> removeList =
            new List<Vector2Int>();

        foreach (var chunk in chunks)
        {
            int distanceX =
                Mathf.Abs(
                    chunk.Key.x -
                    currentPlayerChunk.x
                );

            int distanceY =
                Mathf.Abs(
                    chunk.Key.y -
                    currentPlayerChunk.y
                );

            if (
                distanceX > loadRange ||
                distanceY > loadRange
            )
            {
                Destroy(chunk.Value);
                removeList.Add(chunk.Key);
            }
        }

        foreach (Vector2Int coord in removeList)
        {
            chunks.Remove(coord);
        }
    }

    void CreateChunkIfNeeded(Vector2Int coord)
    {
        if (chunks.ContainsKey(coord))
            return;

        Vector3 position =
            new Vector3(
                coord.x * chunkWidth,
                coord.y * chunkHeight,
                0f
            );

        GameObject chunk =
            Instantiate(
                normalChunkPrefab,
                position,
                Quaternion.identity
            );

        chunk.name =
            $"PC_GameStage_{coord.x}_{coord.y}";

        chunks.Add(coord, chunk);
    }
}