using UnityEngine;
using System.Collections.Generic;

public class MBTIManager : MonoBehaviour
{
    [Header("������ Assets")]
    public MBTIQuestionsSO questionsData;
    public List<PlantDataSO> plantDatabase = new List<PlantDataSO>();

    [Header("���� ����")]
    private Dictionary<string, float> currentScores;
    private int currentQuestionIndex = 0;
    private UserPersonalityData userData;

    // �̺�Ʈ��
    public System.Action<MBTIQuestionsSO.Question> OnQuestionChanged;
    public System.Action<UserPersonalityData, PlantDataSO> OnTestComplete;

    private void Start()
    {
        InitializeScores();
        LoadUserData();

        // �̹� �׽�Ʋ�� �Ϸ��ߴٸ� �ٷ� ��� ǥ��
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

    public void AnswerQuestion(int optionIndex) // 0 : A������, 1: B������
    {
        if (currentQuestionIndex >= questionsData.questions.Count) return;


        var question = questionsData.questions[currentQuestionIndex];
        float scoreValue = (optionIndex == 0) ? question.optionAValue : question.optionBValue;

        Debug.Log($"���� {currentQuestionIndex} : {question.questionText}");
        Debug.Log($"���� : {optionIndex}, ���� : {question.dimension}, ���� : {scoreValue}");

        // Dictionary Ű Ȯ��
        if(currentScores == null)
        {
            Debug.LogError("currentScores�� null�Դϴ�!");
            InitializeScores();
        }

        // ���� �߰�
        if (question.dimension == "Mixed_TF")
        {
            // ���� ���� ó��
            if (currentScores.ContainsKey("TF")) currentScores["TF"] += scoreValue * 0.5f;
        }
        else if (question.dimension == "Mixed_SN")
        {
            if (currentScores.ContainsKey("SN")) currentScores["SN"] += scoreValue * 0.5f;
        }
        else // â����
        {
            if (currentScores.ContainsKey(question.dimension))
            {
                currentScores[question.dimension] += scoreValue;
            }
            else
            {
                Debug.LogError($"Ű '{question.dimension}'�� Dictionary�� �����ϴ�!");
                Debug.LogError($"���� Ű�� : {string.Join(", ", currentScores.Keys)}");
                return;
            }
        }

        currentQuestionIndex++;

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

        // ����� ������ ����
        userData.mbtiType = mbtiType;
        userData.EI_score = currentScores["EI"];
        userData.SN_score = currentScores["SN"];
        userData.TF_score = currentScores["TF"];
        userData.JP_score = currentScores["JP"];

        // �Ĺ� ��Ī
        PlantDataSO matchedPlant = FindBestMatchingPlant(mbtiType);
        userData.matchedPlantId = matchedPlant.plantId;

        // PlayerPrefs�� ����
        userData.SaveToPlayerPrefs();

        // ��� ����
        OnTestComplete?.Invoke(userData, matchedPlant);

        Debug.Log($"MBTI ��� : {mbtiType}, ��Ī�� �Ĺ� : {matchedPlant.koreanName}");
    }

    PlantDataSO FindBestMatchingPlant(string mbtiType)
    {
        // 1���� : ���� ��Ī
        foreach(var plant in plantDatabase)
        {
            if (plant.primaryMBTI.Contains(mbtiType))
            {
                return plant;
            }
        }

        // 2���� : ���� ��Ī
        foreach(var plant in plantDatabase)
        {
            if (plant.secondaryMBTI.Contains(mbtiType))
            {
                return plant;
            }
        }

        // 3���� : ���絵 ��Ī
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
        return plantDatabase[0]; //  �⺻��
    }


    // UI���� ����� �޼����
    public int GetCurrentQuestionIndex() => currentQuestionIndex;
    public int GetTotalQuestions() => questionsData.questions.Count;
    public float GetProgress() => (float) currentQuestionIndex / questionsData.questions.Count;
    public UserPersonalityData GetUserData() => userData;
}
