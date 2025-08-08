using UnityEngine;
using UnityEngine.InputSystem;

public class ARPlantController : MonoBehaviour
{
    [Header("프리팹 회전 보정 설정")]
    public Vector3 prefabDefaultRotation = new Vector3(90, 0, 0);
    public bool applyRotationCorrection = true;

    [Header("Input Actions")]
    public InputActionAsset inputActions;

    [Header("드래그 설정")]
    public float dragSpeed = 0.5f;

    // Input Actions
    private InputAction touchAction;
    private InputAction touchPositionAction;
    private InputAction touchDeltaAction;

    private bool isDragging = false;
    private Vector2 lastTouchPosition;
    private Camera arCamera;

    // 이벤트
    public System.Action<Vector2> OnDragDelta;
    public System.Action OnDragStart;
    public System.Action OnDragEnd;

    private void Start()
    {
        ApplyRotationCorrection();
        SetupInputActions();
        FindARCamera();
    }

   
    private void FindARCamera()
    {
        arCamera = Camera.main;
        if (arCamera == null )
            arCamera = FindAnyObjectByType<Camera>();
    }

    private void ApplyRotationCorrection()
    {
        if (applyRotationCorrection)
        {
            transform.rotation = Quaternion.Euler(prefabDefaultRotation);
            Debug.Log($"{gameObject.name} : 회전 보정 적용됨 - {prefabDefaultRotation}");
        }
    }

    private void SetupInputActions()
    {
        if (inputActions == null) return;

        var actionMap = inputActions.FindActionMap("AR Plant");
        if (actionMap != null)
        {
            touchAction = actionMap.FindAction("Touch");
            touchPositionAction = actionMap.FindAction("TouchPosition");
            touchDeltaAction = actionMap.FindAction("TouchDelta");

            if (touchDeltaAction == null)
                Debug.LogWarning("TouchDelta action not found. Creating fallback");
        }
    }

    private void OnEnable()
    {
        if (touchAction != null)
        {
            touchAction.started += OnTouchStarted;
            touchAction.canceled += OnTouchEnded;
            touchAction?.Enable();
        }

        touchPositionAction?.Enable();
        touchDeltaAction?.Enable();
    }

    private void OnDisable()
    {
        if (touchAction != null)
        {
            touchAction.started -= OnTouchStarted;
            touchAction.canceled -= OnTouchEnded;
            touchAction?.Disable();
        }

        touchPositionAction?.Disable();
        touchDeltaAction?.Disable();
    }

    private void OnTouchStarted(InputAction.CallbackContext context)
    {
       isDragging = true;
        lastTouchPosition = Vector2.zero;
        OnDragStart?.Invoke();
        Debug.Log("Touch started on" + gameObject.name);
    }

    private void OnTouchEnded(InputAction.CallbackContext context)
    {
        isDragging = false;
        OnDragEnd?.Invoke();
        Debug.Log("Touch ended on " + gameObject.name);
    }

    private void Update()
    {
        if (isDragging)
            HandleDragging();
    }

    private void HandleDragging()
    {
        Vector2 delta = Vector2.zero;

        // TouchDelta가 있으면 사용, 없으면 수동 계산
        if (touchDeltaAction != null)
        {
            delta = touchDeltaAction.ReadValue<Vector2>();
        }
        else if (touchPositionAction != null)
        {
            // 수동으로 delta 계산(이전 프레임과의 차이)
            Vector2 currentPos = touchPositionAction.ReadValue<Vector2>();
            if (lastTouchPosition != Vector2.zero)
            {
                delta = currentPos - lastTouchPosition;
            }
            lastTouchPosition = currentPos;
        }

        if (delta != Vector2.zero)
        {
            OnDragDelta?.Invoke(delta);

            AdjustPosition(delta);
        }
         
    }

    public void AdjustPosition(Vector2 delta)
    {
        if (arCamera == null) return;

        // 화면 드래그를 월드 좌표 이동으로 변환
        Vector3 movement = new Vector3(delta.x, 0, delta.y) * dragSpeed * 0.001f;

        // 카메라 기준으로 이동 방향 변환
        movement = arCamera.transform.TransformDirection(movement);
        movement.y = 0;

        // 위치 업데이트
        transform.position += movement;

        // 거리 제한
        ConstrainDistance();
    }

    private void ConstrainDistance()
    {
        if (arCamera == null) return;

        Vector3 toCamera = transform.position - arCamera.transform.position;
        toCamera.y = 0;
        float distance = toCamera.magnitude;

        // 거리 제한
        if (distance < 0.5f || distance > 3f)
        {
            toCamera = toCamera.normalized * Mathf.Clamp(distance, 0.5f, 3f);
            Vector3 newPosition = arCamera.transform.position + toCamera;
            newPosition.y = transform.position.y;
            transform.position = newPosition;
        }
    }

    // 회전 보정값 변경
    public void SetRotationCorrection(Vector3 newRotation)
    {
        prefabDefaultRotation = newRotation;
        ApplyRotationCorrection();
    }

    // 회전 보정 즉시 적용
    public void ForceApplyRotationCorrection()
    {
        ApplyRotationCorrection();
    }

    // 드래그 활성화/비활성화
    public void SetDragEnabled(bool enabled)
    {
        this.enabled = enabled;
    }

    // 현재 드래그 상태 확인
    public bool IsDragging()
    {
        return isDragging;
    }
}
