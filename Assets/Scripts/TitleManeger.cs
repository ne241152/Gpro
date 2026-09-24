using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleManager : MonoBehaviour
{
    // タイトルの「スタート」等のボタンに割当てる関数
    public void GoToSelectScene()
    {
        SceneManager.LoadScene("Select Scene");
    }
}