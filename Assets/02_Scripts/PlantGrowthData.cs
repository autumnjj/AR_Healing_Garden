using System.Collections.Generic;
using UnityEngine;

public enum PlantGrowthStage
{
    Seed = 0,       // 씨앗
    Sprout = 1,     // 새싹
    Growing = 2,    // 성장
    Blooming = 3,   // 계화
}

// 식물 상호작용 타입
public enum InteractionType
{
    PositiveTalk,   // 긍정적인 대화
    WaterGiving,    // 물 주기
    TouchCare,      // 터치 케어
    SunlightGiving, // 햇빛 주기
}

[System.Serializable]
public class PlantGrowthStageData
{
    public PlantGrowthStage stage;
    public Sprite stageSprite;
    public string stageDescription;
    public float requiredGrowthPoints;
    public List<string> stageMessages;
    public Color stageColor = Color.white;
    public Vector3 stageScale = Vector3.one;
}

[System.Serializable]
public class PlantGtowthSettings
{
    [Header("성장 포인트 설정")]
    public float maxGrowthPoints = 100f;
    public float decayRate = 0.5f;      // 시간당 포인트 감소량
    public float decayInterval = 3600f; // 감소 간격(초)

    [Header("상호작용 설정")]
    public float interactionCooldown = 5f; // 상호작용 쿨다운
    public float positiveTalkPoints = 15f;
    public float waterGivingPoints = 10f;
    public float touchCarePoints = 8f;
    public float sunlightGivingPoints = 12f;

    [Header("단계별 포린트 임계값")]
    public float sproutThreshold = 25f;
    public float growingThreshold = 50f;
    public float boomingThreshold = 75f;

}

[System.Serializable]
public class PlantCurrentState
{
    public PlantGrowthStage currentStage;
    public float currentGrowthPoints;
    public float lastInteractionTime;
    public float lastDecayTime;
    public int totalInteractions;
    public Dictionary<InteractionType, int> interactionCounts;

    public PlantCurrentState()
    {
        currentStage = PlantGrowthStage.Seed;
        currentGrowthPoints = 0f;
        lastInteractionTime = 0f;
        lastDecayTime = 0f;
        totalInteractions = 0;
        interactionCounts = new Dictionary<InteractionType, int>();

        // 상호작용 카운트 초기화
        foreach(InteractionType type in System.Enum.GetValues(typeof(InteractionType)))
        {
            interactionCounts[type] = 0;
        }
    }
}
public class PlantGrowthData : MonoBehaviour
{
    [Header("식물 기본 정보")]
    public PlantDataSO plantData;

    [Header("성장 단계 데이터")]
    public List<PlantGrowthStageData> growthStages = new List<PlantGrowthStageData>();

    [Header("성장 설정")]
    public PlantGtowthSettings growthSettings = new PlantGtowthSettings();

    // 현재 식물 상태
    private PlantCurrentState currentState = new PlantCurrentState();

    // 이벤트들
    public System.Action<PlantGrowthStage> OnStageChanged;
    public System.Action<float> OnGrowthPointsChanged;
    public System.Action<InteractionType, float> OnInteractionPerformed;
    public System.Action<string> OnMessageUpdate;

    private void Start()
    {
        InitializeData();
        LoadPlantData();
    }

    private void InitializeData()
    {
        if(plantData == null)
        {
            Debug.LogError("PlantDataSO is not assigned!");
            return;
        }

        // 기본 성장 단계 설정
        if(growthStages.Count == 0)
        {
            SetupDefaultGrowthStages();
        }

        // 저장된 데이터가 없으면 초기화
        if (!HasSavedData())
        {
            ResetToDefault();
        }
    }

    private void SetupDefaultGrowthStages()
    {
        growthStages = new List<PlantGrowthStageData>
        {
            new PlantGrowthStageData
            {
                stage = PlantGrowthStage.Seed,
                stageDescription = "작은 씨앗이 기다리고 있어요.",
                requiredGrowthPoints = 0f,
                stageMessages = new List<string> { "씨앗이 조용히 기다리고 있어요." },
                stageColor = Color.gray,
                stageScale = Vector3.one * 0.5f
            },
            new PlantGrowthStageData
            {
                stage = PlantGrowthStage.Sprout,
                stageDescription = "새싹이 나오기 시작했어요.",
                requiredGrowthPoints = growthSettings.sproutThreshold,
                stageMessages = new List<string> { "새싹이 조심스럽게 고개를 내밀어요." },
                stageColor = Color.green,
                stageScale = Vector3.one * 0.7f
            },
            new PlantGrowthStageData
            {
                stage = PlantGrowthStage.Growing,
                stageDescription = "무럭무럭 자라고 있어요",
                requiredGrowthPoints = growthSettings.growingThreshold,
                stageMessages = new List<string> { "건강하게 자라고 있어요" },
                stageColor = Color.green,
                stageScale = Vector3.one * 1.0f
            },
            new PlantGrowthStageData
            {
                stage = PlantGrowthStage.Blooming,
                stageDescription = "아름다운 꽃이 피었어요!",
                requiredGrowthPoints = growthSettings.boomingThreshold,
                stageMessages = new List<string> { "완전히 성장한 아름다운 모습이에요" },
                stageColor = Color.red,
                stageScale = Vector3.one * 1.2f
            }
        };
    }

    // 데이터 접근 메서드들
    public PlantCurrentState GetCurrentState()
    {
        return currentState;
    }

