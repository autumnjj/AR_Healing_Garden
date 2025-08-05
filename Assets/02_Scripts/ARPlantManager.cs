using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using TMPro;

public class ARPlantManager : MonoBehaviour
{
    [Header("AR Foundation Components")]
    public ARRaycastManager raycastManager;
    public ARPlaneManager planeManager;
    public Camera arCamera;
    
    [Header("Plant Prefabs")]
    public GameObject tablePrefab;
    public GameObject seedPrefab;

    [Header("Plant Prefabs by Type")]
    public PlantPrefabSet sunflowerPrefabs;
    public PlantPrefabSet rosePrefabs;
    public PlantPrefabSet cactusPrefabs;
    public PlantPrefabSet lavenderPrefabs;

    [Header("UI")]
    public GameObject placementIndicator;
    public TextMeshProUGUI instructionText;
    public TextMeshProUGUI statusText;

    [Header("Input Actions")]
    public InputActionAsset inputActions;

    // Input Actions
    private InputAction touchAction;
    private InputAction touchPositionAction;
    private InputAction pinchAction;

    // AR 관련 변수
    private List<ARRaycastHit> raycastHits = new List<ARRaycastHit>();
    private GameObject tableInstance;
    private GameObject currentPlantInstance;
    private bool isTablePlaced = false;
    private bool isPlantPlaced = false;

    // 식물 관련
    private PlantGrowthStage currentGrowthStage = PlantGrowthStage.Seed;
    private string selectedPlantType = "sunflower";
    private float growthPoints = 0f;
    private float maxGrowthPoints = 100f;

    // 상태
    private bool isPlacementMode = true;
    private Vector3 lastTouchPosition;
    private float lastTouchTime;

    [System.Serializable]
    public class PlantPrefabSet
    {
        public GameObject sprout;
        public GameObject growing;
        public GameObject blooming;
    }

    private void Start()
    {
        SetupInputActions();
        InitializeAR();
        LoadSelectedPlant();
        UpdateInstructionText("화면을 터치해서 화분을 놓아보세요!");
    }

    private void SetupInputActions()
    {
        if (inputActions == null) return;

        var actionMap = inputActions.FindActionMap("AR Plant");
        touchAction = actionMap.FindAction("Touch");
        touchPositionAction = actionMap.FindAction("TouchPosition");
        pinchAction = actionMap.FindAction("Pinch");
    }

    private void OnEnable()
    {
        if (touchAction != null)
        {
            touchAction.started += OnTouchStarted;
            touchAction.canceled += OnTouchEnded;
        }

        touchAction?.Enable();
        touchPositionAction?.Enable();
        pinchAction?.Enable();
    }

    private void OnDisable()
    {
        if (touchAction != null)
        {
            touchAction.started -= OnTouchStarted;
            touchAction.canceled -= OnTouchEnded;
        }

        touchAction?.Disable();
        touchPositionAction?.Disable();
        pinchAction?.Disable();
    }

    private void InitializeAR()
    { 
        if(placementIndicator != null)
            placementIndicator.SetActive(false);
    }

    private void LoadSelectedPlant()
    {
        // MBTI 결과에서 식물 타입 가져오기
        selectedPlantType = PlayerPrefs.GetString("Matched_Plant", "sunflower").ToLower();
        Debug.Log($"Selected plant type : {selectedPlantType}");
    }

    private void Update()
    {
        if (isPlacementMode)
            UpdatePlacementIndicator();

        HandlePinchForScale();
    }

    private void UpdatePlacementIndicator()
    {
        var screenCenter = arCamera.ViewportToScreenPoint(new Vector3(0.5f, 0.5f));
        raycastHits.Clear();

        if (raycastManager.Raycast(screenCenter, raycastHits, TrackableType.PlaneWithinPolygon))
        {
            var hit = raycastHits[0];

            if (placementIndicator != null)
            {
                placementIndicator.SetActive(true);
                placementIndicator.transform.SetPositionAndRotation(hit.pose.position, hit.pose.rotation);
            }
        }
        else
        {
            if (placementIndicator != null)
                placementIndicator.SetActive(false);
        }
    }

