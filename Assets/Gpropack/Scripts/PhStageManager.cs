using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class PhStageManager : MonoBehaviour
{
    [Header("プレイヤー")]
    public Transform player;

    [Header("スマホ用背景チャンク")]
    public GameObject normalChunkPrefab;

    [Header("プレイヤーの周囲何チャンク分を残すか")]
    public int loadRange = 2;

    private Dictionary<Vector2Int, GameObject> chunks
        = new Dictionary<Vector2Int, GameObject>();

    private int chunkWidth;
    private int chunkHeight;

    private Vector2Int currentPlayerChunk;


    void Start()
    {
        // Ph_GameStageの中からTilemapを探す
        Tilemap tilemap =
            normalChunkPrefab.GetComponentInChildren<Tilemap>();

        if (tilemap == null)
        {
            Debug.LogError(
                "Ph_GameStageの中にTilemapが見つかりません！"
            );

            enabled = false;
            return;
        }

        // 実際にタイルが置かれている範囲に合わせる
        tilemap.CompressBounds();

        chunkWidth = tilemap.cellBounds.size.x;
        chunkHeight = tilemap.cellBounds.size.y;

        Debug.Log(
            $"【スマホ版】チャンクサイズ → 横:{chunkWidth} 縦:{chunkHeight}"
        );

        if (chunkWidth <= 0 || chunkHeight <= 0)
        {
            Debug.LogError(
                "チャンクサイズが0です！Ph_GameStageのGroundを確認してください。"
            );

            enabled = false;
            return;
        }

        if (player == null)
        {
            Debug.LogError(
                "PhStageManagerのPlayerが設定されていません！"
            );

            enabled = false;
            return;
        }

        // 最初にプレイヤーがいるチャンクを取得
        currentPlayerChunk = GetPlayerChunk();

        // 最初の周囲の背景を生成
        UpdateChunks();
    }


    void Update()
    {
        if (player == null)
            return;

        Vector2Int newPlayerChunk = GetPlayerChunk();

        // 別のチャンクへ移動したときだけ更新
        if (newPlayerChunk != currentPlayerChunk)
        {
            Debug.Log(
                "【スマホ版】チャンク移動！ " +
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
        // Ph_GameStageの中心が(0,0,0)なのでRoundを使用
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
        // プレイヤーを中心として周囲のチャンクを生成
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
        // すでに存在していたら何もしない
        if (chunks.ContainsKey(coord))
            return;


        Vector3 position =
            new Vector3(
                coord.x * chunkWidth,
                coord.y * chunkHeight,
                0f
            );


        GameObject newChunk =
            Instantiate(
                normalChunkPrefab,
                position,
                Quaternion.identity
            );


        newChunk.name =
            $"Ph_GameStage_{coord.x}_{coord.y}";


        chunks.Add(
            coord,
            newChunk
        );
    }
}