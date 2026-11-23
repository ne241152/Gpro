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
            // シーン上のプレイヤーを探す
            if (defaultPlayer == null)
            {
                defaultPlayer = GameObject.FindGameObjectWithTag("Player");
            }

            if (defaultPlayer != null)
            {
                int selectedID =
                    PlayerPrefs.GetInt("SelectedCharacterID", 0);

                WizardPlayerController controller =
                    defaultPlayer.GetComponent<WizardPlayerController>();

                WizardAutoAttack meteorAttack =
                    defaultPlayer.GetComponent<WizardAutoAttack>();

                // =========================
                // キャラクター固有設定
                // =========================

                if (controller != null)
                {
                    // ID 2 = 宇宙人だけ逆操作
                    controller.invertMovement = (selectedID == 2);
                }

                if (meteorAttack != null)
                {
                    // ID 0 = 魔法使いだけ隕石攻撃あり
                    meteorAttack.enabled = (selectedID == 0);
                }

                // =========================
                // 見た目の変更
                // =========================

                SpriteRenderer targetSR =
                    defaultPlayer.GetComponent<SpriteRenderer>();

                if (targetSR == null)
                {
                    Transform wizardSprite =
                        defaultPlayer.transform.Find("WizardSprite");

                    if (wizardSprite != null)
                    {
                        targetSR =
                            wizardSprite.GetComponent<SpriteRenderer>();
                    }
                }

                if (selectedID < playerPrefabs.Length &&
                    playerPrefabs[selectedID] != null)
                {
                    SpriteRenderer prefabSR =
                        playerPrefabs[selectedID]
                        .GetComponent<SpriteRenderer>();

                    if (prefabSR == null)
                    {
                        prefabSR =
                            playerPrefabs[selectedID]
                            .GetComponentInChildren<SpriteRenderer>();
                    }

                    if (targetSR != null && prefabSR != null)
                    {
                        Sprite selectedSprite = prefabSR.sprite;

                        // 現在の表示画像を変更
                        targetSR.sprite = selectedSprite;

                        if (controller != null)
                        {
                            // Controller側の正面画像も変更しないと
                            // 次のフレームで魔法使い画像に戻ってしまう
                            controller.front = selectedSprite;

                            // 勇者・宇宙人はまだ歩行画像が無いため
                            // 正面の白画像だけを使用
                            if (selectedID != 0)
                            {
                                controller.walkLeft =
                                    System.Array.Empty<Sprite>();

                                controller.walkRight =
                                    System.Array.Empty<Sprite>();
                            }
                        }
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
            if (titlePanel != null)
                titlePanel.SetActive(false);

            if (gameHUDPanel != null)
                gameHUDPanel.SetActive(true);

            if (defaultPlayer != null)
                defaultPlayer.SetActive(true);

            PlayerPrefs.SetInt("IsGameStart", 0);
            PlayerPrefs.Save();
        }
        else
        {
            if (titlePanel != null)
                titlePanel.SetActive(true);

            if (gameHUDPanel != null)
                gameHUDPanel.SetActive(false);

            if (defaultPlayer != null)
                defaultPlayer.SetActive(false);
        }
    }
}