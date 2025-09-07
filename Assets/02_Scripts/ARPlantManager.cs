using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using System.Threading.Tasks;

public class ARPlantManager : MonoBehaviour
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

    [Header("Sunflower Prefabs")]
    public GameObject sunflowerSprout;
    public GameObject sunflowerGrowing;
    public GameObject sunflowerBlooming;

    [Header("Rose Prefabs")]
    public GameObject roseSprout;
    public GameObject roseGrowing;
    public GameObject roseBlooming;

    [Header("Cactus Prefabs")]
    public GameObject cactusSprout;
    public GameObject cactusGrowing;
    public GameObject cactusBlooming;

    [Header("Lavender Prefabs")]
    public GameObject lavenderSprout;
    public GameObject lavenderGrowing;
    public GameObject lavenderBlooming;

    [Header("UI")]
    public TextMeshProUGUI instructionText;
    public GameObject voiceUI;
    public GameObject placementUI;

    [Header("Voice Recognition")]
    public ARPlantVoiceController voiceController;

    [Header("Image Effect")]
    public SimpleImageEffects imageEffects;

    [Header("Growth Settings")]
    [Range(50f, 200f)]
    public float maxGrowthPoints = 100f;
    public float pointsPerVoiceSuccess = 15f;
    public float sproutThreshold = 45f;
    public float growingThreshold = 90f;
    public float bloomingThreshold = 100f;

    [Header("AR Settings")]
    public float plantScale = 1.0f;

    [Header("Setup Settings")]
    public float initialSetupDelay = 1.0f;

    private ARAnchor plantAnchor;
    private List<ARRaycastHit> raycastHits = new List<ARRaycastHit>();
    private bool isPlacementMode = true;
    private bool isCreatingAnchor = false;

    // Input Action
    private Vector2 touchPosition;
    private bool isTouchPressed = false;

    [Header("Plant Positioning Strategy")]
    public float pivotCenterOffset = 0.15f;

    private Vector3 seedOriginalPosition;  // Seed의 원래 위치 저장
    private Quaternion seedOriginalRotation; // Seed의 원래 회전 저장

    
    // 오브젝트
    private GameObject currentTableInstance;
    private GameObject currentPlantInstance;

    // 상태
    public bool IsPlaced { get; private set; } = false;
    private PlantGrowthStage currentStage = PlantGrowthStage.Seed;
    private string selectedPlantType = "sunflower";
    private float growthPoints = 0f;
    

    private void Awake()
    {
        // Resources에서 프리팹 로드
        LoadPrefabsFromResources();
    }
    private void Start()
    {
        SetupAR();
        SetupInputActions();
        SetupPlantType();
        // UI 초기화
        InitializeUI();
        // 자동 시작
        StartPlacementMode();
    }

    private void SetupAR()
    {
        // 카메라 찾기
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
        if(touchPositionAction != null)
            touchPositionAction.action.Enable();
        
        if(touchPressAction != null)
        {
            touchPressAction.action.Enable();
            touchPressAction.action.started += OnTouchStarted;
            touchPressAction.action.canceled += OnTouchEnded;
        }
    }

    private void OnTouchStarted(InputAction.CallbackContext context)
    {
        isTouchPressed = true;
        if(isPlacementMode && !IsPlaced && !isCreatingAnchor)
        {
            if(touchPositionAction != null)
            {
                touchPosition = touchPositionAction.action.ReadValue<Vector2>();
                _ = AttemptPlacementAsync();
            }
        }
    }

    private void OnTouchEnded(InputAction.CallbackContext context)
    {
        isTouchPressed = false;
    }
    private void SetupPlantType()
    {
        // 선택된 식물 타입 로드(기본값: sunflower)
        string savedPlantType = PlayerPrefs.GetString("Matched_Plant", "");
        if (!string.IsNullOrEmpty(savedPlantType))
        {
            selectedPlantType = savedPlantType.ToLower();
        }
        else
        {
            selectedPlantType = "sunflower";
        }
        // 유효성 검사
        ValidatePlantType();
    }

    private void LoadPrefabsFromResources()
    {
        // 식물 프리팹들 로드
        LoadPlantPrefabs();
    }

    private void LoadPlantPrefabs()
    {
        // Sunflower
        if (sunflowerSprout == null)
            sunflowerSprout = LoadSinglePrefab("Sunflower_Sprout", "sunflower_sprout");
        if (sunflowerGrowing == null)
            sunflowerGrowing = LoadSinglePrefab("Sunflower_Growing", "sunflower_growing");
        if (sunflowerBlooming == null)
            sunflowerBlooming = LoadSinglePrefab("Sunflower_Blooming", "sunflower_blooming");

        // Rose
        if (roseSprout == null)
            roseSprout = LoadSinglePrefab("Rose_Sprout", "rose_sprout");
        if (roseGrowing == null)
            roseGrowing = LoadSinglePrefab("Rose_Growing", "rose_growing");
        if (roseBlooming == null)
            roseBlooming = LoadSinglePrefab("Rose_Blooming", "rose_blooming");

        // Cactus
        if (cactusSprout == null)
            cactusSprout = LoadSinglePrefab("Cactus_Sprout", "cactus_sprout");
        if (cactusGrowing == null)
            cactusGrowing = LoadSinglePrefab("Cactus_Growing", "cactus_growing");
        if (cactusBlooming == null)
            cactusBlooming = LoadSinglePrefab("Cactus_Blooming", "cactus_blooming");

        // Lavender
        if (lavenderSprout == null)
            lavenderSprout = LoadSinglePrefab("Lavender_Sprout", "lavender_sprout");
        if (lavenderGrowing == null)
            lavenderGrowing = LoadSinglePrefab("Lavender_Growing", "lavender_growing");
        if (lavenderBlooming == null)
            lavenderBlooming = LoadSinglePrefab("Lavender_Blooming", "lavender_blooming");
    }

    private GameObject LoadSinglePrefab(string name1, string name2)
    {
        // 여러 경로 시도
        string[] paths =
        {
            name1,name2,
            $"Prefabs/{name1}", $"Prefabs/{name2}", $"Plants/{name1}", $"Plants/{name2}"
        };

        foreach(string path in paths)
        {
            GameObject prefab = Resources.Load<GameObject>(path);
            if(prefab != null)
            {
                Debug.Log($"Loaded: {path}");
                return prefab;
            }
        }
        return null;
    }

    private void ValidatePlantType()
    {
        if (selectedPlantType != "sunflower" && selectedPlantType != "rose" &&
            selectedPlantType != "cactus" && selectedPlantType != "lavender")
        {
            selectedPlantType = "sunflower";
        }
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
        UpdateInstruction("카메라를 천천히 움직여 바닥이나 테이블을 비춰주세요.\n" +
            "평면이 감지되면 터치해서 테이블을 배치하세요!");
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
        // 화면 중앙에서 평면 감지
        Vector3 screenCenter = arCamera.ViewportToScreenPoint(new Vector3(0.5f, 0.5f, 0f));

        if (raycastManager.Raycast(screenCenter, raycastHits, TrackableType.PlaneWithinPolygon))
        {
            // 평면이 감지되면 배치 가능 메시지
            UpdateInstruction("평면이 감지되었습니다!\n터치해서 " + GetCurrentPlantName() + "을(를) 배치하세요!");
        }
        else
        {
            UpdateInstruction("평면을 찾고 있습니다...\n카메라를 천천히 움직여 바닥이나 테이블을 비춰주세요!");
        }
    }

    private async Task AttemptPlacementAsync()
    {
        if (isCreatingAnchor) return;

        isCreatingAnchor = true;
        UpdateInstruction("식물을 배치하는 중...");

        try
        {
            if(raycastManager.Raycast(touchPosition, raycastHits, TrackableType.PlaneWithinPolygon))
            {
                await PlaceWithARAnchorAsync(raycastHits[0].pose);
            }
            else
            {
                Vector3 screenCenter = arCamera.ViewportToScreenPoint(new Vector3(0.5f, 0.5f, 0f));
                if(raycastManager.Raycast(screenCenter, raycastHits, TrackableType.PlaneWithinPolygon))
                {
                    await PlaceWithARAnchorAsync(raycastHits[0].pose);
                }
                else
                {
                    UpdateInstruction("평면을 찾을 수 없습니다. 바닥이나 테이블을 다시 비춰주세요!");
                }
            }
        }
        catch(System.Exception ex)
        {
            Debug.LogError($"Placement failed: {ex.Message}");
            UpdateInstruction("배치 중 오류가 발생했습니다. 다시 시도해주세요.");
        }
        finally
        {
            isCreatingAnchor = false;
        }
    }

    private async Task PlaceWithARAnchorAsync(Pose placementPose)
    {
        var result = await anchorManager.TryAddAnchorAsync(placementPose);

        if(!result.status.IsSuccess())
        {
            UpdateInstruction("배치 실패! 다시 시도해주세요.");
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

        if(preplacedSeed != null)
        {
            currentPlantInstance = Instantiate(preplacedSeed);
            currentPlantInstance.transform.SetParent(plantAnchor.transform, false);
            currentPlantInstance.transform.localPosition = Vector3.up * 0.1f;
            currentPlantInstance.transform.localRotation = Quaternion.identity;
            currentPlantInstance.transform.localScale = Vector3.one * plantScale;
        }

        OnPlacementComplete();
    }
    private IEnumerator InitialSetup()
    {
        yield return new WaitForSeconds(initialSetupDelay);
        SetupPreplacedObjects();
        OnPlacementComplete();
    }

    private void SetupPreplacedObjects()
    {
        if (preplacedTable != null)
        {
            currentTableInstance = preplacedTable;
        }

        if (plantSpawnPoint == null)
        {
            return;
        }

        // 미리 배치된 씨앗 설정 (시작용, 나중에 교체됨)
        if (preplacedSeed != null)
        {
            // Seed의 현재 위치를 원래 위치로 저장 (씬에서 배치한 위치)
            seedOriginalPosition = preplacedSeed.transform.position;
            seedOriginalRotation = preplacedSeed.transform.rotation;

            // PlantSpawnPoint를 Seed 위치로 업데이트
            plantSpawnPoint.position = seedOriginalPosition;
            plantSpawnPoint.rotation = seedOriginalRotation;

            currentPlantInstance = preplacedSeed;
        }
    }


    private void OnPlacementComplete()
    {
        IsPlaced = true;
        isPlacementMode = false;

        if(planeManager != null)
        {
            planeManager.enabled = false;
            foreach(var plane in planeManager.trackables)
                plane.gameObject.SetActive(false);
        }

        if (placementUI != null)
            placementUI.SetActive(false);

        if (voiceUI != null)
            voiceUI.SetActive(true);

        // 음성 인식 시작
        if (voiceController != null)
        {
            voiceController.enabled = true;
            voiceController.OnRecognitionSuccess += OnVoiceSuccess;
        }

        UpdateInstruction("배치 완료! 화면에 나오는 문장을 따라 말해보세요!");

    }

    private void OnVoiceSuccess(string keyword, float points, string method)
    {
        // 성장 포인트 추가
        growthPoints += points;
        growthPoints = Mathf.Clamp(growthPoints, 0f, maxGrowthPoints);

        if (imageEffects != null)
            imageEffects.PlayVoiceSuccessEffect();

        // UI 업데이트
        UpdateInstruction($"잘했어요! 성장 포인트: {growthPoints:F0}/{maxGrowthPoints} (+{pointsPerVoiceSuccess})");

        // 성장 체크
        CheckGrowth();
    }

    private void CheckGrowth()
    {
        PlantGrowthStage newStage = PlantGrowthStage.Seed;

        // 새로운 성장 단계 계산
        if (growthPoints >= bloomingThreshold)
            newStage = PlantGrowthStage.Blooming;
        else if (growthPoints >= growingThreshold)
            newStage = PlantGrowthStage.Growing;
        else if (growthPoints >= sproutThreshold)
            newStage = PlantGrowthStage.Sprout;

        if (newStage != currentStage)
        {
            currentStage = newStage;
            UpgradePlaneWithAnchor();
        }
    }

    private void UpgradePlaneWithAnchor()
    {
        if (currentPlantInstance == null || plantAnchor == null) return;

        GameObject newPlantPrefab = GetPlantPrefab();
        if (newPlantPrefab == null) return;

        Vector3 localPosition = currentPlantInstance.transform.localPosition;
        Vector3 localScale = currentPlantInstance.transform.localScale;

        Destroy(currentPlantInstance);

        currentPlantInstance = Instantiate(newPlantPrefab);
        currentPlantInstance.transform.SetParent(plantAnchor.transform, false);
        currentPlantInstance.transform.localPosition = localPosition;
        currentPlantInstance.transform.localRotation = Quaternion.identity;
        currentPlantInstance.transform.localScale = localScale;

        if(imageEffects != null)
        {
            switch (currentStage)
            {
                case PlantGrowthStage.Sprout:
                    imageEffects.PlaySproutEffect();
                    break;
                case PlantGrowthStage.Growing:
                    imageEffects.PlayGrowingEffect();
                    break;
                case PlantGrowthStage.Blooming:
                    imageEffects.PlayBloomingEffect();
                    break;
            }
        }

        string celebrationMessage = GetCelebrationMessage();
        UpdateInstruction(celebrationMessage);
        if (currentStage == PlantGrowthStage.Blooming)
            OnPlantFullyGrown();
        Debug.Log("Plant upgraded to {currentStage} - position remains and anchored!");
    }

    private GameObject GetPlantPrefab()
    {
        if(currentStage == PlantGrowthStage.Seed) return null;
        
        // 선택된 식물 타입에 따라 프리팹 반환
        switch (selectedPlantType)
        {
            case "sunflower":
                if(currentStage == PlantGrowthStage.Sprout) return sunflowerSprout;
                if(currentStage == PlantGrowthStage.Growing) return sunflowerGrowing;
                if(currentStage == PlantGrowthStage.Blooming) return sunflowerBlooming;
                break;

            case "rose":
                if(currentStage == PlantGrowthStage.Sprout) return roseSprout;
                if(currentStage == PlantGrowthStage.Growing) return roseGrowing;
                if(currentStage == PlantGrowthStage.Blooming) return roseBlooming;
                break;

            case "cactus":
                if(currentStage == PlantGrowthStage.Sprout) return cactusSprout;
                if(currentStage == PlantGrowthStage.Growing) return cactusGrowing;
                if(currentStage == PlantGrowthStage.Blooming) return cactusBlooming;
                break;

             case "lavender":
                if(currentStage == PlantGrowthStage.Sprout) return lavenderSprout;
                if(currentStage == PlantGrowthStage.Growing) return lavenderGrowing;
                if(currentStage == PlantGrowthStage.Blooming) return lavenderBlooming;
                break;
        }
        return null;
    }

    private string GetStageName()
    {
        switch (currentStage)
        {
            case PlantGrowthStage.Sprout: return "새싹";
            case PlantGrowthStage.Growing: return "성장";
            case PlantGrowthStage.Blooming: return "개화";
            default: return "씨앗";
        }
    }

    private string GetCelebrationMessage()
    {
        switch (currentStage)
        {
            case PlantGrowthStage.Sprout:
                return $"작은 새싹이 돋아났어요!";
            case PlantGrowthStage.Growing:
                return $"쑥쑥 자라고 있어요!";
            case PlantGrowthStage.Blooming:
                return $"완전히 피어났어요!";
            default:
                return "성장하고 있어요!";
        }
    }

    public string GetCurrentPlantName()
    {
        switch (selectedPlantType)
        {
            case "sunflower": return "해바라기";
            case "rose": return "장미";
            case "cactus": return "선인장";
            case "lavender": return "라벤더";
            default: return "식물";
        }
    }

    private void OnPlantFullyGrown()
    {
        UpdateInstruction($"{GetCurrentPlantName()}(이)가 완전히 피어났어요!\n 당신의 긍정적인 말이 기적을 만들었습니다!");
        if (voiceController != null)
            voiceController.OnAllComplete();

        StartCoroutine(CelebrationEffect());
    }

    private IEnumerator CelebrationEffect()
    {
        yield return new WaitForSeconds(1.5f);
    }

    // UI 업데이트 메서드들
    private void UpdateInstruction(string message)
    {
        if (instructionText != null)
            instructionText.text = message;
    }

    public void ResetPlacement()
    {
        if (currentTableInstance != null)
        {
            Destroy(currentTableInstance);
            currentTableInstance = null;
        }

        if(currentPlantInstance != null)
        {
            Destroy(currentPlantInstance);
            currentPlantInstance = null;
        }

        if(plantAnchor != null)
        {
            try
            {
                if(anchorManager != null)
                {
                    bool removeSuccess = anchorManager.TryRemoveAnchor(plantAnchor);
                    if (!removeSuccess)
                        Debug.LogWarning($"Failed to remove anchor: {removeSuccess}");
                }
               
            }
            catch(System.Exception ex)
            {
                Debug.LogError($"Error removing anchor: {ex.Message}");
            }
            finally
            {
                plantAnchor = null;
            }
        }

        IsPlaced = false;
        growthPoints = 0f;
        currentStage = PlantGrowthStage.Seed;
        isCreatingAnchor = false;

        if (planeManager != null)
            planeManager.enabled = true;

        StartPlacementMode();
    }

    private void OnDestroy()
    {
        if (touchPressAction != null && touchPressAction.action != null)
        {
            touchPressAction.action.started -= OnTouchStarted;
            touchPressAction.action.canceled -= OnTouchEnded;
            touchPressAction.action.Disable();
        }

        if(touchPositionAction != null && touchPositionAction.action!= null)
        {
            touchPositionAction.action.Disable();
        }
        if (voiceController != null)
        {
            voiceController.OnRecognitionSuccess -= OnVoiceSuccess;
        }

        if(plantAnchor != null)
        {
            try
            {
                if(anchorManager != null)
                {
                    anchorManager.TryRemoveAnchor(plantAnchor);
                }

                Destroy(plantAnchor);
            }
            catch(System.Exception)
            {
                Debug.LogError("Error removing anchor in OnDestroy");
            }
        }
    }
}
