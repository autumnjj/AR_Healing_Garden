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

    private void Start()
    {
        Debug.Log("=== MBTITestUI Start ===");

        // MBTIManager ã��
        mbtiManager = FindAnyObjectByType<MBTIManager>();
        if (mbtiManager == null)
        {
            Debug.LogError("Cannot find MBTIManager!");
            return;
        }

        // UI ����
        SetupUI();

        // �̺�Ʈ ����
        ConnectEvents();

        Debug.Log("MBTITestUI initialization complete");
    }

    private void SetupUI()
    {
        // �г� �ʱ� ���� ����
        ShowTestPanel();

        // ��ư ����
        if (optionAButton != null)
        {
            optionAButton.onClick.RemoveAllListeners();
            optionAButton.onClick.AddListener(() => {
                mbtiManager.AnswerQuestion(0);
                UpdateProgress();
            });
        }

        if (optionBButton != null)
        {
            optionBButton.onClick.RemoveAllListeners();
            optionBButton.onClick.AddListener(() => {
                mbtiManager.AnswerQuestion(1);
                UpdateProgress();
            });
        }

        if (goToARButton != null)
        {
            goToARButton.onClick.RemoveAllListeners();
            goToARButton.onClick.AddListener(GoToARScene);
        }

        UpdateProgress();
        Debug.Log("UI setup complete");
    }

    private void ConnectEvents()
    {
        if (mbtiManager != null)
        {
            mbtiManager.OnQuestionChanged += DisplayQuestion;
            mbtiManager.OnTestComplete += OnTestComplete;
        }
    }

    private void DisplayQuestion(MBTIQuestionsSO.Question question)
    {
        if (question == null)
        {
            Debug.LogError("Question is null");
            return;
        }

        Debug.Log($"Displaying question: {question.questionText}");

        if (questionText != null) questionText.text = question.questionText;
        if (optionAText != null) optionAText.text = question.optionA;
        if (optionBText != null) optionBText.text = question.optionB;

        // �׽�Ʈ �г��� ���̴��� Ȯ��
        ShowTestPanel();

        // ���� ������Ʈ
        UpdateProgress();
    }

    private void UpdateProgress()
    {
        if (mbtiManager == null) return;

        int currentIndex = mbtiManager.GetCurrentQuestionIndex();
        int totalQuestions = mbtiManager.GetTotalQuestions();

        // ������ �����̴� ������Ʈ
        if (progressSlider != null)
        {
            float progress = totalQuestions > 0 ? (float)currentIndex / totalQuestions : 0f;
            progressSlider.value = progress;
        }

        // ������ �ؽ�Ʈ ������Ʈ
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
        if (resultMBTIText != null)
            resultMBTIText.text = $"����� MBTI : {userData.mbtiType}";

        if (resultPlantNameText != null)
            resultPlantNameText.text = matchedPlant.koreanName;

        if (resultPlantImage != null && matchedPlant.plantImage != null)
        {
            resultPlantImage.sprite = matchedPlant.plantImage;
        }

        if (resultDescriptionText != null)
            resultDescriptionText.text = matchedPlant.symolism;

        Debug.Log($"Test Complete! {userData.mbtiType} - {matchedPlant.koreanName}");
    }

    public void GoToARScene()
    {
        StartCoroutine(TransitionToGrowthScene());
    }

    private IEnumerator TransitionToGrowthScene()
    {
        // ��ȯ �޽��� ǥ��
        if (resultDescriptionText != null)
        {
            resultDescriptionText.text = "�Ĺ� ü��� �Բ� �����غ�����!\n��� �� �̵��մϴ�...";
        }

        yield return new WaitForSeconds(2f);

        // AR ������ ��ȯ
        SceneManager.LoadScene("ARScene");
    }

    private void ShowTestPanel()
    {
        if (testPanel != null) testPanel.SetActive(true);
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
        if (mbtiManager != null)
        {
            mbtiManager.OnQuestionChanged -= DisplayQuestion;
            mbtiManager.OnTestComplete -= OnTestComplete;
        }
    }
}