    public PlantGrowthStageData GetCurrentStageData()
    {
        return GetStageData(currentState.currentStage);
    }

    public PlantGrowthStageData GetStageData(PlantGrowthStage stage)
    {
        return growthStages.Find(s => s.stage == stage);
    }

    public float GetInteractionPoints(InteractionType interactiontype)
    {
        switch (interactiontype)
        {
            case InteractionType.PositiveTalk:
                return growthSettings.positiveTalkPoints;
            case InteractionType.WaterGiving:
                return growthSettings.waterGivingPoints;
            case InteractionType.TouchCare:
                return growthSettings.touchCarePoints;
            case InteractionType.SunlightGiving:
                return growthSettings.sunlightGivingPoints;
            default:
                return 0f;
        }
    }

    public bool CanInteract()
    {
        return (Time.time - currentState.lastInteractionTime) >= growthSettings.interactionCooldown;
    }

    public bool IsMaxGrowthStage()
    {
        return currentState.currentStage == PlantGrowthStage.Blooming;
    }

    // 상태 업데이트 메서드들
    public void AddGrowthPoints(float points, InteractionType interactionType)
    {
        currentState.currentGrowthPoints += points;
        currentState.currentGrowthPoints = Mathf.Clamp(currentState.currentGrowthPoints, 0f, growthSettings.maxGrowthPoints);

        currentState.lastInteractionTime = Time.time;
        currentState.totalInteractions++;
        currentState.interactionCounts[interactionType]++;

        OnGrowthPointsChanged?.Invoke(currentState.currentGrowthPoints);
        OnInteractionPerformed?.Invoke(interactionType, points);

        CheckStageProgress();
        SavePlantData();
    }

    public void DecayGrwothPoints()
    {
        if(Time.time - currentState.lastDecayTime >= growthSettings.decayInterval)
        {
            currentState.currentGrowthPoints -= growthSettings.decayRate;
            currentState.currentGrowthPoints = Mathf.Max(0f, currentState.currentGrowthPoints);
            currentState.lastDecayTime = Time.time;

            OnGrowthPointsChanged?.Invoke(currentState.currentGrowthPoints);
            CheckStageProgress();
            SavePlantData();
        }
    }

    private void CheckStageProgress()
    {
        PlantGrowthStage newStage = CalculateCurrentStage();

        if(newStage != currentState.currentStage)
        {
            currentState.currentStage = newStage;
            OnStageChanged?.Invoke(newStage);

            // 성장 메시지 표시
            var stageData = GetCurrentStageData();
            if(stageData != null && stageData.stageMessages.Count > 0)
            {
                string message = stageData.stageMessages[Random.Range(0, stageData.stageMessages.Count)];
                OnMessageUpdate?.Invoke($"{message}");
            }
        }
    }

    PlantGrowthStage CalculateCurrentStage()
    {
        if(currentState.currentGrowthPoints >= growthSettings.boomingThreshold)
        {
            return PlantGrowthStage.Blooming;
        }
        if(currentState.currentGrowthPoints >= growthSettings.growingThreshold)
        {
            return PlantGrowthStage.Growing;
        }
        if(currentState.currentGrowthPoints >= growthSettings.sproutThreshold)
        {
            return PlantGrowthStage.Sprout;
        }
        
        return PlantGrowthStage.Seed;
    }

    // 데이터 저장/로드
    public void SavePlantData()
    {
        string plantId = plantData.plantId;
        PlayerPrefs.SetFloat($"Plant_{plantId}_GrowthPoints", currentState.currentGrowthPoints);
        PlayerPrefs.SetInt($"Plant_{plantId}_Stage", (int)currentState.currentStage);
        PlayerPrefs.SetFloat($"Plant_{plantId}_LastInteraction", currentState.lastInteractionTime);
        PlayerPrefs.SetFloat($"Plant_{plantId}_LastDecay", currentState.lastDecayTime);
        PlayerPrefs.SetInt($"Plant_{plantId}_TotalInteractions", currentState.totalInteractions);

        // 상호작용 카운트 저장
        foreach(var kvp in currentState.interactionCounts)
        {
            PlayerPrefs.SetInt($"Plant_{plantId}_Count_{kvp.Key}", kvp.Value);
        }

        PlayerPrefs.Save();
    }

    public void LoadPlantData()
    {
        string plantId = plantData.plantId;
        currentState.currentGrowthPoints = PlayerPrefs.GetFloat($"Plant_{plantId}_GrowthPoints", 0f);
        currentState.currentStage = (PlantGrowthStage)PlayerPrefs.GetInt($"Plant_{plantId}_Stage", 0);
        currentState.lastInteractionTime = PlayerPrefs.GetFloat($"Plant_{plantId}_LastInteraction", 0f);
        currentState.lastDecayTime = PlayerPrefs.GetFloat($"Plant_{plantId}_LastDecay", Time.time);
        currentState.totalInteractions = PlayerPrefs.GetInt($"Plant_{plantId}_TotalInteractions", 0);

        // 상호작용 카운트 로드
        foreach(InteractionType type in System.Enum.GetValues(typeof(InteractionType)))
        {
            currentState.interactionCounts[type] = 
                PlayerPrefs.GetInt($"Plant_{plantId}_Count_{type}", 0);
        }
    }

    public bool HasSavedData()
    {
        return PlayerPrefs.HasKey($"Plant_{plantData.plantId}_GrowthPoints");
    }

    public void ResetToDefault()
    {
        currentState = new PlantCurrentState();
        SavePlantData();
    }
}
