using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
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

    private RectTransform touchArea;
    private int activePointerId;

    void Awake()
    {
        touchArea = GetComponent<RectTransform>();

        if (joystickBackground != null)
            joystickBackground.gameObject.SetActive(false);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log("スティックがタッチされました！");
        if (IsActive || joystickBackground == null ||
            joystickHandle == null) return;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            touchArea,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPoint)) return;

        activePointerId = eventData.pointerId;
        IsActive = true;
        Direction = Vector2.zero;

        joystickBackground.anchoredPosition = localPoint;
        joystickHandle.anchoredPosition = Vector2.zero;

        joystickBackground.gameObject.SetActive(true);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!IsActive || eventData.pointerId != activePointerId)
            return;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            touchArea,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 currentPoint)) return;

        Vector2 offset =
            currentPoint - joystickBackground.anchoredPosition;

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

        if (joystickBackground != null)
            joystickBackground.gameObject.SetActive(false);
    }
}
