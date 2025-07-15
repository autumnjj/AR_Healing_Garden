using UnityEngine;
using System.Collections.Generic;

public class MBTIManager : MonoBehaviour
{
    [Header("데이터 Assets")]
    public MBTIQuestionsSO questionsData;
    public List<PlantDataSO> plantDatabase = new List<PlantDataSO>();

    [Header("현재 상태")]
    private Dictionary<string, float> currentScores;
    private int currentQuestionIndex = 0;
    private UserPersonalityData userData;

    // 이벤트들
    public System.Action<MBTIQuestionsSO.Question> OnQuestionChanged;
    public System.Action<UserPersonalityData, PlantDataSO> OnTestComplete;

    private void Start()
    {
        InitializeScores();
        LoadUserData();

        // 이미 테스틀르 완료했다면 바로 결과 표시
        if(userData.HasData())
        {
            PlantDataSO matchedPlant = FindPlantById(userData.matchedPlantId);
            OnTestComplete?.Invoke(userData, matchedPlant);
        }
        else
        {
            StartTest();
        }
    }

    private void InitializeScores()
    {
        currentScores = new Dictionary<string, float>();
        currentScores.Add("EI", 0f);
        currentScores.Add("SN", 0f);
        currentScores.Add("TF", 0f);
        currentScores.Add("JP", 0f);

        Debug.Log("점수 Dictionary 초기화 완료");
        Debug.Log($"초기화된 키들 : {string.Join(", ", currentScores.Keys)}");
    }

    private void LoadUserData()
    {
        userData = new UserPersonalityData();
        userData.LoadFromPlayerPrefs();
    }

    public void StartTest()
    {
        currentQuestionIndex = 0;
        InitializeScores();
        ShowCurrentQuestion();
    }

    public void RestartTest()
    {
        userData.ClearData();
        StartTest();
    }

    private void ShowCurrentQuestion()
    {
        if(currentQuestionIndex < questionsData.questions.Count)
        {
            OnQuestionChanged?.Invoke(questionsData.questions[currentQuestionIndex]);
        }
    }

    public void AnswerQuestion(int optionIndex) // 0 : A선택지, 1: B선택지
    {
        if (currentQuestionIndex >= questionsData.questions.Count) return;


        var question = questionsData.questions[currentQuestionIndex];
        float scoreValue = (optionIndex == 0) ? question.optionAValue : question.optionBValue;

        Debug.Log($"질문 {currentQuestionIndex} : {question.questionText}");
        Debug.Log($"선택 : {optionIndex}, 차원 : {question.dimension}, 점수 : {scoreValue}");

        // Dictionary 키 확인
        if(currentScores == null)
        {
            Debug.LogError("currentScores가 null입니다!");
            InitializeScores();
        }

        // 점수 추가
        if (question.dimension == "Mixed_TF")
        {
            // 복합 질문 처리
            if (currentScores.ContainsKey("TF")) currentScores["TF"] += scoreValue * 0.5f;
        }
        else if (question.dimension == "Mixed_SN")
        {
            if (currentScores.ContainsKey("SN")) currentScores["SN"] += scoreValue * 0.5f;
        }
        else // 창의적
        {
            if (currentScores.ContainsKey(question.dimension))
            {
                currentScores[question.dimension] += scoreValue;
            }
            else
            {
                Debug.LogError($"키 '{question.dimension}'가 Dictionary에 없습니다!");
                Debug.LogError($"현재 키들 : {string.Join(", ", currentScores.Keys)}");
                return;
            }
        }

        currentQuestionIndex++;

        // 다음 질문 또는 결과 계산
        if(currentQuestionIndex >= questionsData.questions.Count)
        {
            CalculateAndSaveResult();
        }
        else
        {
            ShowCurrentQuestion();
        }
    }

    private void CalculateAndSaveResult()
    {
        // MBTI 타입 결정
        string mbtiType = "";
        mbtiType += (currentScores["EI"] > 0) ? "E" : "I";
        mbtiType += (currentScores["SN"] > 0) ? "S" : "N";
        mbtiType += (currentScores["TF"] > 0) ? "T" : "F";
        mbtiType += (currentScores["JP"] > 0) ? "J" : "P";

        // 사용자 데이터 저장
        userData.mbtiType = mbtiType;
        userData.EI_score = currentScores["EI"];
        userData.SN_score = currentScores["SN"];
        userData.TF_score = currentScores["TF"];
        userData.JP_score = currentScores["JP"];

        // 식물 매칭
        PlantDataSO matchedPlant = FindBestMatchingPlant(mbtiType);
        userData.matchedPlantId = matchedPlant.plantId;

        // PlayerPrefs에 저장
        userData.SaveToPlayerPrefs();

        // 결과 전달
        OnTestComplete?.Invoke(userData, matchedPlant);

        Debug.Log($"MBTI 결과 : {mbtiType}, 매칭된 식물 : {matchedPlant.koreanName}");
    }

    PlantDataSO FindBestMatchingPlant(string mbtiType)
    {
        // 1순위 : 완전 매칭
        foreach(var plant in plantDatabase)
        {
            if (plant.primaryMBTI.Contains(mbtiType))
            {
                return plant;
            }
        }

        // 2순위 : 보조 매칭
        foreach(var plant in plantDatabase)
        {
            if (plant.secondaryMBTI.Contains(mbtiType))
            {
                return plant;
            }
        }

        // 3순위 : 유사도 매칭
        PlantDataSO bestMatch = plantDatabase[0];
        int maxSimilarity = 0;

        foreach (var plant in plantDatabase)
        {
            foreach(var plantMBTI in plant.primaryMBTI)
            {
                int similarity = CalculateSimilarity(mbtiType, plantMBTI);
                if (similarity > maxSimilarity)
                {
                    maxSimilarity = similarity;
                    bestMatch = plant;
                }
            }
        }
        return bestMatch;
    }

    int CalculateSimilarity(string type1, string type2)
    {
        int similarity = 0;
        for(int i = 0; i < 4; i++)
        {
            if (type1[i] == type2[i]) similarity++;
        }
        return similarity;
    }

    PlantDataSO FindPlantById(string plantId)
    {
        foreach (var plant in plantDatabase)
        {
            if (plant.plantId == plantId)
            {
                return plant;
            }
        }
        return plantDatabase[0]; //  기본값
    }


    // UI에서 사용할 메서드들
    public int GetCurrentQuestionIndex() => currentQuestionIndex;
    public int GetTotalQuestions() => questionsData.questions.Count;
    public float GetProgress() => (float) currentQuestionIndex / questionsData.questions.Count;
    public UserPersonalityData GetUserData() => userData;
}
