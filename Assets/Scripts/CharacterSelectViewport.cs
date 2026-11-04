using UnityEngine;

// 個々の UI パーツを動かさず、画面全体だけをセーフエリアに合わせる。
[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
[AddComponentMenu("UI/Character Select Viewport")]
public class CharacterSelectViewport : MonoBehaviour
{
    public RectTransform content;
    public Vector2 referenceResolution = new Vector2(1920f, 1080f);
    public bool respectSafeArea = true;

    private RectTransform viewport;

    private void OnEnable()
    {
        viewport = (RectTransform)transform;
        FitContent();
    }

    private void LateUpdate()
    {
        FitContent();
    }

    private void FitContent()
    {
        if (content == null || viewport == null ||
            Screen.width <= 0 || Screen.height <= 0 ||
            referenceResolution.x <= 0f || referenceResolution.y <= 0f) return;

        Rect safe = respectSafeArea
            ? Screen.safeArea
            : new Rect(0f, 0f, Screen.width, Screen.height);
        Vector2 min = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
        Vector2 max = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
        if (viewport.anchorMin != min) viewport.anchorMin = min;
        if (viewport.anchorMax != max) viewport.anchorMax = max;
        if (viewport.offsetMin != Vector2.zero) viewport.offsetMin = Vector2.zero;
        if (viewport.offsetMax != Vector2.zero) viewport.offsetMax = Vector2.zero;

        if (content.sizeDelta != referenceResolution) content.sizeDelta = referenceResolution;
        float scale = Mathf.Min(viewport.rect.width / referenceResolution.x,
            viewport.rect.height / referenceResolution.y);
        if (scale <= 0f) return;
        Vector3 fittedScale = new Vector3(scale, scale, 1f);
        if (content.localScale != fittedScale) content.localScale = fittedScale;
    }
}
