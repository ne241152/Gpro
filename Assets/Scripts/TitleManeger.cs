using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleManeger : MonoBehaviour
{
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
        SceneManager.LoadScene("Select Scene");
    }
}