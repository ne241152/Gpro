
using UnityEngine;
using UnityEngine.SceneManagement;

public class PasskeySceneManager : MonoBehaviour
{
    [Header("仮の画面遷移（認証実装後に変更予定）")]
    [SerializeField] private string nextSceneName = "Select Scene";

    void Update()
    {
        // PC：スペースキー
        if (Input.GetKeyDown(KeyCode.Space))
        {
            GoToNextScene();
            return;
        }

        // PC：マウスクリック
        if (Input.GetMouseButtonDown(0))
        {
            GoToNextScene();
            return;
        }

        // スマホ：画面タップ
        if (Input.touchCount > 0 &&
            Input.GetTouch(0).phase == TouchPhase.Began)
        {
            GoToNextScene();
        }
    }

    public void GoToNextScene()
    {
        SceneManager.LoadScene("Select Scene");
    }
}
