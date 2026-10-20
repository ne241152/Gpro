
using UnityEngine;
using TMPro;
using System.Runtime.InteropServices;

public class MobileSelectLayout : MonoBehaviour
{
    public RectTransform prevButton;
    public RectTransform nextButton;
    public RectTransform confirmButton;

    [Header("Unity内でスマホ表示を試す")]
    public bool previewMobileInEditor = false;

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern int GproIsPhone();
#endif

    void Start()
    {
        bool isMobile = false;

#if UNITY_WEBGL && !UNITY_EDITOR
        isMobile = GproIsPhone() == 1;
#elif UNITY_EDITOR
        isMobile = previewMobileInEditor;
#endif

        // PCでは既存のUIを一切変更しない
        if (!isMobile) return;

        RectTransform canvasRect = GetComponent<RectTransform>();

        if (canvasRect == null ||
            prevButton == null ||
            nextButton == null ||
            confirmButton == null)
        {
            Debug.LogWarning("MobileSelectLayoutの設定が不足しています");
            return;
        }

        float w = canvasRect.rect.width;
        float h = canvasRect.rect.height;

        SetButton(
            prevButton,
            new Vector2(0.17f, 0.5f),
            new Vector2(w * 0.15f, h * 0.18f),
            90f
        );

        SetButton(
            nextButton,
            new Vector2(0.83f, 0.5f),
            new Vector2(w * 0.15f, h * 0.18f),
            90f
        );

        SetButton(
            confirmButton,
            new Vector2(0.5f, 0.1f),
            new Vector2(w * 0.43f, h * 0.13f),
            65f
        );
    }

    void SetButton(
        RectTransform button,
        Vector2 anchor,
        Vector2 size,
        float fontSize)
    {
        button.anchorMin = anchor;
        button.anchorMax = anchor;
        button.pivot = new Vector2(0.5f, 0.5f);
        button.anchoredPosition = Vector2.zero;
        button.sizeDelta = size;
        button.localScale = Vector3.one;

        TextMeshProUGUI text =
            button.GetComponentInChildren<TextMeshProUGUI>(true);

        if (text != null)
        {
            text.enableAutoSizing = true;
            text.fontSizeMin = 20f;
            text.fontSizeMax = fontSize;
            text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.Center;
        }
    }
}
