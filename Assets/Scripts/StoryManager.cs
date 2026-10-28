
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class StoryManager : MonoBehaviour
{
    [Header("ストーリー画像を表示するUI")]
    public Image storyImage;

    [Header("順番に表示するストーリー画像")]
    public Sprite[] storySprites;

    private int currentIndex = 0;

    void Start()
    {
        if (storyImage == null || storySprites.Length == 0)
        {
            Debug.LogError("ストーリー画像が設定されていません！");
            enabled = false;
            return;
        }

        // 最初の画像を表示
        storyImage.sprite = storySprites[currentIndex];
    }

    
    void Update()
    {
        bool next = Input.GetKeyDown(KeyCode.Space);

        // スマホでは画面タップ、PCではマウスクリック
    #if UNITY_ANDROID || UNITY_IOS
        if (Input.touchCount > 0 &&
            Input.GetTouch(0).phase == TouchPhase.Began)
        {
            next = true;
        }
    #else
        if (Input.GetMouseButtonDown(0))
        {
            next = true;
        }
    #endif

        if (next)
        {
            NextStory();
        }
    }


    
    public void NextStory()
    {
        currentIndex++;

        if (currentIndex < storySprites.Length)
        {
            // 次のストーリー画像を表示
            storyImage.sprite = storySprites[currentIndex];
        }
        else
        {
            // 4枚のストーリーが終わったらキャラ選択画面へ
            SceneManager.LoadScene("Select Scene");
        }
    }

}
