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

    [Header("����� ǥ�� (���û���)")]
    public Slider progressSlider;
    public TextMeshProUGUI progressText;

    private MBTIManager mbtiManager;
    private bool isInitialized = false;

    private void Start()
    {
        StartCoroutine(InitializeWithDelay());
    }

    private IEnumerator InitializeWithDelay()
    {
        // MBTIManager�� ������ �ʱ�ȭ�� ������ ���
        yield return new WaitForEndOfFrame();

        mbtiManager = FindAnyObjectByType<MBTIManager>();

        if (mbtiManager == null)
        {
            Debug.LogError("Cannot find MBTIManager in scene!");

            yield break;
        }
        else
        {
            Debug.Log($"MBTIManager found on GameObject: {mbtiManager.gameObject.name}");
            SetupMBTIManager();
        }

    }

    private void SetupMBTIManager()
    {
        if (mbtiManager == null)
        {
            Debug.LogError("MBTIManager is null in SetupMBTIManager!");
            return;
        }

        // �̺�Ʈ ����
        mbtiManager.OnQuestionChanged += DisplayQuestion;
        mbtiManager.OnTestComplete += OnTestComplete;

        if (optionAButton != null)
        {
            optionAButton.onClick.AddListener(() => { mbtiManager.AnswerQuestion(0); UpdateProgress(); });
        }

        if (optionBButton != null)
        {
            optionBButton.onClick.AddListener(() => { mbtiManager.AnswerQuestion(1); UpdateProgress(); });
        }

        if (goToARButton != null)
            goToARButton.onClick.AddListener(GoToARScene);

        // �ʱ� ���� ����
        ShowTestPanel();

        isInitialized = true;

        // MBTIManager�� ���� Ȯ��
        CheckInitialState();
    }

    private void CheckInitialState()
    {
        if (mbtiManager == null) return;

        // MBTIManager�� �̹� �����͸� ������ �ִ��� Ȯ��
        var userData = mbtiManager.GetUserData();
        if (userData != null && userData.HasData())
        {
            // �̹� �Ϸ�� �׽�Ʈ�� �ִٸ� ����� ǥ��
            Debug.Log("There is finished test result.");
        }
        else
        {
            // �� �׽�Ʈ ���� - ù ��° ������ �ڵ����� ǥ�õǾ�� ��
            Debug.Log("New MBTI Test Start");

            StartCoroutine(EnsureTestStart());
        }
    }

    private IEnumerator EnsureTestStart()
    {
        yield return new WaitForSeconds(0.1f);

        if (questionText != null && string.IsNullOrEmpty(questionText.text))
        {
            Debug.Log($"No show for first question, Start Test auto");
            mbtiManager.StartTest();
        }

        // ù��° ������ ǥ�õ� �� Progress ������Ʈ
        yield return new WaitForSeconds(0.1f);
        UpdateProgress();
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
        int totalQuestions = 4;

        // ����� �����̴� ������Ʈ
        if (progressSlider != null)
            progressSlider.value = (float)currentIndex / totalQuestions;

        // ����� �ؽ�Ʈ ������Ʈ
        if (progressText != null)
            progressText.text = $"{currentIndex + 1} / {totalQuestions}";
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
