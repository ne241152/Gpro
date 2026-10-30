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

    [Header("Level Up Images")]
    public Sprite phase1Image;
    public Sprite phase2Image;
    public Sprite phase3Image;

    private Image levelUpPanelImage;
    
    [Header("Game Systems")]
    public WizardPlayerController player;
    public GameObject enemySpawner; 

    [Header("Barrier")]
    public LinkBarrierController linkBarrier;

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
        levelUpPanelImage = levelUpPanel.GetComponent<Image>();

        //ゲーム開始前はバリアを無効化
        if (linkBarrier != null)
        {
            linkBarrier.DeactivateBarrier();
        }

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
        hintPanel.SetActive(false);
        if (phase == 1)
        {
            levelUpPanelImage.sprite = phase1Image;
        }
        else if (phase == 2)
        {
            levelUpPanelImage.sprite = phase2Image;
        }
        else if (phase == 3)
        {
            levelUpPanelImage.sprite = phase3Image;
        }

        levelUpPanel.SetActive(true);

        Time.timeScale = 0;
    }

    // 強化カードを選んだ時の処理
    public void SelectSkillCard(int cardNo)
    {
        Debug.Log("Card " + cardNo + " が押されました");
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
                        player.EnableDelayBomb();
                        break;
                    case 2:
                        player.IncreaseAttackPowerPercent(50f);
                        player.IncreaseAttackSpeed(50f);
                        player.IncreaseMoveSpeed(50f);
                        break;
                    case 3:
                        player.CreateDrone();
                        break;
                }
                break;
            case 3:
                switch (cardNo){
                    case 1:
                        player.IncreaseAttackSpeed(20f);
                        player.IncreaseDefense(10f);
                        player.IncreaseMoveSpeed(10f);
                        // バリアを有効化
                        if (linkBarrier != null)
                        {
                            linkBarrier.ActivateBarrier();
                        }
                        else
                        {
                            Debug.LogWarning("LinkBarrierが設定されていません");
                        }
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
        Debug.Log("ヒントボタンが押されました: " + cardNo);
        hintPanel.SetActive(true); 

        switch (player.BattlePhase)
        {
            case 1:
                switch (cardNo)
                {
                    case 1:
                        hintText.text = "【長いパスワード】\n\n文字数が多いほど解読時間は爆発的に伸びます。\n強力ですが、入力が面倒になるため「攻撃速度低下」のデメリットがついています。";
                        break;
                    case 2:
                        hintText.text = "【パスキー(Passkey)】\n\nこのカードの正体は最新の認証技術「パスキー」です。\nデバイス内蔵の鍵を使うため、偽サイト(フィッシング)による被害を完全に無効化します！";
                        break;
                    case 3:
                        hintText.text = "【二段階認証(2FA)】\n\nパスワードだけでなく、スマホのSMSなどに届くコードを使う防衛手段です。\n万が一パスワードが突破されても、二重の鍵でアカウントを復活・保護できます。";
                        break;
                }
                break;
            case 2:
                switch (cardNo)
                {
                    case 1:
                        hintText.text = "【SMS認証】\n\nSMSで届く認証コードを確認し、入力することで本人確認を行います。\nコードの受信や入力には時間がかかるため、ゲームでは着弾までにラグがある強力な攻撃として表現しています。";
                        break;
                    case 2:
                        hintText.text = "【生体認証】\n\n指紋や顔などの身体的な特徴を使って本人確認を行う技術です。\nパスワード入力が不要で素早く認証できますが、端末の対応状況や利用環境によっては認証できない場合もあります。";
                        break;
                    case 3:
                        hintText.text = "【オートフィル（自動入力）】\n\n保存したIDやパスワードなどを自動入力し、ログインの手間を減らす機能です。\n便利ですが、利用できない場合は手入力が必要になるため、ゲームでは攻撃間隔が変動するドローンとして表現しています。";
                        break;
                }
                break;
            case 3:
                switch (cardNo)
                {
                    case 1:
                        hintText.text = "【シングルサインオン(SSO)】\n\n一度の認証で複数のサービスにログインできる便利な仕組みです。\nしかし、認証の中心となるアカウントが乗っ取られると、連携する複数のサービスにも被害が広がる危険があります。";
                        break;
                    case 2:
                        hintText.text = "【同期パスキー】\n\nパスキーは対応するクラウドサービスを通じて、複数の端末間で同期できます。\n同じパスキーを別の端末でも利用できるため、端末を変えてもパスワードを入力せずに、安全でスムーズなログインが可能になります。";
                        break;
                    case 3:
                        hintText.text = "【リスクベース認証】\n\nログインする場所や端末、普段と異なる操作などから不正アクセスの危険度を判断する仕組みです。\n危険度が低い場合は簡単にログインでき、高い場合は追加認証を求めることで、安全性と利便性を両立します。";
                        break;
                }
                break;
        }
    }

    // 「閉じる」ボタンを押したときに呼ばれる処理
    public void CloseHint()
    {
        hintPanel.SetActive(false); 
    }
}
