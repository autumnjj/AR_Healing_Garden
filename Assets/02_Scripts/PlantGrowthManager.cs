using UnityEngine;
using System.Collections;

public class PlantGrowthManager : MonoBehaviour
{
    [Header("핵심 컴포넌트")]
    public PlantGrowthData plantGrowthData;
    public PlantInteractionSystem interactionSystem;
    public PlantGrowthUI growthUI;

    [Header("MBTI 연동")]
    public MBTIManager mbtiManager;

    [Header("자동 실행 설정")]
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

        Debug.Log("PlantGrowthManager 초기화 완료");
    }

    private void InitializeComponents()
    {
        // 필수 컴포넌트 확인
        if(plantGrowthData == null) plantGrowthData = GetComponent<PlantGrowthData>();

        if(interactionSystem == null) interactionSystem = GetComponent<PlantInteractionSystem>();

        if(growthUI == null) growthUI = GetComponent<PlantGrowthUI>();
        
        // 컴포넌트 간 참조 설정
        if(interactionSystem != null)
        {
            interactionSystem.plantGrowthData = plantGrowthData;
            interactionSystem.plantGrowthUI = growthUI;
        }

        if(growthUI != null)
        {
            growthUI.plantGrowthData = plantGrowthData;
        }

        // 누락된 컴포넌트 경고
        if(plantGrowthData == null) Debug.LogError("PlantGrowthData 컴포넌트를 찾을 수 없습니다.");

        if(interactionSystem == null) Debug.LogError("PlantInteractionSystem 컴포넌트를 찾을 수 없습니다.");

        if(growthUI == null) Debug.LogError("PlantGrowthUI 컴포넌트를 찾을 수 없습니다.");
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

            // 이미 완료된 MBTI 결과가 있는지 확인
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
            growthUI.ShowMessage($"{matchedPlant.koreanName}과 함께 성장해보세요!", Color.green);
        }
    }

    private void SetPlantFromMBTI(string plantId)
    {
        if(plantGrowthData == null) return;

        // 현재 식물 데이터가 다르면 변경
        if(plantGrowthData.plantData == null || plantGrowthData.plantData.plantId != plantId)
        {
            // 새로운 식물 데이터 찾기
            PlantDataSO newPlantData = FindPlantDataById(plantId);
            if(newPlantData != null)
            {
                plantGrowthData.plantData = newPlantData;

                // UI 업데이트
                if (growthUI != null)
                {
                    growthUI.InitializeDisplay();
                }
                Debug.Log($"식물이 {newPlantData.koreanName}으로 변경되었습니다.");
            }
        }
    }

    private void LoadMBTIResult()
    {
        // PlayerPrefs에서 MBTI 결과 로드
        string mbtiType = PlayerPrefs.GetString("MBTI_Type", "");
        string matchedPlantId = PlayerPrefs.GetString("Matched_Plant", "");

        if (!string.IsNullOrEmpty(mbtiType) && !string .IsNullOrEmpty(matchedPlantId))
        {
            Debug.Log($"MBTI 결과 로드 : {mbtiType} -> {matchedPlantId}");

            // 해당 식물 데이터 찾기
            PlantDataSO matchedPlant = FindPlantDataById(matchedPlantId);
            if (matchedPlant != null) 
            {
                SetPlantData(matchedPlant);

                // 환영 메시지
                if(growthUI != null)
                {
                    growthUI.ShowMessage($"{matchedPlant.koreanName}과 함께 성장해보세요!", Color.green);
                }
            }
            else
            {
                Debug.LogWarning($"식물 ID '{matchedPlantId}'를 찾을 수 없습니다. 기본 식물 사용.");
                SetDefaultPlant();
            }
        }
        else
        {
            Debug.Log("MBTI 테스트 결과가 없습니다. 기본 식물 사용.");
            SetDefaultPlant();
        }
    }

    PlantDataSO FindPlantDataById(string plantId)
    {
        // MBTIManager가 씬에 있으면 그것을 사용
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
        // MBTIManager가 없으면 모든 PlantDataSO assets 검색
        PlantDataSO[] allPlants = FindAllPlantAssets();

        foreach(var plant in allPlants)
        {
            if(plant != null && plant.plantId == plantId)
            {
                return plant;
            }
        }
        Debug.LogWarning($"식물 ID '{plantId}'를 찾ㄱ을 수 없습니다.");
        return null;
    }

    PlantDataSO[] FindAllPlantAssets()
    {
        // Project의 모든 PlantDataSO asset찾기
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
        // 첫 번째 식물을 기본값으로 사용
        PlantDataSO[] allPlants = FindAllPlantAssets();
        if(allPlants.Length > 0 && allPlants[0] != null)
        {
            SetPlantData(allPlants[0]);
            Debug.Log($"기본 식물 설정 : {allPlants[0].koreanName}");
        }
        else
        {
            Debug.LogError("사용 가능한 식물 데이터가 없습니다.");
        }
    }

    private void HandleStageChanged(PlantGrowthStage newStage)
    {
        Debug.Log($"식물이 {newStage}단계로 성장했습니다!");

        // 성장 단계별 특별 효과
        switch(newStage)
        {
            case PlantGrowthStage.Sprout:
                PlayGrowthCelebration("새싹이 돋아났어요!");
                break;
            case PlantGrowthStage.Growing:
                PlayGrowthCelebration("무럭무럭 자라고 있어요!");
                break;
            case PlantGrowthStage.Blooming:
                PlayGrowthCelebration("아름다운 꽃이 피었어요!");
                PlayMaxGrowthCelebration();
                break;
        }
    }

    private void HandleGrowthPointsChanged(float newPoints)
    {
        // 성장 포인트 변화 로깅
        Debug.Log($"현재 성장 포인트 : {newPoints}");
    }

    private void HandleMessageUpdate(string message)
    {
        Debug.Log($"식물 메시지 : {message}");
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
        // 최대 성장 시 특별 효과
        if(growthUI != null)
        {
            StartCoroutine(MaxGrowthCelebrationRoutine());
        }
    }

    IEnumerator MaxGrowthCelebrationRoutine()
    {
        // 3번 반복되는 축하 효과
        for(int i = 0; i < 3; i++)
        {
            if(growthUI != null)
            {
                Debug.Log("최대 성장 축하 효과!");
            }
            yield return new WaitForSeconds(0.5f);
        }

        // 최종 축하 메시지
        if(growthUI != null)
        {
            growthUI.ShowMessage("축하합니다! 식물이 완전히 성장했습니다!", Color.gold);
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

    // 공개 메서드들
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
        if(plantGrowthData == null) return "시스템 오류";

        var state = plantGrowthData.GetCurrentState();
        var stageData = plantGrowthData.GetCurrentStageData();

        return $"식물: {plantGrowthData.plantData?.koreanName ?? "없음"}\n" +
            $"단계: {state.currentStage}\n" +
            $"포인트 : {state.currentGrowthPoints:F1}\n +" +
            $"총 상호작용: {state.totalInteractions}\n +" +
            $"설명: {stageData?.stageDescription ?? "없음"}";
    }

    private void OnDestroy()
    {
        // 이벤트 연결 해제
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
