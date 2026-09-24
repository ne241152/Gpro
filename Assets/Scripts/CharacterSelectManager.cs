using UnityEngine;
using UnityEngine.UI;
using TMPro; 
using UnityEngine.SceneManagement; // 1. シーン遷移を使うために追加

public class CharacterSelectManager : MonoBehaviour
{
    [Header("UIへの割り当て")]
    public Image characterImageDisplay; 
    public TextMeshProUGUI nameText;    
    public TextMeshProUGUI descriptionText; 

    [System.Serializable]
    public struct CharacterData
    {
        public Sprite characterSprite;
        public string characterName;
        [TextArea(3, 5)]
        public string description;
    }

    [Header("キャラクターの設定データ")] 
    public CharacterData[] characters;

    private int currentIndex = 0; 

    void Start()
    {
        UpdateUI();
    }

    public void NextCharacter()
    {
        currentIndex++;
        if (currentIndex >= characters.Length)
        {
            currentIndex = 0;
        }
        UpdateUI();
    }

    public void PrevCharacter()
    {
        currentIndex--;
        if (currentIndex < 0)
        {
            currentIndex = characters.Length - 1;
        }
        UpdateUI();
    }

    private void UpdateUI()
    {
        characterImageDisplay.sprite = characters[currentIndex].characterSprite;
        nameText.text = characters[currentIndex].characterName;
        descriptionText.text = characters[currentIndex].description;
    }

    // 2. [決定] ボタンを押した時の処理を有効化
    public void ConfirmSelection()
    {
        Debug.Log(characters[currentIndex].characterName + " が選ばれました！");
        
        // 選ばれたキャラの番号（0, 1, 2...）を保存
        PlayerPrefs.SetInt("SelectedCharacterID", currentIndex);
        // 「セレクト画面から本編に戻ってきたよ」という目印（フラグ）を立てる
        PlayerPrefs.SetInt("IsGameStart", 1); 
        PlayerPrefs.Save();
        
        // あなたの言う通り、元の SampleScene に戻る
        UnityEngine.SceneManagement.SceneManager.LoadScene("SampleScene");
    }
}