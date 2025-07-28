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

    [Header("진행률 표시 (선택사항)")]
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
        // MBTIManager가 완전히 초기화될 때까지 대기
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

        // 이벤트 연결
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

        // 초기 상태 설정
        ShowTestPanel();

        isInitialized = true;

        // MBTIManager의 상태 확인
        CheckInitialState();
    }

    private void CheckInitialState()
    {
        if (mbtiManager == null) return;

        // MBTIManager가 이미 데이터를 가지고 있는지 확인
        var userData = mbtiManager.GetUserData();
        if (userData != null && userData.HasData())
        {
            // 이미 완료된 테스트가 있다면 결과를 표시
            Debug.Log("There is finished test result.");
        }
        else
        {
            // 새 테스트 시작 - 첫 번째 질문이 자동으로 표시되어야 함
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

        // 첫번째 질문이 표시된 후 Progress 업데이트
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

        // 질문 표시 후 바로 Progress 업데이트
        UpdateProgress();
        
        Debug.Log($"Question displayed : {question.questionText}");
    }

    private void UpdateProgress()
    {
        if (mbtiManager == null || !isInitialized) return;

        int currentIndex = mbtiManager.GetCurrentQuestionIndex();
        int totalQuestions = 4;

        // 진행률 슬라이더 업데이트
        if (progressSlider != null)
            progressSlider.value = (float)currentIndex / totalQuestions;

        // 진행률 텍스트 업데이트
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
