using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class MBTITestUI : MonoBehaviour
{
    [Header("UI 요소들")]
    public TextMeshProUGUI questionText;
    public TextMeshProUGUI optionAText;
    public TextMeshProUGUI optionBText;
    public Button optionAButton;
    public Button optionBButton;

    [Header("결과 UI")]
    public GameObject testPanel;
    public GameObject resultPanel;
    public TextMeshProUGUI resultMBTIText;
    public TextMeshProUGUI resultPlantNameText;
    public Image resultPlantImage;
    public TextMeshProUGUI resultDescriptionText;
    public Button goToARButton;

    [Header("진행률 표시")]
    public Slider progressSlider;
    public TextMeshProUGUI progressText;

    private MBTIManager mbtiManager;

    private void Start()
    {
        Debug.Log("=== MBTITestUI Start ===");

        // MBTIManager 찾기
        mbtiManager = FindAnyObjectByType<MBTIManager>();
        if (mbtiManager == null)
        {
            Debug.LogError("Cannot find MBTIManager!");
            return;
        }

        // UI 설정
        SetupUI();

        // 이벤트 연결
        ConnectEvents();

        Debug.Log("MBTITestUI initialization complete");
    }

    private void SetupUI()
    {
        // 패널 초기 상태 설정
        ShowTestPanel();

        // 버튼 설정
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

        // 테스트 패널이 보이는지 확인
        ShowTestPanel();

        // 진도 업데이트
        UpdateProgress();
    }

    private void UpdateProgress()
    {
        if (mbtiManager == null) return;

        int currentIndex = mbtiManager.GetCurrentQuestionIndex();
        int totalQuestions = mbtiManager.GetTotalQuestions();

        // 진도률 슬라이더 업데이트
        if (progressSlider != null)
        {
            float progress = totalQuestions > 0 ? (float)currentIndex / totalQuestions : 0f;
            progressSlider.value = progress;
        }

        // 진도률 텍스트 업데이트
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

        // 결과 표시
        if (resultMBTIText != null)
            resultMBTIText.text = $"당신의 MBTI : {userData.mbtiType}";

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
        // 전환 메시지 표시
        if (resultDescriptionText != null)
        {
            resultDescriptionText.text = "식물 체험과 함께 성장해보세요!\n잠시 후 이동합니다...";
        }

        yield return new WaitForSeconds(2f);

        // AR 씬으로 전환
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
        // 이벤트 연결 해제
        if (mbtiManager != null)
        {
            mbtiManager.OnQuestionChanged -= DisplayQuestion;
            mbtiManager.OnTestComplete -= OnTestComplete;
        }
    }
}
