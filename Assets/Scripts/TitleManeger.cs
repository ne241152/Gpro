using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleManeger : MonoBehaviour
{
    // スタートボタンを押したらストーリー画面へ

    [SerializeField] private GameObject howToPlayPanel;

    public void OpenHowToPlay()
    {
        howToPlayPanel.SetActive(true);
    }

    public void CloseHowToPlay()
    {
        howToPlayPanel.SetActive(false);
    }

    public void GoToSelectScene()
    {
        SceneManager.LoadScene("StoryScene");
    }
}

