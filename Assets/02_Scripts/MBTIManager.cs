using UnityEngine;
using System.Collections.Generic;

public class MBTIManager : MonoBehaviour
{
    [Header("������ Assets")]
    public MBTIQuestionsSO questionsData;
    public List<PlantDataSO> plantDatabase = new List<PlantDataSO>();

    [Header("Resources ��� ����")]
    [SerializeField]
    private string questionsResourcePath = "MBTI/MBTIQuestions";
    [SerializeField]
    private string plantDataResouresPath = "PlantData";

    [Header("���� ����")]
    private Dictionary<string, float> currentScores;
    private int currentQuestionIndex = 0;
    private UserPersonalityData userData;

    // �̺�Ʈ��
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

        Debug.Log("���� Dictionary �ʱ�ȭ �Ϸ�");
        Debug.Log($"�ʱ�ȭ�� Ű�� : {string.Join(", ", currentScores.Keys)}");
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

    public void AnswerQuestion(int optionIndex) // 0 : A������, 1: B������
    {
        if (questionsData == null || currentQuestionIndex >= questionsData.questions.Count) return;

        var question = questionsData.questions[currentQuestionIndex];
        float scoreValue = (optionIndex == 0) ? question.optionAValue : question.optionBValue;

        Debug.Log($"Question {currentQuestionIndex} : {question.questionText}");
        Debug.Log($"Selected : {optionIndex}, ���� : {question.dimension}, Score : {scoreValue}");

        // Dictionary Ű Ȯ��
        if(currentScores == null)
        {
            Debug.LogError("currentScores�� null�Դϴ�!");
            InitializeScores();
        }

        if (currentScores.ContainsKey(question.dimension))
        {
            currentScores[question.dimension] += scoreValue;
        }
        else
        {
            Debug.LogError($"Ű '{question.dimension}'�� Dictionary�� �����ϴ�.");
            return;
        }

        currentQuestionIndex++;

        Debug.Log($"Now Question Index : {currentQuestionIndex}, Total question count: {questionsData.questions.Count}");

        // ���� ���� �Ǵ� ��� ���
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
        // MBTI Ÿ�� ����
        string mbtiType = "";
        mbtiType += (currentScores["EI"] > 0) ? "E" : "I";
        mbtiType += (currentScores["SN"] > 0) ? "S" : "N";
        mbtiType += (currentScores["TF"] > 0) ? "T" : "F";
        mbtiType += (currentScores["JP"] > 0) ? "J" : "P";

        Debug.Log($"Calculate MBTI: {mbtiType}");

        // ����� ������ ����
        userData.mbtiType = mbtiType;
        userData.EI_score = currentScores["EI"];
        userData.SN_score = currentScores["SN"];
        userData.TF_score = currentScores["TF"];
        userData.JP_score = currentScores["JP"];

        // �Ĺ� ��Ī
        PlantDataSO matchedPlant = FindBestMatchingPlant(mbtiType);
        if (matchedPlant == null)
        {
            Debug.LogError("There is no matching plants");
            return;
        }
        userData.matchedPlantId = matchedPlant.plantId;

        // PlayerPrefs�� ����
        userData.SaveToPlayerPrefs();

        // ��� ����
        OnTestComplete?.Invoke(userData, matchedPlant);

        Debug.Log($"MBTI ��� : {mbtiType}, ��Ī�� �Ĺ� : {matchedPlant.koreanName}");
    }

    PlantDataSO FindBestMatchingPlant(string mbtiType)
    {
       if (plantDatabase.Count == 0)
        {
            Debug.LogError("PlantDatabase is empty");
            return null;
        }

        // 4�� �Ĺ��� �ܼ�ȭ�� ��Ī
        Dictionary<string, string> mbtiToPlantId = new Dictionary<string, string>
       {
           // �عٶ�� �׷�(������, Ȱ����)
           {"ENFP", "sunflower" }, {"ENFJ", "sunflower" }, {"ESFP", "sunflower" }, {"ESFJ", "sunflower" },

           // ������ �׷�(������, ������)
           {"INTJ", "cactus" }, {"INTP", "cactus" }, {"ENTJ", "cactus" }, {"ENTP", "cactus" },

           // �󺥴� �׷�(������, ������)
           {"ISFP", "lavender" },{"INFP", "lavender" },{"ISFJ", "lavender" },{"INFJ", "lavender" },

           // ��� �׷�(ü����, �ǿ���)
           {"ESTJ", "rose" }, {"ESTP", "rose" }, {"ISTJ", "rose" }, {"ISTP", "rose" }
       };

        // ��Ī���� �Ĺ� ID ã��
        string targetPlantId = mbtiToPlantId.ContainsKey(mbtiType) ? mbtiToPlantId[mbtiType] : "sunflower";

        // PlantDatabase���� �ش� ID�� �Ĺ� ã��
        foreach(var plant in plantDatabase)
        {
            if (plant.plantId == targetPlantId)
            {
                Debug.Log($"MBTI {mbtiType}�� ��Ī�� �Ĺ�: {plant.koreanName}");
                return plant;
            }
        }

        // ��ã���� ù��° �Ĺ� ��ȯ
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
        return plantDatabase.Count > 0 ? plantDatabase[0] : null; //  �⺻��
    }


    // UI���� ����� �޼����
    public int GetCurrentQuestionIndex() => currentQuestionIndex;
    public int GetTotalQuestions() => questionsData != null ? questionsData.questions.Count : 0;
    public float GetProgress() => questionsData != null ? (float) currentQuestionIndex / questionsData.questions.Count : 0f;
    public UserPersonalityData GetUserData() => userData;
}
