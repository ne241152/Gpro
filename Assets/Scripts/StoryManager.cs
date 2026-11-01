
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

[System.Serializable]
public class StoryDialogueLine
{
    public string speakerName;

    [TextArea(2, 5)]
    public string dialogue;
}

[System.Serializable]
public class StoryDialoguePage
{
    public StoryDialogueLine[] lines;
}


public class StoryManager : MonoBehaviour
{
    [Header("スキップボタン")]
    public RectTransform skipButtonRect;

    [Header("ストーリー画像を表示するUI")]
    public Image storyImage;

    [Header("順番に表示するストーリー画像")]
    public Sprite[] storySprites;

    [Header("話している人の名前")]
    public TextMeshProUGUI speakerNameText;

    [Header("セリフ")]
    public TextMeshProUGUI dialogueText;

    [Header("画像ごとのセリフ")]
    public StoryDialoguePage[] storyPages;

    [Header("文字送りの速さ（秒）")]
    [Range(0.01f, 0.2f)]
    public float textSpeed = 0.05f;

    private int currentIndex = 0;
    private int currentLineIndex = 0;

    private Coroutine typingCoroutine;
    private bool isTyping = false;

    void Awake()
    {
        // 2周目でもゲーム内の時間が正常に進むようにする
        Time.timeScale = 1f;
    }
    
    void Start()
    {
        if (storyImage == null ||
            storySprites == null ||
            storySprites.Length == 0 ||
            speakerNameText == null ||
            dialogueText == null)
        {
            Debug.LogError("StoryManagerの設定が不足しています！");
            enabled = false;
            return;
        }

        ShowPage();
    }

    
    void Update()
    {
        bool spacePressed = Input.GetKeyDown(KeyCode.Space);
        bool pointerPressed = false;
        Vector2 pointerPosition = Vector2.zero;

        #if UNITY_ANDROID || UNITY_IOS
        if (Input.touchCount > 0 &&
            Input.GetTouch(0).phase == TouchPhase.Began)
        {
            pointerPressed = true;
            pointerPosition = Input.GetTouch(0).position;
        }
        #else
        if (Input.GetMouseButtonDown(0))
        {
            pointerPressed = true;
            pointerPosition = Input.mousePosition;
        }
        #endif

        // スキップボタン上のクリック・タップでは
        // 通常の文字送りを実行しない
        if (pointerPressed && skipButtonRect != null)
        {
            if (RectTransformUtility.RectangleContainsScreenPoint(
                skipButtonRect, pointerPosition, null))
            {
                pointerPressed = false;
            }
        }

        if (spacePressed || pointerPressed)
        {
            NextStory();
        }
    }


    void ShowPage()
    {
        storyImage.sprite = storySprites[currentIndex];
        currentLineIndex = 0;

        ShowLine();
    }

    StoryDialogueLine[] GetCurrentLines()
    {
        if (storyPages == null ||
            currentIndex >= storyPages.Length ||
            storyPages[currentIndex] == null)
        {
            return null;
        }

        return storyPages[currentIndex].lines;
    }

    void ShowLine()
    {
        StoryDialogueLine[] lines = GetCurrentLines();

        if (lines == null ||
            lines.Length == 0)
        {
            speakerNameText.text = "";
            dialogueText.text = "";
            return;
        }

        StoryDialogueLine line = lines[currentLineIndex];

        // 名前は最初から全部表示
        speakerNameText.text = line.speakerName;

        // セリフは最初は非表示
        dialogueText.text = line.dialogue;
        dialogueText.maxVisibleCharacters = 0;

        dialogueText.ForceMeshUpdate();

        // 1文字ずつ表示開始
        typingCoroutine = StartCoroutine(TypeText());
    }

    IEnumerator TypeText()
    {
        isTyping = true;

        int totalCharacters = dialogueText.textInfo.characterCount;

        for (int i = 1; i <= totalCharacters; i++)
        {
            dialogueText.maxVisibleCharacters = i;

            yield return new WaitForSeconds(textSpeed);
        }

        isTyping = false;
        typingCoroutine = null;
    }

    public void NextStory()
    {
        // 文字送りの途中なら全文表示
        if (isTyping)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;

            dialogueText.maxVisibleCharacters = int.MaxValue;
            isTyping = false;
            return;
        }

        StoryDialogueLine[] lines = GetCurrentLines();

        // 同じ画像に次のセリフがあれば表示
        if (lines != null &&
            currentLineIndex + 1 < lines.Length)
        {
            currentLineIndex++;
            ShowLine();
            return;
        }

        // セリフが終わったら次の画像へ
        currentIndex++;

        if (currentIndex < storySprites.Length)
        {
            ShowPage();
        }
        else
        {
            // 次はパスキー画面へ
            SceneManager.LoadScene("PasskeyScene");
        }
    }
    
     public void SkipStory()
    {
        SceneManager.LoadScene("PasskeyScene");
    }
}
