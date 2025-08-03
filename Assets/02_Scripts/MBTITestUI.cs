using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class MBTITestUI : MonoBehaviour
{
    [Header("UI ��ҵ�")]
    public TextMeshProUGUI questionText;
    public TextMeshProUGUI optionAText;
    public TextMeshProUGUI optionBText;
    public Button optionAButton;
    public Button optionBButton;

    [Header("��� UI")]
    public GameObject testPanel;
    public GameObject resultPanel;
    public TextMeshProUGUI resultMBTIText;
    public TextMeshProUGUI resultPlantNameText;
    public Image resultPlantImage;
    public TextMeshProUGUI resultDescriptionText;
    public Button goToARButton;

    [Header("����� ǥ��")]
    public Slider progressSlider;
    public TextMeshProUGUI progressText;

    private MBTIManager mbtiManager;
    private bool isInitialized = false;

    private void Start()
    {
        InitializeImmediately();
    }

    private void InitializeImmediately()
    {
        Debug.Log("MBTITestUI Initialize start");

        // 1. MBTIManager ã��
        mbtiManager = FindAnyObjectByType<MBTIManager>();

        if (mbtiManager == null)
        {
            Debug.LogError("Cannot find MBTIManager in scene!");
            return;
        }
        Debug.Log($"MBTIManager found : {mbtiManager.gameObject.name}");

        // 2. ���� ������� ��� ������ �� ����
        SetupMBTIManager();

        // 3. �ڷ�ƾ���� ������ �ʱ�ȭ
        StartCoroutine(SafeInitialization());

    }

    private void SetupMBTIManager()
    {
        if (mbtiManager == null)
        {
            Debug.LogError("MBTIManager is null.");
            return;
        }

        mbtiManager.OnQuestionChanged += DisplayQuestion;
        mbtiManager.OnTestComplete += OnTestComplete;

        if (optionAButton != null)
        {
            optionAButton.onClick.AddListener(() =>
            {
                mbtiManager.AnswerQuestion(0);
                UpdateProgress();
            });
        }

        if (optionBButton != null)
        {
            optionBButton.onClick.AddListener(() =>
            { 
                mbtiManager.AnswerQuestion(1);
                UpdateProgress();
            });
        }

        if (goToARButton != null)
        {
            goToARButton.onClick.AddListener(GoToARScene);
        }

        // �ʱ� ���� ����
        ShowTestPanel();

        Debug.Log("UI setup complete");
    }


    private IEnumerator SafeInitialization()
    { 
        Debug.Log("Safe initialization started");

        // �� ������ ���
        yield return null;

        // MBTIManager�� ������ �ʱ�ȭ�� ������ ���
        int waitCount = 0;
        while(mbtiManager.GetTotalQuestions() == 0 && waitCount < 30)
        {
            Debug.Log($"Waiting for MBTIManager to initialize...{waitCount}");
            yield return new WaitForSeconds(0.1f);
            waitCount++;
        }

        if (waitCount >= 30)
        {
            Debug.LogError("MBTIManager initialization timed out!");
            yield break;
        }

        Debug.Log("MBTIManager is ready");

        // ����� ������ Ȯ��
        var userData = mbtiManager.GetUserData();

        if (userData == null || !userData.HasData())
        {
            Debug.Log("Found existing test results");
            // �̹� �Ϸ�� �׽�Ʈ�� �ִٸ� ��� ǥ�ô� MBTIManager���� �ڵ����� ó����
        }
        else
        {
            Debug.Log("No existing data, starting new test");

            // �� �׽�Ʈ ����
            mbtiManager.StartTest();

            // ��� ��� �� ù ��° ������ ǥ�õǾ����� Ȯ��
            yield return new WaitForSeconds(0.2f);

            if (questionText != null && string.IsNullOrEmpty(questionText.text))
            {
                Debug.LogWarning("First question text is empty, displaying first question manually");
                ForceDisplayFirstQuestion();
            }
        }
        isInitialized = true;
        UpdateProgress();

        Debug.Log("Initialization Complete");
    }

    private void ForceDisplayFirstQuestion()
    {
        if (mbtiManager == null) return;

        var totalQuestions = mbtiManager.GetTotalQuestions();
        Debug.Log($"Force displaying first question. Total questions: {totalQuestions}");

        if (totalQuestions > 0)
        {
            mbtiManager.StartTest();
        }
    }

    private void DisplayQuestion(MBTIQuestionsSO.Question question)
    {
        if ( question == null)
        {
            Debug.LogError("Question is null");
            return;
        }
        if (questionText != null) questionText.text = question.questionText;
        if (optionAText != null) optionAText.text = question.optionA;
        if (optionBText != null) optionBText.text = question.optionB;

        // ���� ǥ�� �� �ٷ� Progress ������Ʈ
        UpdateProgress();
        
        Debug.Log($"Question displayed : {question.questionText}");
    }

    private void UpdateProgress()
    {
        if (mbtiManager == null || !isInitialized) return;

        int currentIndex = mbtiManager.GetCurrentQuestionIndex();
        int totalQuestions = mbtiManager.GetTotalQuestions();

        // ����� �����̴� ������Ʈ
        if (progressSlider != null)
        {
            float progress = totalQuestions > 0 ? (float)currentIndex / totalQuestions : 0f;
            progressSlider.value = progress;
        }
            

        // ����� �ؽ�Ʈ ������Ʈ
        if (progressText != null)
        {
            int displayIndex = Mathf.Min(currentIndex + 1, totalQuestions);
            progressText.text = $"{displayIndex} / {totalQuestions}";
        }
            
    }

    private void OnTestComplete(UserPersonalityData userData, PlantDataSO matchedPlant)
    {
        Debug.Log("OnTestComplete called!");

        if (matchedPlant == null)
        {
            Debug.LogError("matchedPlant is null");
            return;
        }

        ShowResultPanel();

        // ��� ǥ��
        if (resultMBTIText != null )
            resultMBTIText.text = $"����� MBTI : {userData.mbtiType}";

        if (resultPlantNameText != null ) 
            resultPlantNameText.text = matchedPlant.koreanName;

        if(resultPlantImage != null && matchedPlant.plantImage != null)
        {
            resultPlantImage.sprite = matchedPlant.plantImage;
        }

        if (resultDescriptionText != null )
            resultDescriptionText.text = matchedPlant.symolism;

        Debug.Log($"Test Complete! {userData.mbtiType} - {matchedPlant.koreanName}");

    }

    public void GoToARScene()
    {
        StartCoroutine(TransitionToGrowthScene());
    }

    IEnumerator TransitionToGrowthScene()
    {
        // ��ȯ �޽��� ǥ��
        if(resultDescriptionText != null)
        {
            resultDescriptionText.text = "�Ĺ� ģ���� �Բ� �����غ�����!\n��� �� �̵��մϴ�...";
        }

        yield return new WaitForSeconds(2f); 

        // �� ��ȯ
        SceneManager.LoadScene(2);
    }

    private void ShowTestPanel()
    {
        if (testPanel != null ) testPanel.SetActive(true);
        if (resultPanel != null) resultPanel.SetActive(false);
    }

    private void ShowResultPanel()
    {
        if (testPanel != null) testPanel.SetActive(false);
        if (resultPanel != null) resultPanel.SetActive(true);
    }

    private void OnDestroy()
    {
        // �̺�Ʈ ���� ����
        if(mbtiManager != null)
        {
            mbtiManager.OnQuestionChanged -= DisplayQuestion;
            mbtiManager.OnTestComplete -= OnTestComplete;
        }
    }
}
