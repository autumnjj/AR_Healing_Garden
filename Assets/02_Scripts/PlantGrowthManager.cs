using UnityEngine;
using System.Collections;

public class PlantGrowthManager : MonoBehaviour
{
    [Header("�ٽ� ������Ʈ")]
    public PlantGrowthData plantGrowthData;
    public PlantInteractionSystem interactionSystem;
    public PlantGrowthUI growthUI;

    [Header("MBTI ����")]
    public MBTIManager mbtiManager;

    [Header("�ڵ� ���� ����")]
    public bool startWithMBTIResult = true;
    public bool enableAutoDecay = true;

    private void Start()
    {
        InitializeComponents();
        ConnectEvents();

        if (startWithMBTIResult)
        {
            ConnectToMBTI();
        }

        if (enableAutoDecay)
        {
            StartCoroutine(AutoDecayRoutine());
        }

        Debug.Log("PlantGrowthManager �ʱ�ȭ �Ϸ�");
    }

    private void InitializeComponents()
    {
        // �ʼ� ������Ʈ Ȯ��
        if(plantGrowthData == null) plantGrowthData = GetComponent<PlantGrowthData>();

        if(interactionSystem == null) interactionSystem = GetComponent<PlantInteractionSystem>();

        if(growthUI == null) growthUI = GetComponent<PlantGrowthUI>();
        
        // ������Ʈ �� ���� ����
        if(interactionSystem != null)
        {
            interactionSystem.plantGrowthData = plantGrowthData;
            interactionSystem.plantGrowthUI = growthUI;
        }

        if(growthUI != null)
        {
            growthUI.plantGrowthData = plantGrowthData;
        }

        // ������ ������Ʈ ���
        if(plantGrowthData == null) Debug.LogError("PlantGrowthData ������Ʈ�� ã�� �� �����ϴ�.");

        if(interactionSystem == null) Debug.LogError("PlantInteractionSystem ������Ʈ�� ã�� �� �����ϴ�.");

        if(growthUI == null) Debug.LogError("PlantGrowthUI ������Ʈ�� ã�� �� �����ϴ�.");
    }

    private void ConnectEvents()
    {
        if(plantGrowthData != null)
        {
            plantGrowthData.OnStageChanged += HandleStageChanged;
            plantGrowthData.OnGrowthPointsChanged += HandleGrowthPointsChanged;
            plantGrowthData.OnMessageUpdate += HandleMessageUpdate;
        }
    }

    private void ConnectToMBTI()
    {
        if(mbtiManager == null) mbtiManager = FindAnyObjectByType<MBTIManager>();

        if (mbtiManager != null)
        {
            mbtiManager.OnTestComplete += HandleMBTITestComplete;

            // �̹� �Ϸ�� MBTI ����� �ִ��� Ȯ��
            var userData = mbtiManager.GetUserData();
            if(userData != null && userData.HasData())
            {
                SetPlantFromMBTI(userData.matchedPlantId);
            }
        }
    }


    private void HandleMBTITestComplete(UserPersonalityData userData, PlantDataSO matchedPlant)
    {
        SetPlantFromMBTI(matchedPlant.plantId);

        if(growthUI != null)
        {
            growthUI.ShowMessage($"{matchedPlant.koreanName}�� �Բ� �����غ�����!", Color.green);
        }
    }

    private void SetPlantFromMBTI(string plantId)
    {
        if(plantGrowthData == null) return;

        // ���� �Ĺ� �����Ͱ� �ٸ��� ����
        if(plantGrowthData.plantData == null || plantGrowthData.plantData.plantId != plantId)
        {
            // ���ο� �Ĺ� ������ ã��
            PlantDataSO newPlantData = FindPlantDataById(plantId);
            if(newPlantData != null)
            {
                plantGrowthData.plantData = newPlantData;

                // UI ������Ʈ
                if (growthUI != null)
                {
                    growthUI.InitializeDisplay();
                }
                Debug.Log($"�Ĺ��� {newPlantData.koreanName}���� ����Ǿ����ϴ�.");
            }
        }
    }

    private void LoadMBTIResult()
    {
        // PlayerPrefs���� MBTI ��� �ε�
        string mbtiType = PlayerPrefs.GetString("MBTI_Type", "");
        string matchedPlantId = PlayerPrefs.GetString("Matched_Plant", "");

        if (!string.IsNullOrEmpty(mbtiType) && !string .IsNullOrEmpty(matchedPlantId))
        {
            Debug.Log($"MBTI ��� �ε� : {mbtiType} -> {matchedPlantId}");

            // �ش� �Ĺ� ������ ã��
            PlantDataSO matchedPlant = FindPlantDataById(matchedPlantId);
            if (matchedPlant != null) 
            {
                SetPlantData(matchedPlant);

                // ȯ�� �޽���
                if(growthUI != null)
                {
                    growthUI.ShowMessage($"{matchedPlant.koreanName}�� �Բ� �����غ�����!", Color.green);
                }
            }
            else
            {
                Debug.LogWarning($"�Ĺ� ID '{matchedPlantId}'�� ã�� �� �����ϴ�. �⺻ �Ĺ� ���.");
                SetDefaultPlant();
            }
        }
        else
        {
            Debug.Log("MBTI �׽�Ʈ ����� �����ϴ�. �⺻ �Ĺ� ���.");
            SetDefaultPlant();
        }
    }

