using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject titlePanel;
    public GameObject gameHUDPanel;
    public GameObject levelUpPanel;
    public GameObject resultPanel;
    
    [Header("Game Systems")]
    public WizardPlayerController player;
    public GameObject enemySpawner; 

    [Header("UI Elements")]
    public TextMeshProUGUI resultText;

    [Header("Hint UI")]
    public GameObject hintPanel;       // ネタバレを出すポップアップ画面
    public TextMeshProUGUI hintText;   // ネタバレの文章を入れるテキスト
    
    // ボタンの場所を記憶する枠
    public RectTransform HintButton_1;
    public RectTransform HintButton_2;
    public RectTransform HintButton_3;

    void Start()
    {
        ShowTitle(); // 起動時はタイトル画面を表示
    }

    // タイトル画面を表示
    public void ShowTitle()
    {
        titlePanel.SetActive(true);
        gameHUDPanel.SetActive(false);
        levelUpPanel.SetActive(false);
        resultPanel.SetActive(false);
        
        if (player != null) player.gameObject.SetActive(false);
        if (enemySpawner != null) enemySpawner.SetActive(false);

        Time.timeScale = 0; 
    }

    // ゲーム開始
    public void StartGame()
    {
        titlePanel.SetActive(false);
        gameHUDPanel.SetActive(true);
        
        if (player != null) player.gameObject.SetActive(true);
        if (enemySpawner != null) enemySpawner.SetActive(true);

        Time.timeScale = 1; 
    }

    // レベルアップ画面を表示
    public void ShowLevelUp(int phase)
    {
        levelUpPanel.SetActive(true);
        Time.timeScale = 0; 
    }

    // 強化カードを選んだ時の処理
    public void SelectSkillCard(int cardNo)
    {
        switch (player.BattlePhase){
            case 1:
                switch (cardNo){
                    case 1:
                        player.IncreaseMaxHP(20);
                        player.IncreaseDefense(20f);
                        player.DecreaseAttackSpeed(20f);
                        break;
                    case 2:
                        player.EnablePasskey();
                        break;
                    case 3:
                        player.EnableRevive();
                        player.DecreaseMoveSpeed(30f);
                        break;
                }
                break;
            case 2:
                switch (cardNo){
                    case 1:
                        player.CreateDrone();
                        break;
                    case 2:
                        player.IncreaseAttackPower(1);
                        player.IncreaseAttackSpeed(50f);
                        break;
                    case 3:
                        player.EnableDelayBomb();
                        break;
                }
                break;
            case 3:
                switch (cardNo){
                    case 1:
                        player.IncreaseAttackSpeed(20f);
                        player.IncreaseDefense(10f);
                        player.IncreaseMoveSpeed(10f);
                        break;
                    case 2:
                        break;
                    case 3:
                        break;
                }
                break;
        }
        player.BattlePhase++;
        levelUpPanel.SetActive(false);
        Time.timeScale = 1;
    }

    // クリアまたは失敗画面を表示
    public void ShowResult(bool isClear)
    {
        resultPanel.SetActive(true);
        gameHUDPanel.SetActive(false);
        Time.timeScale = 0;

        if (isClear) {
            resultText.text = "Congratulation\nアカウントを保護しました"; 
        } else {
            resultText.text = "GAME OVER\nあなたの情報が漏洩しました"; 
        }
    }

    // 「？」ボタンを押したときに呼ばれる処理
    public void ShowHint(int cardNo)
    {
        hintPanel.SetActive(true); 

        // ヒントパネル自体の座標パーツを取得
        RectTransform panelRect = hintPanel.GetComponent<RectTransform>();
        RectTransform targetBtn = null; // どのボタンが押されたかを記憶する用

        switch (cardNo)
        {
            case 1:
                hintText.text = "【長いパスワード】\n文字数が多いほど解読時間は爆発的に伸びます。\n強力ですが、入力が面倒になるため「攻撃速度低下」のデメリットがついています。";
                targetBtn = HintButton_1;
                break;
            case 2:
                hintText.text = "【パスキー(Passkey)】\nこのカードの正体は最新の認証技術「パスキー」です。\nデバイス内蔵の鍵を使うため、偽サイト(フィッシング)による被害を完全に無効化します！";
                targetBtn = HintButton_2;
                break;
            case 3:
                hintText.text = "【二段階認証(2FA)】\nパスワードだけでなく、スマホのSMSなどに届くコードを使う防衛手段です。\n万が一パスワードが突破されても、二重の鍵でアカウントを復活・保護できます。";
                targetBtn = HintButton_3;
                break;
        }

        // ヒントパネルの「横位置(X)」だけを、押したボタンに合わせる
        if (panelRect != null && targetBtn != null)
        {
            Vector3 newPos = panelRect.position;
            newPos.x = targetBtn.position.x; 
            panelRect.position = newPos;
        }
    }

    // 「閉じる」ボタンを押したときに呼ばれる処理
    public void CloseHint()
    {
        hintPanel.SetActive(false); 
    }
}