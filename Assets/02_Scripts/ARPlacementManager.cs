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
    [Header("Plant Prefabs")]
    public GameObject seedPrefab;

    [Header("AR Components")]
    public ARRaycastManager raycastManager;
    public ARAnchorManager anchorManager;
    public ARPlaneManager planeManager;
    public Camera arCamera;

    [Header("Input Actions")]
    public InputActionReference touchPositionAction;
    public InputActionReference touchPressAction;

    [Header("Settings")]
    public float plantScale = 0.5f;
    public float plantYOffset = 0.02f;
    public float fallbackDistance = 1.2f;
    public float detectionTimeout = 8f;

    [Header("UI")]
    public TextMeshProUGUI instructionText;
    public GameObject voiceUI;
    public Button plantSeedButton;

    // 상태
    private bool isPlanted = false;
    private bool isCreatingAnchor = false;
    private float detectionTimer = 0f;

    // AR 오브젝트
    private ARAnchor plantAnchor;
    private GameObject currentPlant;
    private Vector3 fixedLocalPosition;

    // 레이캐스트
    private List<ARRaycastHit> raycastHits = new List<ARRaycastHit>();

    public System.Action OnSeedPlanted;
    public bool IsPlanted => isPlanted;

    private void Start()
    {
        SetupComponents();
        SetupInputActions();
        SetupUI();
        StartPlantingFlow();

        // 고정 위치 설정
        fixedLocalPosition = new Vector3(0, plantYOffset, 0);
    }

    private void SetupComponents()
    {
        if (arCamera == null)
            arCamera = Camera.main ?? FindAnyObjectByType<Camera>();

        if (raycastManager == null)
            raycastManager = FindAnyObjectByType<ARRaycastManager>();

        if (anchorManager == null)
            anchorManager = FindAnyObjectByType<ARAnchorManager>();

        if (planeManager == null)
            planeManager = FindAnyObjectByType<ARPlaneManager>();
    }

    private void SetupInputActions()
    {
        if (touchPositionAction != null)
            touchPositionAction.action.Enable();

        if (touchPressAction != null)
        {
            touchPressAction.action.Enable();
            touchPressAction.action.started += OnTouchStarted;
        }
    }

    private void SetupUI()
    {
        if (voiceUI != null)
            voiceUI.SetActive(false);

        if (plantSeedButton != null)
        {
            plantSeedButton.onClick.AddListener(PlantSeedAtCenter);
            plantSeedButton.gameObject.SetActive(false);
        }
    }

    private void OnTouchStarted(InputAction.CallbackContext context)
    {
        if (isPlanted || isCreatingAnchor) return;

        Vector2 touchPos = touchPositionAction.action.ReadValue<Vector2>();
        _ = TryPlantAtTouch(touchPos);
    }
    private void StartPlantingFlow()
    {
        UpdateInstruction("평면을 찾고 있어요...\n바닥을 비춰주세요!");
        StartCoroutine(PlantingDetectionRoutine());
    }
    private IEnumerator PlantingDetectionRoutine()
    {
        while (!isPlanted && detectionTimer < detectionTimeout)
        {
            detectionTimer += Time.deltaTime;

            // 3초 후 씨앗 심기 버튼 표시
            if (detectionTimer > 3f && plantSeedButton != null && !plantSeedButton.gameObject.activeInHierarchy)
            {
                plantSeedButton.gameObject.SetActive(true);
                UpdateInstruction("평면을 터치하거나\n'화분 배치' 버튼을 눌러주세요!");
            }

            yield return new WaitForSeconds(0.5f);
        }

        // 타임아웃 시 자동으로 씨앗 심기
        if (!isPlanted)
        {
            Debug.Log("Auto-planting seed due to timeout");
            PlantSeedAtCenter();
        }
    }

    private async Task TryPlantAtTouch(Vector2 touchPosition)
    {
        if (isCreatingAnchor) return;

        isCreatingAnchor = true;
        UpdateInstruction("화분을 배치 중...");

        try
        {
            if (raycastManager.Raycast(touchPosition, raycastHits, TrackableType.PlaneWithinPolygon))
            {
                await PlantSeedAtPose(raycastHits[0].pose);
            }
            else
            {
                UpdateInstruction("평면을 찾을 수 없어요. 다시 터치해주세요!");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Seed planting failed: {ex.Message}");
            UpdateInstruction("배치 중 오류가 발생했어요. 다시 시도해주세요.");
        }
        finally
        {
            isCreatingAnchor = false;
        }
    }

    public void PlantSeedAtCenter()
    {
        if (isPlanted) return;

        Debug.Log("Planting seed at center/fallback position");

        Vector3 screenCenter = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0);

        // 화면 중앙에서 평면 감지 시도
        if (raycastManager.Raycast(screenCenter, raycastHits, TrackableType.PlaneWithinPolygon))
        {
            _ = PlantSeedAtPose(raycastHits[0].pose);
        }
        else
        {
            // 평면 없으면 카메라 앞 고정 위치에
            Vector3 forwardDirection = arCamera.transform.forward;
            forwardDirection.y = -0.5f;
            forwardDirection.Normalize();

            Vector3 placementPosition = arCamera.transform.position + forwardDirection * fallbackDistance;
            placementPosition.y = arCamera.transform.position.y - 1f;

            Pose fallbackPose = new Pose(placementPosition, Quaternion.identity);
            _ = PlantSeedAtPose(fallbackPose);
        }
    }

    private async Task PlantSeedAtPose(Pose pose)
    {
        if (isPlanted) return;

        try
        {
            var result = await anchorManager.TryAddAnchorAsync(pose);

            if (!result.status.IsSuccess())
            {
                PlantSeedWithoutAnchor(pose);
                return;
            }

            plantAnchor = result.value;
            CreateSeed(plantAnchor.transform);

        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"Anchor creation failed: {ex.Message}, planting without anchor");
            PlantSeedWithoutAnchor(pose);
        }
    }

    private void PlantSeedWithoutAnchor(Pose pose)
    {
        GameObject anchorObject = new GameObject("SeedAnchor");
        anchorObject.transform.position = pose.position;
        anchorObject.transform.rotation = pose.rotation;

        CreateSeed(anchorObject.transform);
    }

    private void CreateSeed(Transform parent)
    {
        if (seedPrefab == null)
        {
            Debug.LogError("Seed prefab is not assigned!");
            return;
        }

        // 씨앗 생성 (씬에 미리 배치 안했음!)
        currentPlant = Instantiate(seedPrefab, parent);
        currentPlant.transform.localPosition = fixedLocalPosition;
        currentPlant.transform.localRotation = Quaternion.identity;
        currentPlant.transform.localScale = Vector3.one * plantScale;

        OnSeedPlantingSuccess();

        Debug.Log($"Seed created at local position: {fixedLocalPosition}");
    }

    private void OnSeedPlantingSuccess()
    {
        isPlanted = true;

        // UI 정리
        if (plantSeedButton != null)
            plantSeedButton.gameObject.SetActive(false);

        if (voiceUI != null)
            voiceUI.SetActive(true);

        // 평면 감지 비활성화 (성능 향상)
        if (planeManager != null)
        {
            planeManager.enabled = false;
            foreach (var plane in planeManager.trackables)
                plane.gameObject.SetActive(false);
        }

        UpdateInstruction("화분이 배치되었어요!\n화면의 문장을 따라 말해보세요!");

        OnSeedPlanted?.Invoke();
    }

    public void ReplacePlant(GameObject newPlantPrefab)
    {
        if (!isPlanted || newPlantPrefab == null || currentPlant == null) return;

        Transform parent = currentPlant.transform.parent;

        Debug.Log($"Replacing plant with {newPlantPrefab.name}");
        Debug.Log($"Current plant position before replace: {currentPlant.transform.localPosition}");

        // 기존 식물 제거
        Destroy(currentPlant);

        // 새 식물 생성 - 고정된 로컬 위치 사용
        currentPlant = Instantiate(newPlantPrefab, parent);
        currentPlant.transform.localPosition = fixedLocalPosition;
        currentPlant.transform.localRotation = Quaternion.identity;
        currentPlant.transform.localScale = Vector3.one * plantScale;

        Debug.Log($"Plant replaced successfully at position: {currentPlant.transform.localPosition}");
    }

    private void UpdateInstruction(string message)
    {
        if (instructionText != null)
            instructionText.text = message;
    }

    // 기존 시스템과의 호환성
    public GameObject GetCurrentPlant() => currentPlant;
    public Transform GetPlantAnchor() => plantAnchor?.transform ?? currentPlant?.transform.parent;

    private void OnDestroy()
    {
        if (touchPressAction != null && touchPressAction.action != null)
        {
            touchPressAction.action.started -= OnTouchStarted;
            touchPressAction.action.Disable();
        }

        if (touchPositionAction != null && touchPositionAction.action != null)
            touchPositionAction.action.Disable();

        if (plantAnchor != null && anchorManager != null)
        {
            try
            {
                anchorManager.TryRemoveAnchor(plantAnchor);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error removing anchor: {ex.Message}");
            }
        }
    }
}