    PlantDataSO FindPlantDataById(string plantId)
    {
        // MBTIManager�� ���� ������ �װ��� ���
        if(mbtiManager != null)
        {
            foreach(var plant in mbtiManager.plantDatabase)
            {
                if(plant != null && plant.plantId == plantId)
                {
                    return plant;
                }
            }
        }
        // MBTIManager�� ������ ��� PlantDataSO assets �˻�
        PlantDataSO[] allPlants = FindAllPlantAssets();

        foreach(var plant in allPlants)
        {
            if(plant != null && plant.plantId == plantId)
            {
                return plant;
            }
        }
        Debug.LogWarning($"�Ĺ� ID '{plantId}'�� ã���� �� �����ϴ�.");
        return null;
    }

    PlantDataSO[] FindAllPlantAssets()
    {
        // Project�� ��� PlantDataSO assetã��
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:PlantDataSO");
        PlantDataSO[] plants = new PlantDataSO[guids.Length];

        for (int i = 0; i < guids.Length; i++)
        { 
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[i]);
            plants[i] = UnityEditor.AssetDatabase.LoadAssetAtPath<PlantDataSO>(path);
        }
        return plants;
    }


    private void SetDefaultPlant()
    {
        // ù ��° �Ĺ��� �⺻������ ���
        PlantDataSO[] allPlants = FindAllPlantAssets();
        if(allPlants.Length > 0 && allPlants[0] != null)
        {
            SetPlantData(allPlants[0]);
            Debug.Log($"�⺻ �Ĺ� ���� : {allPlants[0].koreanName}");
        }
        else
        {
            Debug.LogError("��� ������ �Ĺ� �����Ͱ� �����ϴ�.");
        }
    }

    private void HandleStageChanged(PlantGrowthStage newStage)
    {
        Debug.Log($"�Ĺ��� {newStage}�ܰ�� �����߽��ϴ�!");

        // ���� �ܰ躰 Ư�� ȿ��
        switch(newStage)
        {
            case PlantGrowthStage.Sprout:
                PlayGrowthCelebration("������ ���Ƴ����!");
                break;
            case PlantGrowthStage.Growing:
                PlayGrowthCelebration("�������� �ڶ�� �־��!");
                break;
            case PlantGrowthStage.Blooming:
                PlayGrowthCelebration("�Ƹ��ٿ� ���� �Ǿ����!");
                PlayMaxGrowthCelebration();
                break;
        }
    }

    private void HandleGrowthPointsChanged(float newPoints)
    {
        // ���� ����Ʈ ��ȭ �α�
        Debug.Log($"���� ���� ����Ʈ : {newPoints}");
    }

    private void HandleMessageUpdate(string message)
    {
        Debug.Log($"�Ĺ� �޽��� : {message}");
    }

    private void PlayGrowthCelebration(string message)
    {
        if(growthUI != null)
        {
            growthUI.ShowMessage(message, Color.yellow);
        }
    }

    private void PlayMaxGrowthCelebration()
    {
        // �ִ� ���� �� Ư�� ȿ��
        if(growthUI != null)
        {
            StartCoroutine(MaxGrowthCelebrationRoutine());
        }
    }

    IEnumerator MaxGrowthCelebrationRoutine()
    {
        // 3�� �ݺ��Ǵ� ���� ȿ��
        for(int i = 0; i < 3; i++)
        {
            if(growthUI != null)
            {
                Debug.Log("�ִ� ���� ���� ȿ��!");
            }
            yield return new WaitForSeconds(0.5f);
        }

        // ���� ���� �޽���
        if(growthUI != null)
        {
            growthUI.ShowMessage("�����մϴ�! �Ĺ��� ������ �����߽��ϴ�!", Color.gold);
        }
    }

    IEnumerator AutoDecayRoutine()
    {
        while(true)
        {
            yield return new WaitForSeconds(1f); 
            if(plantGrowthData != null)
            {
                plantGrowthData.DecayGrwothPoints(); 
            }
        }
    }

    // ���� �޼����
    public void PauseGrowthSystem()
    {
        enableAutoDecay = false;

        if(interactionSystem != null)
        {
            interactionSystem.SetInteractionEnabled(false);
        }
    }

    public void ResumeGrowthSystem()
    {
        enableAutoDecay = true;
        if(interactionSystem != null)
        {
            interactionSystem.SetInteractionEnabled(true);
        }

        if(enableAutoDecay)
        {
            StartCoroutine(AutoDecayRoutine());
        }
    }

    public void SetPlantData(PlantDataSO newPlantData)
    {
        if(plantGrowthData != null)
        {
            plantGrowthData.plantData = newPlantData;
            
            if(growthUI != null)
            {
                growthUI.InitializeDisplay();
            }
        }
    }

    public PlantCurrentState GetCurrentPlantState()
    {
        return plantGrowthData?.GetCurrentState();
    }

    public string GetSystemStatus()
    {
        if(plantGrowthData == null) return "�ý��� ����";

        var state = plantGrowthData.GetCurrentState();
        var stageData = plantGrowthData.GetCurrentStageData();

        return $"�Ĺ�: {plantGrowthData.plantData?.koreanName ?? "����"}\n" +
            $"�ܰ�: {state.currentStage}\n" +
            $"����Ʈ : {state.currentGrowthPoints:F1}\n +" +
            $"�� ��ȣ�ۿ�: {state.totalInteractions}\n +" +
            $"����: {stageData?.stageDescription ?? "����"}";
    }

    private void OnDestroy()
    {
        // �̺�Ʈ ���� ����
        if(plantGrowthData != null)
        {
            plantGrowthData.OnStageChanged -= HandleStageChanged;
            plantGrowthData.OnGrowthPointsChanged -= HandleGrowthPointsChanged;
            plantGrowthData.OnMessageUpdate -= HandleMessageUpdate;
        }
        if(mbtiManager != null)
        {
            mbtiManager.OnTestComplete -= HandleMBTITestComplete;
        }
    }
}
