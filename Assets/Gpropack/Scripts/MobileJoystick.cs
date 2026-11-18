using UnityEngine;
using UnityEngine.EventSystems;

public class MobileJoystick : MonoBehaviour,
    IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("スティックのUI")]
    public RectTransform joystickBackground;
    public RectTransform joystickHandle;

    [Header("操作設定")]
    public float movementRadius = 70f;

    public Vector2 Direction { get; private set; }
    public bool IsActive { get; private set; }

    private int activePointerId;
    private Vector2 startPoint;

    void Awake()
    {
        if (joystickBackground != null)
        {
            joystickBackground.gameObject.SetActive(true);

            // 基盤全体でタッチを受け取れるようにする
            var bgImage = joystickBackground.GetComponent<UnityEngine.UI.Image>();
            if (bgImage != null) bgImage.raycastTarget = true;
        }
        ResetJoystick();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (IsActive || joystickBackground == null ||
            joystickHandle == null) return;

        // ハンドルだけでなく基盤のどこを触っても操作開始
        if (!RectTransformUtility.RectangleContainsScreenPoint(
            joystickBackground,
            eventData.position,
            eventData.pressEventCamera)) return;

        // 押した位置を基準点(ニュートラル)として記録する
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            joystickBackground,
            eventData.position,
            eventData.pressEventCamera,
            out startPoint)) return;

        activePointerId = eventData.pointerId;
        IsActive = true;
        Direction = Vector2.zero;

        joystickHandle.anchoredPosition = Vector2.zero;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!IsActive || eventData.pointerId != activePointerId)
            return;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            joystickBackground,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 currentPoint)) return;

        // 押した位置からどれだけ動かしたか
        Vector2 offset = currentPoint - startPoint;

        float radius = Mathf.Max(1f, movementRadius);

        Direction = Vector2.ClampMagnitude(offset / radius, 1f);

        joystickHandle.anchoredPosition = Direction * radius;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.pointerId != activePointerId) return;
        ResetJoystick();
    }

    void OnDisable()
    {
        ResetJoystick();
    }

    private void ResetJoystick()
    {
        Direction = Vector2.zero;
        IsActive = false;

        if (joystickHandle != null)
            joystickHandle.anchoredPosition = Vector2.zero;

    }
}
