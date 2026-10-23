
using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleManager : MonoBehaviour
{
    // スタートボタンを押したらストーリー画面へ
    public void GoToSelectScene()
    {
        SceneManager.LoadScene("StoryScene");
    }
}