    private void OnTouchStarted(InputAction.CallbackContext context)
    {
        lastTouchPosition = touchPositionAction.ReadValue<Vector2>();
        lastTouchTime = Time.time;
    }

    private void OnTouchEnded(InputAction.CallbackContext context)
    {
        Vector2 currentTouchPosition = touchPositionAction.ReadValue<Vector2>();
        float touchDuration = Time.time - lastTouchTime;
        float touchDistance = Vector2.Distance(lastTouchPosition, currentTouchPosition);

        // 탭 감지
        if (touchDuration < 0.3f && touchDistance < 50f)
            ProcessTap(currentTouchPosition);
    }

    private void ProcessTap(Vector2 screenPosition)
    {
        if (isPlacementMode)
        {
            PlaceTableAndPlant(screenPosition);
        }
        else
        {
            // 식물과 상호작용
            InteractWithPlant(screenPosition);
        }
    }

    private void PlaceTableAndPlant(Vector2 screenPosition)
    {
        raycastHits.Clear();

        if (raycastManager.Raycast(screenPosition, raycastHits, TrackableType.PlaneWithinPolygon))
        {
            var hit = raycastHits[0];

            // 탁자 배치
            if (tablePrefab != null && !isTablePlaced)
            {
                tableInstance = Instantiate(tablePrefab, hit.pose.position, hit.pose.rotation);
                isTablePlaced = true;

                // 탁자 위에 씨앗 배치
                Vector3 plantPosition = hit.pose.position + Vector3.up * 0.1f;
                currentPlantInstance = Instantiate(seedPrefab, plantPosition, hit.pose.rotation);
                isPlantPlaced = true;

                // 배치 모드 종료
                isPlacementMode = false;
                if (placementIndicator != null)
                    placementIndicator.SetActive(false);

                // 평면 감지 비활성화(성능 향상)
                if (planeManager != null)
                {
                    planeManager.enabled = false;
                    foreach(var plane in planeManager.trackables)
                    {
                        plane.gameObject.SetActive(false);
                    }
                }

                UpdateInstructionText("식물을 터치하거나 긍정적인 말을 해보세요!");
                UpdateStatusText($"성장 단계: 씨앗 ({growthPoints:F0}/{maxGrowthPoints}");
            }
        }
    }

    private void InteractWithPlant(Vector2 screenPosition)
    {
        if (!isPlantPlaced) return;

        Ray ray = arCamera.ScreenPointToRay(screenPosition);
        RaycastHit hit;

        // 식물 터치 감지
        if (Physics.Raycast(ray, out hit))
        {
            if (hit.collider.gameObject == currentPlantInstance ||
                hit.collider.transform.IsChildOf(currentPlantInstance.transform))
            {
                // 터치 상호작용 수행
                AddGrowthPoints(10f, "터치");
                PlayTouchEffect(hit.point);
            }
        }
    }

    private void HandlePinchForScale()
    {
        if(Input.touchCount == 2)
        {
            Touch touch1 = Input.GetTouch(0);
            Touch touch2 = Input.GetTouch(1);

            float currentDistance = Vector2.Distance(touch1.position, touch2.position);

            if (touch1.phase == TouchPhase.Moved || touch2.phase == TouchPhase.Moved)
            {
                float prevDistance = Vector2.Distance(
                    touch1.position - touch1.deltaPosition,
                    touch2.position - touch2.deltaPosition);

                float deltaDistance = currentDistance - prevDistance;

                if (currentPlantInstance != null && Mathf.Abs(deltaDistance) > 10f)
                {
                    float scaleChange = deltaDistance * 0.001f;
                    Vector3 newScale = currentPlantInstance.transform.localScale + Vector3.one * scaleChange;

                    // 스케일 제한
                    newScale = Vector3.Clamp(newScale, Vector3.one * 0.5f, Vector3.one * 2f);
                    currentPlantInstance.transform.localScale = newScale;
                }
            }

        }
        
    }

