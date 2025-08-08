using UnityEngine;
using UnityEngine.InputSystem;

public class ARPlantController : MonoBehaviour
{
    [Header("������ ȸ�� ���� ����")]
    public Vector3 prefabDefaultRotation = new Vector3(90, 0, 0);
    public bool applyRotationCorrection = true;

    [Header("Input Actions")]
    public InputActionAsset inputActions;

    [Header("�巡�� ����")]
    public float dragSpeed = 0.5f;

    // Input Actions
    private InputAction touchAction;
    private InputAction touchPositionAction;
    private InputAction touchDeltaAction;

    private bool isDragging = false;
    private Vector2 lastTouchPosition;
    private Camera arCamera;

    // �̺�Ʈ
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
            Debug.Log($"{gameObject.name} : ȸ�� ���� ����� - {prefabDefaultRotation}");
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

        // TouchDelta�� ������ ���, ������ ���� ���
        if (touchDeltaAction != null)
        {
            delta = touchDeltaAction.ReadValue<Vector2>();
        }
        else if (touchPositionAction != null)
        {
            // �������� delta ���(���� �����Ӱ��� ����)
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

        // ȭ�� �巡�׸� ���� ��ǥ �̵����� ��ȯ
        Vector3 movement = new Vector3(delta.x, 0, delta.y) * dragSpeed * 0.001f;

        // ī�޶� �������� �̵� ���� ��ȯ
        movement = arCamera.transform.TransformDirection(movement);
        movement.y = 0;

        // ��ġ ������Ʈ
        transform.position += movement;

        // �Ÿ� ����
        ConstrainDistance();
    }

    private void ConstrainDistance()
    {
        if (arCamera == null) return;

        Vector3 toCamera = transform.position - arCamera.transform.position;
        toCamera.y = 0;
        float distance = toCamera.magnitude;

        // �Ÿ� ����
        if (distance < 0.5f || distance > 3f)
        {
            toCamera = toCamera.normalized * Mathf.Clamp(distance, 0.5f, 3f);
            Vector3 newPosition = arCamera.transform.position + toCamera;
            newPosition.y = transform.position.y;
            transform.position = newPosition;
        }
    }

    // ȸ�� ������ ����
    public void SetRotationCorrection(Vector3 newRotation)
    {
        prefabDefaultRotation = newRotation;
        ApplyRotationCorrection();
    }

    // ȸ�� ���� ��� ����
    public void ForceApplyRotationCorrection()
    {
        ApplyRotationCorrection();
    }

    // �巡�� Ȱ��ȭ/��Ȱ��ȭ
    public void SetDragEnabled(bool enabled)
    {
        this.enabled = enabled;
    }

    // ���� �巡�� ���� Ȯ��
    public bool IsDragging()
    {
        return isDragging;
    }
}
