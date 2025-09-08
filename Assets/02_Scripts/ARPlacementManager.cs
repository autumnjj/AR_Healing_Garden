using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ARPlacementManager : MonoBehaviour
{
    [Header("AR Foundation Components")]
    public ARRaycastManager raycastManager;
    public ARAnchorManager anchorManager;
    public ARPlaneManager planeManager;
    public Camera arCamera;

    [Header("Input Actions")]
    public InputActionReference touchPositionAction;
    public InputActionReference touchPressAction;

    [Header("Pre-placed Prefabs")]
    public GameObject preplacedTable;
    public Transform plantSpawnPoint;
    public GameObject preplacedSeed;

    [Header("UI")]
    public TextMeshProUGUI instructionText;
    public GameObject voiceUI;
    public GameObject placementUI;

    [Header("AR Settings")]
    public float plantScale = 1.0f;
    public float initialSetupDelay = 1.0f;

    private ARAnchor plantAnchor;
    private List<ARRaycastHit> raycastHits = new List<ARRaycastHit>();
    private bool isPlacementMode = true;
    private bool isCreatingAnchor = false;

    // Input Action
    private Vector2 touchPosition;

    private Vector3 seedOriginalPosition;  
    private Quaternion seedOriginalRotation;

    private GameObject currentTableInstance;
    private GameObject currentPlantInstance;

    public bool IsPlaced { get; private set; } = false;

    public System.Action OnPlacementComplete;

    private void Start()
    {
        SetupAR();
        SetupInputActions();
        InitializeUI();
        StartPlacementMode();
    }

    private void SetupAR()
    {
        if (arCamera == null)
            arCamera = Camera.main ?? FindAnyObjectByType<Camera>();

        if (raycastManager == null)
            raycastManager = FindAnyObjectByType<ARRaycastManager>();

        if (anchorManager == null)
            anchorManager = FindAnyObjectByType<ARAnchorManager>();

        if (planeManager == null)
            planeManager = FindAnyObjectByType<ARPlaneManager>();

        Debug.Log("AR Foundation components initialized");
    }

    private void SetupInputActions()
    {
        if (touchPositionAction != null)
            touchPositionAction.action.Enable();

        if (touchPressAction != null)
        {
            touchPressAction.action.Enable();
            touchPressAction.action.started += OnTouchStarted;
            touchPressAction.action.canceled += OnTouchEnded;
        }
    }

    private void OnTouchStarted(InputAction.CallbackContext context)
    {
        if (isPlacementMode && !IsPlaced && !isCreatingAnchor)
        {
            if (touchPositionAction != null)
            {
                touchPosition = touchPositionAction.action.ReadValue<Vector2>();
                _ = AttemptPlacementAsync();
            }
        }
    }

    private void OnTouchEnded(InputAction.CallbackContext context)
    {

    }

    private void InitializeUI()
    {
        if (voiceUI != null)
            voiceUI.SetActive(false);

        if (placementUI != null)
            placementUI.SetActive(true);
    }

    private void StartPlacementMode()
    {
        isPlacementMode = true;
        UpdateInstruction("ī�޶� õõ�� ������ �ٴ��̳� ���̺��� �����ּ���.\n" +
            "����� �����Ǹ� ��ġ�ؼ� ���̺��� ��ġ�ϼ���!");
    }
    private void Update()
    {
        if (isPlacementMode && !IsPlaced)
            CheckPlaneDetection();

        if (touchPositionAction != null && touchPositionAction.action.enabled)
            touchPosition = touchPositionAction.action.ReadValue<Vector2>();
    }

    private void CheckPlaneDetection()
    {
        // ȭ�� �߾ӿ��� ��� ����
        Vector3 screenCenter = arCamera.ViewportToScreenPoint(new Vector3(0.5f, 0.5f, 0f));

        if (raycastManager.Raycast(screenCenter, raycastHits, TrackableType.PlaneWithinPolygon))
        {
            // ����� �����Ǹ� ��ġ ���� �޽���
            UpdateInstruction("����� �����Ǿ����ϴ�!\n��ġ�ؼ� �Ĺ��� ��ġ�ϼ���!");
        }
        else
        {
            UpdateInstruction("����� ã�� �ֽ��ϴ�...\nī�޶� õõ�� ������ �ٴ��̳� ���̺��� �����ּ���!");
        }
    }

    private async Task AttemptPlacementAsync()
    {
        if (isCreatingAnchor) return;

        isCreatingAnchor = true;
        UpdateInstruction("�Ĺ��� ��ġ�ϴ� ��...");

        try
        {
            if (raycastManager.Raycast(touchPosition, raycastHits, TrackableType.PlaneWithinPolygon))
            {
                await PlaceWithARAnchorAsync(raycastHits[0].pose);
            }
            else
            {
                Vector3 screenCenter = arCamera.ViewportToScreenPoint(new Vector3(0.5f, 0.5f, 0f));
                if (raycastManager.Raycast(screenCenter, raycastHits, TrackableType.PlaneWithinPolygon))
                {
                    await PlaceWithARAnchorAsync(raycastHits[0].pose);
                }
                else
                {
                    UpdateInstruction("����� ã�� �� �����ϴ�. �ٴ��̳� ���̺��� �ٽ� �����ּ���!");
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Placement failed: {ex.Message}");
            UpdateInstruction("��ġ �� ������ �߻��߽��ϴ�. �ٽ� �õ����ּ���.");
        }
        finally
        {
            isCreatingAnchor = false;
        }
    }

    private async Task PlaceWithARAnchorAsync(Pose placementPose)
    {
        var result = await anchorManager.TryAddAnchorAsync(placementPose);

        if (!result.status.IsSuccess())
        {
            UpdateInstruction("��ġ ����! �ٽ� �õ����ּ���.");
            return;
        }

        plantAnchor = result.value;

        if (preplacedTable != null)
        {
            currentTableInstance = Instantiate(preplacedTable);
            currentTableInstance.transform.SetParent(plantAnchor.transform, false);
            currentTableInstance.transform.localPosition = Vector3.zero;
            currentTableInstance.transform.localRotation = Quaternion.identity;
            currentTableInstance.transform.localScale = Vector3.one * plantScale;
        }

        if (preplacedSeed != null)
        {
            currentPlantInstance = Instantiate(preplacedSeed);
            currentPlantInstance.transform.SetParent(plantAnchor.transform, false);
            currentPlantInstance.transform.localPosition = Vector3.up * 0.1f;
            currentPlantInstance.transform.localRotation = Quaternion.identity;
            currentPlantInstance.transform.localScale = Vector3.one * plantScale;
        }

        OnPlacementCompleteInternal();
    }

    private void OnPlacementCompleteInternal()
    {
        IsPlaced = true;
        isPlacementMode = false;

        if (planeManager != null)
        {
            planeManager.enabled = false;
            foreach (var plane in planeManager.trackables)
                plane.gameObject.SetActive(false);
        }

        if (placementUI != null)
            placementUI.SetActive(false);

        if (voiceUI != null)
            voiceUI.SetActive(true);

        UpdateInstruction("��ġ �Ϸ�! ȭ�鿡 ������ ������ ���� ���غ�����!");

        OnPlacementComplete?.Invoke();
    }

    public void ReplacePlant(GameObject newPlantPrefab)
    {
        if (currentPlantInstance == null || plantAnchor == null || newPlantPrefab == null) return;

        Vector3 localPosition = currentPlantInstance.transform.transform.localPosition;
        Vector3 localScale = currentPlantInstance.transform.localScale;

        Destroy(currentPlantInstance);

        currentPlantInstance = Instantiate(newPlantPrefab);
        currentPlantInstance.transform.SetParent(plantAnchor.transform, false);
        currentPlantInstance.transform.localPosition = localPosition;
        currentPlantInstance.transform.localRotation = Quaternion.identity;
        currentPlantInstance.transform.localScale = localScale;

        Debug.Log($"Plant replaced with {newPlantPrefab.name}");
    }

    public void ResetPlacement()
    {
        if (currentTableInstance != null)
        {
            Destroy(currentTableInstance);
            currentTableInstance = null;
        }

        if (currentPlantInstance != null)
        {
            Destroy(currentPlantInstance);
            currentPlantInstance = null;
        }

        if (plantAnchor != null)
        {
            try
            {
                if (anchorManager != null)
                {
                    bool removeSuccess = anchorManager.TryRemoveAnchor(plantAnchor);
                    if (!removeSuccess)
                        Debug.LogWarning($"Failed to remove anchor: {removeSuccess}");
                }

            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error removing anchor: {ex.Message}");
            }
            finally
            {
                plantAnchor = null;
            }
        }

        IsPlaced = false;
        isCreatingAnchor = false;

        if (planeManager != null)
            planeManager.enabled = true;

        StartPlacementMode();
    }

    private void UpdateInstruction(string message)
    {
        if (instructionText != null)
            instructionText.text = message;
    }

    public GameObject GetCurrentPlant() => currentPlantInstance;
    public Transform GetPlantAnchor() => plantAnchor?.transform;

    private void OnDestroy()
    {
        if (touchPressAction != null && touchPressAction.action != null)
        {
            touchPressAction.action.started -= OnTouchStarted;
            touchPressAction.action.canceled -= OnTouchEnded;
            touchPressAction.action.Disable();
        }

        if (touchPositionAction != null && touchPositionAction.action != null)
        {
            touchPositionAction.action.Disable();
        }

        if (plantAnchor != null)
        {
            try
            {
                if (anchorManager != null)
                    anchorManager.TryRemoveAnchor(plantAnchor);
                
                Destroy(plantAnchor);
            }
            catch (System.Exception)
            {
                Debug.LogError("Error removing anchor in OnDestroy");
            }
        }
    }
}
