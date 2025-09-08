using UnityEngine;
using TMPro;
using System.Collections;

public class ARPlantGrowthController : MonoBehaviour
{
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

    [Header("Growth Settings")]
    [Range(50f, 200f)]
    public float maxGrowthPoints = 100f;
    public float pointsPerVoiceSuccess = 15f;
    public float sproutThreshold = 45f;
    public float growingThreshold = 90f;
    public float bloomingThreshold = 100f;

    [Header("UI")]
    public TextMeshProUGUI instructionText;

    [Header("Image Effect")]
    public SimpleImageEffects imageEffects;

    [Header("Voice Recognition")]
    public ARPlantVoiceController voiceController;

    private ARPlacementManager placementManager;

    private PlantGrowthStage currentStage = PlantGrowthStage.Seed;
    private string selectedPlantType = "sunflower";
    private float growthPoints = 0f;

    private void Awake()
    {
        LoadPrefabsFromResources();
    }
    private void Start()
    {
        SetupReferences();
        SetupPlantType();
        ConnectEvents();
    }

    private void SetupReferences()
    {
        placementManager = FindAnyObjectByType<ARPlacementManager>();
        if (placementManager != null)
            Debug.LogError("Can Not Find ARPlacementManager");
    }

    private void SetupPlantType()
    {
        string savedPlantType = PlayerPrefs.GetString("Matched_Plant", "");
        if (!string.IsNullOrEmpty(savedPlantType))
        {
            selectedPlantType = savedPlantType.ToLower();
        }
        else
        {
            selectedPlantType = "sunflower";
        }
        ValidatePlantType();
    }

    private void ValidatePlantType()
    {
        if (selectedPlantType != "sunflower" && selectedPlantType != "rose" &&
            selectedPlantType != "cactus" && selectedPlantType != "lavender")
        {
            selectedPlantType = "sunflower";
        }
    }

    private void ConnectEvents()
    {
        if (placementManager != null)
            placementManager.OnPlacementComplete += OnPlacementComplete;

        if (voiceController != null)
            voiceController.OnRecognitionSuccess += OnVoiceSuccess;
    }

    private void OnPlacementComplete()
    {
        if (voiceController != null)
            voiceController.enabled = true;

        Debug.Log("Plant placement completed - voice recognition enabled");
    }

    private void OnVoiceSuccess(string keyword, float points, string method)
    {
        growthPoints += pointsPerVoiceSuccess;
        growthPoints = Mathf.Clamp(growthPoints, 0f, maxGrowthPoints);

        if (imageEffects != null)
            imageEffects.PlayVoiceSuccessEffect();

        UpdateInstruction($"잘했어요! 성장 포인트: {growthPoints:F0}/{maxGrowthPoints} (+{pointsPerVoiceSuccess})");

        CheckGrowth();
    }

    private void CheckGrowth()
    {
        PlantGrowthStage newStage = PlantGrowthStage.Seed;

        if (growthPoints >= bloomingThreshold)
            newStage = PlantGrowthStage.Blooming;
        else if (growthPoints >= growingThreshold)
            newStage = PlantGrowthStage.Growing;
        else if (growthPoints >= sproutThreshold)
            newStage = PlantGrowthStage.Sprout;

        if (newStage != currentStage)
        {
            currentStage = newStage;
            UpgradePlant();
        }
    }

    private void UpgradePlant()
    {
        GameObject newPlantPrefab = GetPlantPrefab();
        if (newPlantPrefab == null || placementManager == null) return;

        placementManager.ReplacePlant(newPlantPrefab);

        if (imageEffects != null)
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
        if (currentStage == PlantGrowthStage.Seed) return null;

        switch (selectedPlantType)
        {
            case "sunflower":
                if (currentStage == PlantGrowthStage.Sprout) return sunflowerSprout;
                if (currentStage == PlantGrowthStage.Growing) return sunflowerGrowing;
                if (currentStage == PlantGrowthStage.Blooming) return sunflowerBlooming;
                break;

            case "rose":
                if (currentStage == PlantGrowthStage.Sprout) return roseSprout;
                if (currentStage == PlantGrowthStage.Growing) return roseGrowing;
                if (currentStage == PlantGrowthStage.Blooming) return roseBlooming;
                break;

            case "cactus":
                if (currentStage == PlantGrowthStage.Sprout) return cactusSprout;
                if (currentStage == PlantGrowthStage.Growing) return cactusGrowing;
                if (currentStage == PlantGrowthStage.Blooming) return cactusBlooming;
                break;

            case "lavender":
                if (currentStage == PlantGrowthStage.Sprout) return lavenderSprout;
                if (currentStage == PlantGrowthStage.Growing) return lavenderGrowing;
                if (currentStage == PlantGrowthStage.Blooming) return lavenderBlooming;
                break;
        }
        return null;
    }

    private void LoadPrefabsFromResources()
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
        string[] paths = {
            name1, name2,
            $"Prefabs/{name1}", $"Prefabs/{name2}",
            $"Plants/{name1}", $"Plants/{name2}"
        };

        foreach (string path in paths)
        {
            GameObject prefab = Resources.Load<GameObject>(path);
            if (prefab != null)
            {
                Debug.Log($"Loaded: {path}");
                return prefab;
            }
        }
        return null;
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

    private void UpdateInstruction(string message)
    {
        if (instructionText != null)
            instructionText.text = message;
    }

    public void ResetGrowth()
    {
        growthPoints = 0f;
        currentStage = PlantGrowthStage.Seed;

        if (placementManager != null)
            placementManager.ResetPlacement();
    }

    private void OnDestroy()
    {
        if (placementManager != null)
            placementManager.OnPlacementComplete -= OnPlacementComplete;

        if (voiceController != null)
            voiceController.OnRecognitionSuccess -= OnVoiceSuccess;
    }
}
