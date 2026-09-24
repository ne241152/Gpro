using UnityEngine;

public class SampleSceneManager : MonoBehaviour
{
    [Header("UIパネルの切り替え")]
    public GameObject titlePanel;     
    public GameObject gameHUDPanel;   

    [Header("キャラクター画像（プレハブ）の配列")]
    public GameObject[] playerPrefabs; 

    [Header("最初からヒエラルキーにいる完璧なPlayer")]
    public GameObject defaultPlayer;   

    void Awake()
    {
        int isGameStart = PlayerPrefs.GetInt("IsGameStart", 0);

        if (isGameStart == 1)
        {
            // シーン上のプレイヤーを自動で探して確実に1体に絞る
            if (defaultPlayer == null)
            {
                defaultPlayer = GameObject.FindGameObjectWithTag("Player");
            }

            if (defaultPlayer != null) 
            {
                // 見た目のすり替え処理
                SpriteRenderer targetSR = defaultPlayer.GetComponent<SpriteRenderer>();
                if (targetSR == null) targetSR = defaultPlayer.GetComponentInChildren<SpriteRenderer>();

                int selectedID = PlayerPrefs.GetInt("SelectedCharacterID", 0);

                if (selectedID < playerPrefabs.Length && playerPrefabs[selectedID] != null)
                {
                    SpriteRenderer prefabSR = playerPrefabs[selectedID].GetComponent<SpriteRenderer>();
                    if (prefabSR == null) prefabSR = playerPrefabs[selectedID].GetComponentInChildren<SpriteRenderer>();

                    if (targetSR != null && prefabSR != null)
                    {
                        targetSR.sprite = prefabSR.sprite;
                    }
                }
            }
        }
    }

    void Start()
    {
        Time.timeScale = 1f;

        int isGameStart = PlayerPrefs.GetInt("IsGameStart", 0);

        if (isGameStart == 1)
        {
            if (titlePanel != null) titlePanel.SetActive(false); 
            if (gameHUDPanel != null) gameHUDPanel.SetActive(true); 
            
            // ここでシーン上に元々いる本物だけをアクティブにする
            if (defaultPlayer != null) defaultPlayer.SetActive(true);

            PlayerPrefs.SetInt("IsGameStart", 0);
            PlayerPrefs.Save();
        }
        else
        {
            if (titlePanel != null) titlePanel.SetActive(true); 
            if (gameHUDPanel != null) gameHUDPanel.SetActive(false); 
            
            // タイトル画面の時はプレイヤーの重複を防ぐために非表示、または1体のみに制限
            if (defaultPlayer != null) defaultPlayer.SetActive(false); 
        }
    }
}