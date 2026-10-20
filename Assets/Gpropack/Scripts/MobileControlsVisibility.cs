
using UnityEngine;

public class MobileControlsVisibility : MonoBehaviour
{
    public GameObject touchArea;
    public GameObject titlePanel;
    public GameObject levelUpPanel;
    public GameObject resultPanel;

    void Update()
    {
        bool isPlaying =
            !IsOpen(titlePanel) &&
            !IsOpen(levelUpPanel) &&
            !IsOpen(resultPanel) &&
            Time.timeScale > 0f;

        if (touchArea != null &&
            touchArea.activeSelf != isPlaying)
        {
            touchArea.SetActive(isPlaying);
        }
    }

    bool IsOpen(GameObject panel)
    {
        return panel != null && panel.activeInHierarchy;
    }
}
