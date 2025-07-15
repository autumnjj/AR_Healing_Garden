using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;

public class ARInputManager : MonoBehaviour
{
    [Header("Input Actions")]
    public InputActionAsset inputActions;

    [Header("AR Components")]
    public ARRaycastManager raycastManager;
    public Camera arCamera;

    [Header("Plant Managerment")]
    public ARPlantManager plantManager;
    public PlantInteractionSystem interactionSystem;

    [Header("Input Settings")]
    public float tapMaxDuration = 0.3f;
    public float tapMaxDistance = 50f;
    public LayerMask plantLayerMask = -1;

    // Input Actions
    private InputAction touchAction;
    private InputAction touchPositionAction;
    private InputAction touchPressAction;
    private InputAction[] plantInteractionActions = new InputAction[4];
    private InputAction resetPlantAction;

    // Touch tracking
    private Vector2 touchStartPosition;
    private float touchStartTime;
    private bool isTouching = false;

    // Raycast hits
    private List<ARRaycastHit> raycastHits = new List<ARRaycastHit>();

    private void Awake()
    {
        SetupInputActions();
    }

    private void SetupInputActions()
    {
        if (inputActions == null) return;

        var actionMap = inputActions.FindActionMap("AR Plant Interaction");

        // Touch Actions
        touchAction = actionMap.FindAction("Touch");
        touchPositionAction = actionMap.FindAction("TouchPosition");
        touchPressAction = actionMap.FindAction("TouchPress");

        // Plant Interaction Actions
        plantInteractionActions[0] = actionMap.FindAction("PlantInteraction1");
        plantInteractionActions[1] = actionMap.FindAction("PlantInteraction2");
        plantInteractionActions[2] = actionMap.FindAction("PlantInteraction3");
        plantInteractionActions[3] = actionMap.FindAction("Plantinteraction4");

        // Reset Action
        resetPlantAction = actionMap.FindAction("ResetPlant");
    }

    private void OnEnable()
    {
        EnableInputActions();
        SubscribeToInputEvents();
    }

    private void OnDisable()
    {
        UnsubscribeFromInputEvents();
        DisableInputActions();
    }

    private void EnableInputActions()
    {
        touchAction?.Enable();
        touchPositionAction?.Enable();
        touchPressAction?.Enable();

        foreach(var action in plantInteractionActions)
        {
            action?.Enable();
        }

        resetPlantAction?.Enable();
    }

    private void DisableInputActions()
    {
        touchAction?.Disable();
        touchPositionAction?.Disable();
        touchPressAction?.Disable();

        foreach(var action in plantInteractionActions)
        {
            action?.Disable();
        }

        resetPlantAction?.Disable();
    }

    private void SubscribeToInputEvents()
    {
        if (touchPressAction != null)
        {
            touchPressAction.started += OnTouchStarted;
            touchPressAction.canceled += OnTouchEnded;
        }

        // Plant Interaction Events
        for (int i = 0; i < plantInteractionActions.Length; i++)
        {
            if(plantInteractionActions[i] != null)
            {
                int interactionIndex = i; //로컬 변수로 캡처
                plantInteractionActions[i].performed += _ => OnPlantInteractionPerformed(interactionIndex);
            }
        }

        if(resetPlantAction != null)
        {
            resetPlantAction.performed += OnResetPlantPerformed;
        }
    }

    private void UnsubscribeFromInputEvents()
    {
        if (touchPressAction != null)
        {
            touchPressAction.started -= OnTouchStarted;
            touchPressAction.canceled -= OnTouchEnded;
        }

        for(int i = 0; i < plantInteractionActions.Length; i++)
        {
            if (plantInteractionActions[i] != null)
            {
                int interactionIndex = i;
                plantInteractionActions[i].performed -= _ => OnPlantInteractionPerformed(interactionIndex);
            }
        }

        if (resetPlantAction != null) 
        {
            resetPlantAction.performed -= OnResetPlantPerformed;
        }
    }

    private void OnTouchStarted(InputAction.CallbackContext context)
    {
        isTouching = true;
        touchStartTime = Time.time;
        touchStartPosition = touchPositionAction.ReadValue<Vector2>();

        Debug.Log($"Touch Started at : {touchStartPosition}");
    }

