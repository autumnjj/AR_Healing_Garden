using UnityEngine;
using System.Collections.Generic;

public class MBTIManager : MonoBehaviour
{
    [Header("데이터 Assets")]
    public MBTIQuestionsSO questionsData;
    public List<PlantDataSO> plantDatabase = new List<PlantDataSO>();

    [Header("Resources 경로 설정")]
    [SerializeField]
    private string questionsResourcePath = "MBTI/MBTIQuestions";
    [SerializeField]
    private string plantDataResouresPath = "PlantData";

    [Header("현재 상태")]
    private Dictionary<string, float> currentScores;
    private int currentQuestionIndex = 0;
    private UserPersonalityData userData;

    // 이벤트들
    public System.Action<MBTIQuestionsSO.Question> OnQuestionChanged;
    public System.Action<UserPersonalityData, PlantDataSO> OnTestComplete;

    private void Start()
    {
        LoadDataFromResources();
        InitializeScores();
        LoadUserData();

        Debug.Log($"Loaded questions: {questionsData?.questions?.Count ?? 0}");
        Debug.Log($"User has data: {userData.HasData()}");

        Invoke(nameof(StartFreshTest), 0.1f);
    }
    private void StartFreshTest()
    {
        Debug.Log("Starting fresh test with questions");
        StartTest();
    }


    private void LoadDataFromResources()
    {
        if(questionsData == null)
        {
            questionsData = Resources.Load<MBTIQuestionsSO>(questionsResourcePath);
            if (questionsData == null)
            {
                Debug.LogError($"Resources/{questionsResourcePath} cannot find MBTIQuestionsSO.");
            }
            else
            {
                Debug.Log("Resources Folder Load MBTI Questions Data");
            }
        }

        if (plantDatabase.Count == 0)
        {
            PlantDataSO[] plants = Resources.LoadAll<PlantDataSO>(plantDataResouresPath);
            if(plants.Length > 0)
            {
                plantDatabase.AddRange(plants);
                Debug.Log($"Resources Folder Load {plants.Length} plant data");
            }
            else
            {
                Debug.LogWarning($"Resources/{plantDataResouresPath} cannot find PlantDataSO");
            }
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
        if(questionsData == null)
        {
            Debug.LogError("questionsData is null. Did not load in Resources Folder");
            return;
        }

        if (questionsData.questions == null || questionsData.questions.Count == 0)
        {
            Debug.LogError("Question Data is empty");
            return;
        } 
        currentQuestionIndex = 0;
        InitializeScores();

        ShowCurrentQuestion();
    }


    private void ShowCurrentQuestion()
    {
        if(questionsData != null && currentQuestionIndex < questionsData.questions.Count)
        {
            var question = questionsData.questions[currentQuestionIndex];
            OnQuestionChanged?.Invoke(question);
        }
    }

    public void AnswerQuestion(int optionIndex) // 0 : A선택지, 1: B선택지
    {
        if (questionsData == null || currentQuestionIndex >= questionsData.questions.Count) return;

        var question = questionsData.questions[currentQuestionIndex];
        float scoreValue = (optionIndex == 0) ? question.optionAValue : question.optionBValue;

        Debug.Log($"Question {currentQuestionIndex} : {question.questionText}");
        Debug.Log($"Selected : {optionIndex}, 차원 : {question.dimension}, Score : {scoreValue}");

        // Dictionary 키 확인
        if(currentScores == null)
        {
            Debug.LogError("currentScores가 null입니다!");
            InitializeScores();
        }

        if (currentScores.ContainsKey(question.dimension))
        {
            currentScores[question.dimension] += scoreValue;
        }
        else
        {
            Debug.LogError($"키 '{question.dimension}'가 Dictionary에 없습니다.");
            return;
        }

        currentQuestionIndex++;

        Debug.Log($"Now Question Index : {currentQuestionIndex}, Total question count: {questionsData.questions.Count}");

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

        Debug.Log($"Calculate MBTI: {mbtiType}");

        // 사용자 데이터 저장
        userData.mbtiType = mbtiType;
        userData.EI_score = currentScores["EI"];
        userData.SN_score = currentScores["SN"];
        userData.TF_score = currentScores["TF"];
        userData.JP_score = currentScores["JP"];

        // 식물 매칭
        PlantDataSO matchedPlant = FindBestMatchingPlant(mbtiType);
        if (matchedPlant == null)
        {
            Debug.LogError("There is no matching plants");
            return;
        }
        userData.matchedPlantId = matchedPlant.plantId;

        // PlayerPrefs에 저장
        userData.SaveToPlayerPrefs();

        // 결과 전달
        OnTestComplete?.Invoke(userData, matchedPlant);

        Debug.Log($"MBTI 결과 : {mbtiType}, 매칭된 식물 : {matchedPlant.koreanName}");
    }

    PlantDataSO FindBestMatchingPlant(string mbtiType)
    {
       if (plantDatabase.Count == 0)
        {
            Debug.LogError("PlantDatabase is empty");
            return null;
        }

        // 4개 식물로 단순화된 매칭
        Dictionary<string, string> mbtiToPlantId = new Dictionary<string, string>
       {
           // 해바라기 그룹(외향적, 활발한)
           {"ENFP", "sunflower" }, {"ENFJ", "sunflower" }, {"ESFP", "sunflower" }, {"ESFJ", "sunflower" },

           // 선인장 그룹(독립적, 강인한)
           {"INTJ", "cactus" }, {"INTP", "cactus" }, {"ENTJ", "cactus" }, {"ENTP", "cactus" },

           // 라벤더 그룹(조용한, 섬세한)
           {"ISFP", "lavender" },{"INFP", "lavender" },{"ISFJ", "lavender" },{"INFJ", "lavender" },

           // 장미 그룹(체계적, 실용적)
           {"ESTJ", "rose" }, {"ESTP", "rose" }, {"ISTJ", "rose" }, {"ISTP", "rose" }
       };

        // 매칭도닌 식물 ID 찾기
        string targetPlantId = mbtiToPlantId.ContainsKey(mbtiType) ? mbtiToPlantId[mbtiType] : "sunflower";

        // PlantDatabase에서 해당 ID의 식물 찾기
        foreach(var plant in plantDatabase)
        {
            if (plant.plantId == targetPlantId)
            {
                Debug.Log($"MBTI {mbtiType}에 매칭된 식물: {plant.koreanName}");
                return plant;
            }
        }

        // 못찾으면 첫번째 식물 반환
        Debug.LogWarning($"Cant not find Plant ID '{targetPlantId}' use basic plant");
        return plantDatabase[0];
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
        return plantDatabase.Count > 0 ? plantDatabase[0] : null; //  기본값
    }


    // UI에서 사용할 메서드들
    public int GetCurrentQuestionIndex() => currentQuestionIndex;
    public int GetTotalQuestions() => questionsData != null ? questionsData.questions.Count : 0;
    public float GetProgress() => questionsData != null ? (float) currentQuestionIndex / questionsData.questions.Count : 0f;
    public UserPersonalityData GetUserData() => userData;
}
