using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    [Header("プレイヤーのPrefabをセレクト画面と同じ順番で登録")]
    public GameObject[] playerPrefabs;

    [Header("プレイヤーを出現させたい位置（空のObjectなど）")]
    public Transform spawnPoint;

    [Header("メインカメラのCameraFollowスクリプト")]
    public CameraFollow cameraFollow;

    void Start()
    {
        // セレクト画面で保存したIDを読み込む（何もなければ0番目）
        int selectedID = PlayerPrefs.GetInt("SelectedCharacterID", 0);

        // Prefabが正しく登録されていれば生成する
        if (selectedID < playerPrefabs.Length && playerPrefabs[selectedID] != null)
        {
            // プレイヤーを生成
            GameObject spawnedPlayer = Instantiate(playerPrefabs[selectedID], spawnPoint.position, Quaternion.identity);

            // カメラの追従ターゲットに設定
            if (cameraFollow != null)
            {
                // ※CameraFollow内の変数名（targetなど）に合わせて代入してください
                cameraFollow.target = spawnedPlayer.transform;
            }
        }
        else
        {
            Debug.LogError("プレイヤーのPrefabが正しく登録されていないか、IDが不正です。");
        }
    }
}