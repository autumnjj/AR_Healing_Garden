using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class ARPlantManager : MonoBehaviour
{
    [Header("AR Foundation Components")]
    public Camera arCamera;
    
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

    [Header("Voice Recognition")]
    public ARPlantVoiceController voiceController;

    [Header("Visual Effects")]
    public ParticleSystem growthParticles;
    public ParticleSystem successParticles;

    [Header("Growth Settings")]
    [Range(50f, 200f)]
    public float maxGrowthPoints = 100f;
    public float pointsPerVoiceSuccess = 15f;

    [Header("Growth Thresholds")]
    public float sproutThreshold = 45f;
    public float growingThreshold = 90f;
    public float bloomingThreshold = 100f;

    [Header("Setup Settings")]
    public float initialSetupDelay = 1.0f;
    
    // 오브젝트
    private GameObject tableInstance;
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
        SetupCamera();
        SetupPlantType();
        // UI 초기화
        InitializeUI();
        // 자동 시작
        StartCoroutine(InitialSetup());
    }

    private void SetupCamera()
    {
        // 카메라 찾기
        if (arCamera == null)
        {
            arCamera = Camera.main;
            if (arCamera == null)
                arCamera = FindAnyObjectByType<Camera>();
        }
    }

    private void SetupPlantType()
    {
        // 선택된 식물 타입 로드(기본값: sunflower)
        string savedPlantType = PlayerPrefs.GetString("Matched_Plant", "");
        Debug.Log($"Saved Plant Type from PlayerPrefs: '{savedPlantType}'");
        if (!string.IsNullOrEmpty(savedPlantType))
        {
            selectedPlantType = savedPlantType.ToLower();
            Debug.Log($"Plant type set to: {selectedPlantType}");
        }
        else
        {
            selectedPlantType = "sunflower";
        }
        // 유효성 검사
        ValidatePlantType();
        Debug.Log($"Selected Plant: {GetCurrentPlantName()}");
    }

    private void LoadPrefabsFromResources()
    {
        Debug.Log("Loading prefabs from Resources...");

        // 식물 프리팹들 로드
        LoadPlantPrefabs();

        Debug.Log("Plant prefabs loading complete");
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
        Debug.LogWarning($"Could not find prefab: {name1}_{name2}");
        return null;
    }

    private void ValidatePlantType()
    {
        if (selectedPlantType != "sunflower" && selectedPlantType != "rose" &&
            selectedPlantType != "cactus" && selectedPlantType != "lavender")
        {
            Debug.LogWarning($"Invalid plant type : {selectedPlantType}, using default: sunflower");
            selectedPlantType = "sunflower";
        }
    }

    private void InitializeUI()
    {
        if (voiceUI != null)
            voiceUI.SetActive(false);

        UpdateInstruction($"{GetCurrentPlantName()}(이)가 준비되었습니다1");
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
            tableInstance = preplacedTable;
            Debug.Log("Pre-placed table found and configured (FIXED)");
        }
        else
        {
            Debug.LogError("Pre-placed table not assigned! Please assign it in the inspector.");
        }

        if (plantSpawnPoint == null)
        {
            return;
        }

        // 미리 배치된 씨앗 설정 (시작용, 나중에 교체됨)
        if (preplacedSeed != null)
        {
            // 씨앗을 Spawn Point 위치로 이동
            preplacedSeed.transform.position = plantSpawnPoint.position;
            preplacedSeed.transform.rotation = plantSpawnPoint.rotation;

            currentPlantInstance = preplacedSeed;
            Debug.Log("Pre-placed seed positioned at spawn point");
        }
        else
        {
            Debug.LogError("Pre-placed seed not assigned! Please assign it in the inspector.");
        }

        Debug.Log($"Plant Spawn Point position: {plantSpawnPoint.position}");
    }

    

    private void OnPlacementComplete()
    {
        IsPlaced = true;

        if (voiceUI != null)
            voiceUI.SetActive(true);

        // 음성 인식 시작
        if (voiceController != null)
        {
            voiceController.enabled = true;
            voiceController.OnVoiceRecognitionSuccess += OnVoiceSuccess;
        }

        UpdateInstruction("화면에 나오는 문장을 따라 말해보세요!");

        // 시작 효과
        if (successParticles != null && currentPlantInstance != null)
        {
            successParticles.transform.position = currentPlantInstance.transform.position;
            successParticles.Play();
        }
    }

    private void OnVoiceSuccess(string recognizedText)
    {
        // 성장 포인트 추가
        growthPoints += pointsPerVoiceSuccess;
        growthPoints = Mathf.Clamp(growthPoints, 0f, maxGrowthPoints);

        // 파티클 효과
        if(successParticles != null && currentPlantInstance != null)
        {
            successParticles.transform.position = currentPlantInstance.transform.position;
            successParticles.Play();
        }

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
            ChangePlant();
        }
    }

    private void ChangePlant()
    {
        if (currentPlantInstance == null || plantSpawnPoint == null)
        {
            Debug.LogError("ChangePlant failed: currentPlantInstance or plantSpawnPoint is null");
            return;
        }

        // 기존 식물의 정보 저장 (Spawn Point 기준으로)
        Vector3 spawnPosition = plantSpawnPoint.position;
        Quaternion spawnRotation = plantSpawnPoint.rotation;
        Vector3 currentScale = currentPlantInstance.transform.localScale;

        Debug.Log($"Attempting to change plant to stage: {currentStage} for plant type: {selectedPlantType}");

        // 새 식물 프리팹 가져오기
        GameObject newPlantPrefab = GetPlantPrefab();

        if (newPlantPrefab == null)
        {
            return;
        }

        Debug.Log($"Found prefab: {newPlantPrefab.name} for stage: {currentStage}");

        // 기존 식물 제거
        Destroy(currentPlantInstance);

        // 새 식물 생성 (항상 Spawn Point 위치에)
        currentPlantInstance = Instantiate(newPlantPrefab, spawnPosition, spawnRotation);

        // 크기는 기존 것 유지 (또는 기본 크기 사용)
        currentPlantInstance.transform.localScale = currentScale;

        // 성장 파티클 (Spawn Point 위치에)
        if (growthParticles != null)
        {
            growthParticles.transform.position = spawnPosition;
            growthParticles.Play();
        }

        string stageName = GetStageName();
        string celebrationMessage = GetCelebrationMessage();
        UpdateInstruction(celebrationMessage);

        Debug.Log($"Plant successfully changed to {newPlantPrefab.name} at spawn point: {spawnPosition}");

        if (currentStage == PlantGrowthStage.Blooming)
        {
            OnPlantFullyGrown();
        }
    }

    private void OnPlantFullyGrown()
    {
        UpdateInstruction($"{GetCurrentPlantName()}(이)가 완전히 피어났어요!\n 당신의 긍정적인 말이 기적을 만들었습니다!");
        if (voiceController != null)
            voiceController.OnAllTargetsComplete();

        StartCoroutine(CelebrationEffect());
    }

    private IEnumerator CelebrationEffect()
    {
        for (int i = 0; i < 3; i++)
        {
            if (growthParticles != null)
            {
                growthParticles.transform.position = currentPlantInstance.transform.position;
                growthParticles.Emit(30);
            }
            yield return new WaitForSeconds(0.5f);
        }
    }


    private GameObject GetPlantPrefab()
    {
        if(currentStage == PlantGrowthStage.Seed)
            return null;
        
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
                return $"작은 새싹이 돋아났어요!\n{GetCurrentPlantName()}(이)가 당신의 따뜻한 말에 반응하고 있습니다.";
            case PlantGrowthStage.Growing:
                return $"쑥쑥 자라고 있어요!\n긍정적인 에너지가 {GetCurrentPlantName()}(을)를 건강하게 키우고 있습니다.";
            case PlantGrowthStage.Blooming:
                return $"완전히 피어났어요!\n당신의 사랑스러운 말들이 아름다운 {GetCurrentPlantName()}(을)를 완성했습니다!";
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

    // UI 업데이트 메서드들
    private void UpdateInstruction(string message)
    {
        if (instructionText != null)
            instructionText.text = message;
    }

    private void OnDestroy()
    {
        if (voiceController != null)
        {
            voiceController.OnVoiceRecognitionSuccess -= OnVoiceSuccess;
        }
    }
}
