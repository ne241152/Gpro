using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class CharacterSelectManager : MonoBehaviour
{
    [Header("UIへの割り当て")]
    public Image characterImageDisplay;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI recommendationText;

    [Header("操作ボタン")]
    public Button previousButton;
    public Button nextButton;
    public Button confirmButton;

    [Header("決定後のシーン")]
    public string gameplaySceneName = "SampleScene";

    [System.Serializable]
    public struct CharacterData
    {
        public Sprite characterSprite;
        public string characterName;
        [TextArea(3, 5)]
        public string description;
        [TextArea(2, 3)]
        public string recommendation;
    }

    [Header("キャラクターの設定データ")] 
    public CharacterData[] characters;

    private int currentIndex;

    public int CurrentIndex => currentIndex;

    void Start()
    {
        SelectCharacter(currentIndex);
    }

    public void NextCharacter()
    {
        SelectCharacter(currentIndex + 1);
    }

    public void PrevCharacter()
    {
        SelectCharacter(currentIndex - 1);
    }

    // ボタン操作と Inspector のプレビューで同じ表示処理を使用する。
    public void SelectCharacter(int index)
    {
        int count = characters == null ? 0 : characters.Length;
        if (previousButton != null) previousButton.interactable = count > 1;
        if (nextButton != null) nextButton.interactable = count > 1;
        if (confirmButton != null) confirmButton.interactable = count > 0;

        if (count == 0)
        {
            if (characterImageDisplay != null) characterImageDisplay.enabled = false;
            if (nameText != null) nameText.text = string.Empty;
            if (descriptionText != null) descriptionText.text = string.Empty;
            if (recommendationText != null) recommendationText.text = string.Empty;
            return;
        }

        currentIndex = ((index % count) + count) % count;
        CharacterData character = characters[currentIndex];
        if (characterImageDisplay != null)
        {
            characterImageDisplay.sprite = character.characterSprite;
            characterImageDisplay.enabled = character.characterSprite != null;
        }
        if (nameText != null) nameText.text = character.characterName;
        if (descriptionText != null) descriptionText.text = character.description;
        if (recommendationText != null) recommendationText.text = character.recommendation;
    }

    public void ConfirmSelection()
    {
        if (characters == null || characters.Length == 0) return;

        Debug.Log(characters[currentIndex].characterName + " が選ばれました！");

        // 既存の PlayerSpawner が使うキャラクター番号と開始フラグを維持。
        PlayerPrefs.SetInt("SelectedCharacterID", currentIndex);
        PlayerPrefs.SetInt("IsGameStart", 1);
        PlayerPrefs.Save();

        SceneManager.LoadScene(gameplaySceneName);
    }
}
