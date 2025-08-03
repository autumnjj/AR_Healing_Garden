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
    private bool isInitialized = false;

    private void Start()
    {
        InitializeImmediately();
    }

    private void InitializeImmediately()
    {
        Debug.Log("MBTITestUI Initialize start");

        // 1. MBTIManager 찾기
        mbtiManager = FindAnyObjectByType<MBTIManager>();

        if (mbtiManager == null)
        {
            Debug.LogError("Cannot find MBTIManager in scene!");
            return;
        }
        Debug.Log($"MBTIManager found : {mbtiManager.gameObject.name}");

        // 2. 기존 방식으로 모든 설정을 한 번에
        SetupMBTIManager();

        // 3. 코루틴으로 안전한 초기화
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

        // 초기 상태 설정
        ShowTestPanel();

        Debug.Log("UI setup complete");
    }


    private IEnumerator SafeInitialization()
    { 
        Debug.Log("Safe initialization started");

        // 한 프레임 대기
        yield return null;

        // MBTIManager가 완전히 초기화될 때까지 대기
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

        // 사용자 데이터 확인
        var userData = mbtiManager.GetUserData();

        if (userData == null || !userData.HasData())
        {
            Debug.Log("Found existing test results");
            // 이미 완료된 테스트가 있다면 결과 표시는 MBTIManager에서 자동으로 처리됨
        }
        else
        {
            Debug.Log("No existing data, starting new test");

            // 새 테스트 시작
            mbtiManager.StartTest();

            // 잠시 대기 후 첫 번째 질문이 표시되었는지 확인
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

        // 질문 표시 후 바로 Progress 업데이트
        UpdateProgress();
        
        Debug.Log($"Question displayed : {question.questionText}");
    }

    private void UpdateProgress()
    {
        if (mbtiManager == null || !isInitialized) return;

        int currentIndex = mbtiManager.GetCurrentQuestionIndex();
        int totalQuestions = mbtiManager.GetTotalQuestions();

        // 진행률 슬라이더 업데이트
        if (progressSlider != null)
        {
            float progress = totalQuestions > 0 ? (float)currentIndex / totalQuestions : 0f;
            progressSlider.value = progress;
        }
            

        // 진행률 텍스트 업데이트
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
        if (resultMBTIText != null )
            resultMBTIText.text = $"당신의 MBTI : {userData.mbtiType}";

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
        // 전환 메시지 표시
        if(resultDescriptionText != null)
        {
            resultDescriptionText.text = "식물 친구와 함께 성장해보세요!\n잠시 후 이동합니다...";
        }

        yield return new WaitForSeconds(2f); 

        // 씬 전환
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
        // 이벤트 연결 해제
        if(mbtiManager != null)
        {
            mbtiManager.OnQuestionChanged -= DisplayQuestion;
            mbtiManager.OnTestComplete -= OnTestComplete;
        }
    }
}
