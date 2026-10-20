
using UnityEngine;

public class LandscapeNotice : MonoBehaviour
{
    public GameObject rotateNotice;

    void Update()
    {
        if (rotateNotice == null) return;

        // 画面が縦長なら案内を表示
        bool isPortrait = Screen.height > Screen.width;

        if (rotateNotice.activeSelf != isPortrait)
        {
            rotateNotice.SetActive(isPortrait);
        }
    }
}
