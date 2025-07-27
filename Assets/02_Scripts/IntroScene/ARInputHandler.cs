using UnityEngine;
using UnityEngine.InputSystem;

public class ARInputHandler : MonoBehaviour
{
    [Header("Input Actions")]
    public InputActionAsset inputActions;

    [Header("터치 설정")]
    public float tapMaxDuration = 0.3f;
    public float tapMaxDistance = 50f;

    [Header("카메라 참조")]
    public Camera arCamera;

    // Input Actions
    private InputAction touchPositionAction;
    private InputAction touchPressAction;

    // 터치 상태
    private bool isTouching = false;
    private Vector2 touchStartPosition;
    private float touchStartTime;
    private bool inputEnabled = true;

    // 이벤트
    public System.Action<Vector2> OnSparrowTouched;

    private void Awake()
    {
        SetupInputActions();
    }

    private void SetupInputActions()
    {
        if (inputActions == null) return;

        var actionMap = inputActions.FindActionMap("Touch");
        touchPositionAction = actionMap.FindAction("TouchPosition");
        touchPressAction = actionMap.FindAction("TouchPress");
    }

    private void OnEnable()
    {
        touchPressAction?.Enable();
        touchPositionAction?.Enable();

        if(touchPressAction != null)
        {
            touchPressAction.started += OnTouchStarted;
            touchPressAction.canceled += OnTouchEnded;
        }
    }

    private void OnDisable()
    {
        if (touchPressAction != null)
        {
            touchPressAction.started += OnTouchStarted;
            touchPressAction.canceled += OnTouchEnded;
        }

        touchPressAction?.Disable();
        touchPositionAction?.Disable();
    }

    private void OnTouchStarted(InputAction.CallbackContext context)
    {
        if (!inputEnabled) return;

        isTouching = true;
        touchStartTime = Time.time;
        touchStartPosition = GetTouchPosition();
    }

    private void OnTouchEnded(InputAction.CallbackContext context)
    {
        if (!inputEnabled || !isTouching) return;

        isTouching = false;
        Vector2 touchEndPosition = GetTouchPosition();
        float touchDuration = Time.time - touchStartTime;
        float touchDistance = Vector2.Distance(touchStartPosition, touchEndPosition);

        // 탭 판정
        if(touchDuration <= tapMaxDuration && touchDistance <= tapMaxDistance)
        {
            ProcessTap(touchEndPosition);
        }

        Debug.Log($"Touch ended. Duration: {touchDuration:F2}s, Distance: {touchDistance:F1}px");
    }

    private void ProcessTap(Vector2 screenPosition)
    {
        Debug.Log($"Tap detected at : {screenPosition}");

        // 참새 터치 확인
        if (IsSparrowTouch(screenPosition))
        {
            OnSparrowTouched?.Invoke(screenPosition);
        }
    }

    private bool IsSparrowTouch(Vector2 screenPosition)
    {
        // SparrowController에서 참새 위치 확인
        var sparrowController = FindAnyObjectByType<SparrowController>();
        if (sparrowController == null || !sparrowController.IsActive()) return false;

        Vector3 sparrowWorldPos = sparrowController.GetSparrowPosition();
        if(sparrowWorldPos == Vector3.zero) return false;

        Vector3 sparrowScreenPos = arCamera.WorldToScreenPoint(sparrowWorldPos);
        if(sparrowScreenPos.z < 0) return false;

        Vector2 sparrowScreen2D = new Vector2(sparrowScreenPos.x, sparrowScreenPos.y);
        float distance = Vector2.Distance(screenPosition, sparrowScreen2D);

        // 터치 허용 반경
        return distance <= 100f;
    }

    private Vector2 GetTouchPosition()
    {
        return touchPositionAction?.ReadValue<Vector2>() ?? Vector2.zero;
    }

    // 공개 메서드들
    public void SetInputEnabled(bool enabled) => inputEnabled = enabled;
    public bool IsInputEnabled() => inputEnabled;
}
