using System.Collections.Generic;
using UnityEngine;

public enum PlantGrowthStage
{
    Seed = 0,       // ����
    Sprout = 1,     // ����
    Growing = 2,    // ����
    Blooming = 3,   // ��ȭ
}

// �Ĺ� ��ȣ�ۿ� Ÿ��
public enum InteractionType
{
    PositiveTalk,   // �������� ��ȭ
    WaterGiving,    // �� �ֱ�
    TouchCare,      // ��ġ �ɾ�
    SunlightGiving, // �޺� �ֱ�
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
    [Header("���� ����Ʈ ����")]
    public float maxGrowthPoints = 100f;
    public float decayRate = 0.5f;      // �ð��� ����Ʈ ���ҷ�
    public float decayInterval = 3600f; // ���� ����(��)

    [Header("��ȣ�ۿ� ����")]
    public float interactionCooldown = 5f; // ��ȣ�ۿ� ��ٿ�
    public float positiveTalkPoints = 15f;
    public float waterGivingPoints = 10f;
    public float touchCarePoints = 8f;
    public float sunlightGivingPoints = 12f;

    [Header("�ܰ躰 ����Ʈ �Ӱ谪")]
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

        // ��ȣ�ۿ� ī��Ʈ �ʱ�ȭ
        foreach(InteractionType type in System.Enum.GetValues(typeof(InteractionType)))
        {
            interactionCounts[type] = 0;
        }
    }
}
public class PlantGrowthData : MonoBehaviour
{
    [Header("�Ĺ� �⺻ ����")]
    public PlantDataSO plantData;

    [Header("���� �ܰ� ������")]
    public List<PlantGrowthStageData> growthStages = new List<PlantGrowthStageData>();

    [Header("���� ����")]
    public PlantGtowthSettings growthSettings = new PlantGtowthSettings();

    // ���� �Ĺ� ����
    private PlantCurrentState currentState = new PlantCurrentState();

    // �̺�Ʈ��
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

        // �⺻ ���� �ܰ� ����
        if(growthStages.Count == 0)
        {
            SetupDefaultGrowthStages();
        }

        // ����� �����Ͱ� ������ �ʱ�ȭ
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
                stageDescription = "���� ������ ��ٸ��� �־��.",
                requiredGrowthPoints = 0f,
                stageMessages = new List<string> { "������ ������ ��ٸ��� �־��." },
                stageColor = Color.gray,
                stageScale = Vector3.one * 0.5f
            },
            new PlantGrowthStageData
            {
                stage = PlantGrowthStage.Sprout,
                stageDescription = "������ ������ �����߾��.",
                requiredGrowthPoints = growthSettings.sproutThreshold,
                stageMessages = new List<string> { "������ ���ɽ����� ���� ���о��." },
                stageColor = Color.green,
                stageScale = Vector3.one * 0.7f
            },
            new PlantGrowthStageData
            {
                stage = PlantGrowthStage.Growing,
                stageDescription = "�������� �ڶ�� �־��",
                requiredGrowthPoints = growthSettings.growingThreshold,
                stageMessages = new List<string> { "�ǰ��ϰ� �ڶ�� �־��" },
                stageColor = Color.green,
                stageScale = Vector3.one * 1.0f
            },
            new PlantGrowthStageData
            {
                stage = PlantGrowthStage.Blooming,
                stageDescription = "�Ƹ��ٿ� ���� �Ǿ����!",
                requiredGrowthPoints = growthSettings.boomingThreshold,
                stageMessages = new List<string> { "������ ������ �Ƹ��ٿ� ����̿���" },
                stageColor = Color.red,
                stageScale = Vector3.one * 1.2f
            }
        };
    }

    // ������ ���� �޼����
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

    // ���� ������Ʈ �޼����
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

            // ���� �޽��� ǥ��
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

    // ������ ����/�ε�
    public void SavePlantData()
    {
        string plantId = plantData.plantId;
        PlayerPrefs.SetFloat($"Plant_{plantId}_GrowthPoints", currentState.currentGrowthPoints);
        PlayerPrefs.SetInt($"Plant_{plantId}_Stage", (int)currentState.currentStage);
        PlayerPrefs.SetFloat($"Plant_{plantId}_LastInteraction", currentState.lastInteractionTime);
        PlayerPrefs.SetFloat($"Plant_{plantId}_LastDecay", currentState.lastDecayTime);
        PlayerPrefs.SetInt($"Plant_{plantId}_TotalInteractions", currentState.totalInteractions);

        // ��ȣ�ۿ� ī��Ʈ ����
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

        // ��ȣ�ۿ� ī��Ʈ �ε�
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
