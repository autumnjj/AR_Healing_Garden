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

    [Header("Image Effect")]
    public SimpleImageEffects imageEffects;

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

    [Header("Plant Positioning Strategy")]
    public float pivotCenterOffset = 0.15f;

    private Vector3 seedOriginalPosition;  // Seed�� ���� ��ġ ����
    private Quaternion seedOriginalRotation; // Seed�� ���� ȸ�� ����

    // ������Ʈ
    private GameObject tableInstance;
    private GameObject currentPlantInstance;

    // ����
    public bool IsPlaced { get; private set; } = false;
    private PlantGrowthStage currentStage = PlantGrowthStage.Seed;
    private string selectedPlantType = "sunflower";
    private float growthPoints = 0f;
    

    private void Awake()
    {
        // Resources���� ������ �ε�
        LoadPrefabsFromResources();
    }


    private void Start()
    {
        SetupCamera();
        SetupPlantType();
        // UI �ʱ�ȭ
        InitializeUI();
        // �ڵ� ����
        StartCoroutine(InitialSetup());
    }

    private void SetupCamera()
    {
        // ī�޶� ã��
        if (arCamera == null)
        {
            arCamera = Camera.main;
            if (arCamera == null)
                arCamera = FindAnyObjectByType<Camera>();
        }
    }

    private void SetupPlantType()
    {
        // ���õ� �Ĺ� Ÿ�� �ε�(�⺻��: sunflower)
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
        // ��ȿ�� �˻�
        ValidatePlantType();
    }

    private void LoadPrefabsFromResources()
    {
        // �Ĺ� �����յ� �ε�
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
        // ���� ��� �õ�
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
            Debug.LogWarning($"Invalid plant type : {selectedPlantType}, using default: sunflower");
            selectedPlantType = "sunflower";
        }
    }

    private void InitializeUI()
    {
        if (voiceUI != null)
            voiceUI.SetActive(false);

        UpdateInstruction($"{GetCurrentPlantName()}(��)�� �غ�Ǿ����ϴ�1");
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
        }

        if (plantSpawnPoint == null)
        {
            return;
        }

        // �̸� ��ġ�� ���� ���� (���ۿ�, ���߿� ��ü��)
        if (preplacedSeed != null)
        {
            // Seed�� ���� ��ġ�� ���� ��ġ�� ���� (������ ��ġ�� ��ġ)
            seedOriginalPosition = preplacedSeed.transform.position;
            seedOriginalRotation = preplacedSeed.transform.rotation;

            // PlantSpawnPoint�� Seed ��ġ�� ������Ʈ
            plantSpawnPoint.position = seedOriginalPosition;
            plantSpawnPoint.rotation = seedOriginalRotation;

            currentPlantInstance = preplacedSeed;
        }
    }


    private void OnPlacementComplete()
    {
        IsPlaced = true;

        if (voiceUI != null)
            voiceUI.SetActive(true);

        // ���� �ν� ����
        if (voiceController != null)
        {
            voiceController.enabled = true;
            voiceController.OnVoiceRecognitionSuccess += OnVoiceSuccess;
        }

        UpdateInstruction("ȭ�鿡 ������ ������ ���� ���غ�����!");

    }

    private void OnVoiceSuccess(string recognizedText)
    {
        // ���� ����Ʈ �߰�
        growthPoints += pointsPerVoiceSuccess;
        growthPoints = Mathf.Clamp(growthPoints, 0f, maxGrowthPoints);

        if (imageEffects != null)
            imageEffects.PlayVoiceSuccessEffect();

        // UI ������Ʈ
        UpdateInstruction($"���߾��! ���� ����Ʈ: {growthPoints:F0}/{maxGrowthPoints} (+{pointsPerVoiceSuccess})");

        // ���� üũ
        CheckGrowth();
    }

    private void CheckGrowth()
    {
        PlantGrowthStage newStage = PlantGrowthStage.Seed;

        // ���ο� ���� �ܰ� ���
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

        Quaternion currentRotation = currentPlantInstance.transform.rotation;
        Vector3 currentScale = currentPlantInstance.transform.localScale;

        Debug.Log($"Attempting to change plant to stage: {currentStage} for plant type: {selectedPlantType}");

        // �� �Ĺ� ������ ��������
        GameObject newPlantPrefab = GetPlantPrefab();

        if (newPlantPrefab == null)
        {
            return;
        }

        Debug.Log($"Found prefab: {newPlantPrefab.name} for stage: {currentStage}");

        // ���� �Ĺ� ����
        Destroy(currentPlantInstance);

        // 3D ������Ʈ�� �߰� pivot ����
        Vector3 spawnPosition = plantSpawnPoint.position;
        if (currentStage != PlantGrowthStage.Seed)
        {
            spawnPosition.y += pivotCenterOffset;
            Debug.Log($"Applied pivot center offset: {pivotCenterOffset}");
        }

        // �� �Ĺ� ���� - ������ ��ġ ���
        currentPlantInstance = Instantiate(newPlantPrefab, spawnPosition, plantSpawnPoint.rotation);

        // ũ��� ���� �� ����
        currentPlantInstance.transform.localScale = currentScale;

        // ���� �ܰ躰 �̹��� ����Ʈ (���⼭��!)
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

        string stageName = GetStageName();
        string celebrationMessage = GetCelebrationMessage();
        UpdateInstruction(celebrationMessage);

        if (currentStage == PlantGrowthStage.Blooming)
        {
            OnPlantFullyGrown();
        }
    }


    private void OnPlantFullyGrown()
    {
        UpdateInstruction($"{GetCurrentPlantName()}(��)�� ������ �Ǿ���!\n ����� �������� ���� ������ ��������ϴ�!");
        if (voiceController != null)
            voiceController.OnAllTargetsComplete();

        StartCoroutine(CelebrationEffect());
    }

    private IEnumerator CelebrationEffect()
    {
        yield return new WaitForSeconds(1.5f);
    }

    private GameObject GetPlantPrefab()
    {
        if(currentStage == PlantGrowthStage.Seed)
            return null;
        
        // ���õ� �Ĺ� Ÿ�Կ� ���� ������ ��ȯ
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
            case PlantGrowthStage.Sprout: return "����";
            case PlantGrowthStage.Growing: return "����";
            case PlantGrowthStage.Blooming: return "��ȭ";
            default: return "����";
        }
    }

    private string GetCelebrationMessage()
    {
        switch (currentStage)
        {
            case PlantGrowthStage.Sprout:
                return $"���� ������ ���Ƴ����!";
            case PlantGrowthStage.Growing:
                return $"���� �ڶ�� �־��!";
            case PlantGrowthStage.Blooming:
                return $"������ �Ǿ���!";
            default:
                return "�����ϰ� �־��!";
        }
    }

    public string GetCurrentPlantName()
    {
        switch (selectedPlantType)
        {
            case "sunflower": return "�عٶ��";
            case "rose": return "���";
            case "cactus": return "������";
            case "lavender": return "�󺥴�";
            default: return "�Ĺ�";
        }
    }

    // UI ������Ʈ �޼����
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