    public void AddGrowthPoints(float points, string source)
    {
        growthPoints += points;
        growthPoints = Mathf.Clamp(growthPoints, 0f, maxGrowthPoints);

        UpdateStatusText($"성장 단계: {GetStageKoreanName(currentGrowthStage)} (+{points} from {source}");

        // 성장 단계 체크
        CheckGrowthStage();
    }

    private void CheckGrowthStage()
    {
        PlantGrowthStage newStage = CalculateGrowthStage();

        if (newStage != currentGrowthStage)
        {
            currentGrowthStage = newStage;
            UpdatePlantVisual();
            UpdateInstructionText($"축하합니다! {GetStageKoreanName(currentGrowthStage)} 단계로 성장했어요!");
        }
    }

    private PlantGrowthStage CalculateGrowthStage()
    {
        if (growthPoints >= 75f) return PlantGrowthStage.Blooming;
        if (growthPoints >= 50f) return PlantGrowthStage.Growing;
        if (growthPoints >= 25f) return PlantGrowthStage.Sprout;
        return PlantGrowthStage.Seed;
    }

    private void UpdatePlantVisual()
    {
        if (currentPlantInstance == null) return;

        Vector3 position = currentPlantInstance.transform.position;
        Quaternion rotation = currentPlantInstance.transform.rotation;
        Vector3 scale = currentPlantInstance.transform.localScale;

        // 기존 식물 제거
        Destroy(currentPlantInstance );

        // 새로운 단계의 식물 생성
        GameObject newPlantPrefab = GetPlantPrefabForStage(currentGrowthStage);
        if (newPlantPrefab != null)
        {
            currentPlantInstance = Instantiate(newPlantPrefab, position, rotation);
            currentPlantInstance.transform.localScale = scale;
        }
    }

    private GameObject GetPlantPrefabForStage(PlantGrowthStage stage)
    {
        PlantPrefabSet prefabSet = GetPrefabSetForPlantType(selectedPlantType);
        if (prefabSet == null) return seedPrefab;

        switch (stage)
        {
            case PlantGrowthStage.Sprout: return prefabSet.sprout;
            case PlantGrowthStage.Growing: return prefabSet.growing;
            case PlantGrowthStage.Blooming: return prefabSet.blooming;
            default: return seedPrefab;
        }
    }

    private PlantPrefabSet GetPrefabSetForPlantType(string plantType)
    {
        switch (plantType.ToLower())
        {
            case "sunflower": return sunflowerPrefabs;
            case "rose": return rosePrefabs;
            case "cactus": return cactusPrefabs;
            case "lavender": return lavenderPrefabs;
            default: return sunflowerPrefabs;
        }
    }

    private void PlayTouchEffect(Vector3 position)
    {
        // 간단한 파티클 효과나 애니메이션
        Debug.Log($"Touch effect at {position}");
    }

    private string GetStageKoreanName(PlantGrowthStage stage)
    {
        switch (stage)
        {
            case PlantGrowthStage.Seed: return "씨앗";
            case PlantGrowthStage.Sprout: return "새싹";
            case PlantGrowthStage.Growing: return "성장";
            case PlantGrowthStage.Blooming: return "개화";
            default: return "알 수 없음";
        }
    }

    // UI 업데이트 메서드들
    private void UpdateInstructionText(string message)
    {
        if (instructionText != null)
            instructionText.text = message;
    }

    private void UpdateStatusText(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }

    // 음성 인식에서 호출할 메서드
    public void OnPositiveSpeechDetected(float points)
    {
        AddGrowthPoints(points, "음성");
    }

    // 리셋 기능
    public void ResetPlant()
    {
        if (tableInstance != null) Destroy(tableInstance);
        if (currentPlantInstance != null) Destroy(currentPlantInstance);

        isTablePlaced = false;
        isPlantPlaced = false;
        isPlacementMode = true;
        currentGrowthStage = PlantGrowthStage.Seed;
        growthPoints = 0f;

        if (planeManager != null)
        {
            planeManager.enabled = true;
            foreach(var plane in planeManager.trackables)
            {
                plane.gameObject.SetActive(true);
            }
        }

        UpdateInstructionText("탁자를 터치해서 화분을 놓아보세요!");
    }
}