    private void OnTouchEnded(InputAction.CallbackContext context) 
    {
        if (!isTouching) return;

        isTouching = false;
        Vector2 touchEndPosition = touchPositionAction.ReadValue<Vector2>();
        float touchDuration = Time.time - touchStartTime;
        float touchDistance = Vector2.Distance(touchStartPosition, touchEndPosition);

        // 탭인지 확인(짧은 시간, 짧은 거리)
        if(touchDuration <= tapMaxDistance && touchDistance <= tapMaxDistance)
        {
            ProcessTap(touchEndPosition);
        }

        Debug.Log($"Touch Ended. Duration : {touchDuration}, Distance : {touchDistance}");
    }

    private void ProcessTap(Vector2 screenPosition)
    {
        if (plantManager == null || arCamera == null) return;

        // 식물이 배치되지 않았다면 배치 시도
        if (!plantManager.IsPlantPlaced())
        {
            ProcessPlantPlacement(screenPosition);
        }
        else
        {
            // 식물이 배치되었다면 상호작용 시도
            ProcessPlantInteraction(screenPosition);
        }
    }

    private void ProcessPlantPlacement(Vector2 screenPosition)
    {
        raycastHits.Clear();

        if(raycastManager.Raycast(screenPosition, raycastHits, TrackableType.PlaneWithinPolygon))
        {
            // 수평 평면 우선 선택
            ARRaycastHit selectedHit = raycastHits[0];
            foreach(var hit in raycastHits)
            {
                var plane = hit.trackable as ARPlane;
                if(plane != null && plane.alignment == PlaneAlignment.HorizontalUp)
                {
                    selectedHit = hit;
                    break;
                }
            }

            // 식물 배치
            if(plantManager != null)
            {
                plantManager.PlacePlantAtPosition(selectedHit.pose);
                Debug.Log("Plant placed via touch input!");
            }
        }
    }

    private void ProcessPlantInteraction(Vector2 screenPosition)
    {
        Ray ray = arCamera.ScreenPointToRay(screenPosition);
        RaycastHit hit;

        if(Physics.Raycast(ray, out hit, Mathf.Infinity, plantLayerMask))
        {
            // 식물 오브젝트인지 확인
            var plantController = hit.collider.GetComponent<ARPlantController>();
            if(plantController == null)
            {
                plantController = hit.collider.GetComponentInChildren<ARPlantController>();
            }

            if(plantController != null)
            {
                // 기본 터치 상호작용 수행
                PerformPlantInteraction(InteractionType.TouchCare);

                // 시각적 피드백
                plantController.OnTouchFeedback(hit.point);

                Debug.Log("Plant touched via input system!");
            }
        }
    }

    private void OnPlantInteractionPerformed(int interactionIndex)
    {
        if (plantManager == null || !plantManager.IsPlantPlaced()) return;

        InteractionType interactionType = (InteractionType)interactionIndex;
        PerformPlantInteraction(interactionType);

        Debug.Log($"Plant interaction performed : {interactionType} via keyboard");
    }

    private void PerformPlantInteraction(InteractionType interactionType)
    {
        if(interactionSystem != null)
        {
            interactionSystem.PerformInteraction(interactionType);
        }
    }

    private void OnResetPlantPerformed(InputAction.CallbackContext context)
    {
        if(plantManager != null)
        {
            plantManager.ResetPlantPlacement();
            Debug.Log("Plant reset via input system");
        }
    }

    // 공개 메서드들
    public bool IsTouching()
    {
        return isTouching;
    }

    public Vector2 GetCurrentTouchPosition()
    {
        return touchPositionAction.ReadValue<Vector2>();
    }

    public void SetPlantLayerMask(LayerMask layerMask)
    {
        plantLayerMask = layerMask;
    }

    // 특정 상황에서 입력 비활성화/활성화
    public void SetInputEnabled(bool enabled)
    {
        if (enabled)
        {
            EnableInputActions();
        }
        else
        {
            DisableInputActions();
        }
    }
}
