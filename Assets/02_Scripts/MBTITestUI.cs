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
    public Button restartButton;

    private MBTIManager mbtiManager;

    private void Start()
    {
        mbtiManager = FindAnyObjectByType<MBTIManager>();

        if(mbtiManager == null)
        {
            Debug.LogError("MBTIManager를 찾을 수 없습니다.");
            return;
        }

        // 이벤트 연결
        mbtiManager.OnQuestionChanged += DisplayQuestion;
        mbtiManager.OnTestComplete += OnTestComplete;

        // 버튼 이벤트 연결
        optionAButton.onClick.AddListener(() => mbtiManager.AnswerQuestion(0));
        optionBButton.onClick.AddListener(() => mbtiManager.AnswerQuestion(1));
        restartButton.onClick.AddListener(() => mbtiManager.RestartTest());

        // 초기 상태 설정
        ShowTestPanel();
    }

    private void DisplayQuestion(MBTIQuestionsSO.Question question)
    {
        questionText.text = question.questionText;
        optionAText.text = question.optionA;
        optionBText.text = question.optionB;
    }

    private void OnTestComplete(UserPersonalityData userData, PlantDataSO matchedPlant)
    {
        ShowResultPanel();

        // 결과 표시
        resultMBTIText.text = $"당신의 성격 유형 : {userData.mbtiType}";
        resultPlantNameText.text = matchedPlant.koreanName;
        if(matchedPlant.plantImage != null)
        {
            resultPlantImage.sprite = matchedPlant.plantImage;
        }
        resultDescriptionText.text = matchedPlant.symolism;

        Debug.Log($"테스트 완료! {userData.mbtiType} - {matchedPlant.koreanName}");

        StartCoroutine(TransitionToGrowthScene());
    }

    IEnumerator TransitionToGrowthScene()
    {
        // 전환 메시지 표시
        if(resultDescriptionText != null)
        {
            resultDescriptionText.text = "식물 친구와 함께 성장해보세요!\n잠시 후 이동합니다...";
        }

        yield return new WaitForSeconds(3f); 

        // 씬 전환
        SceneManager.LoadScene(1);
    }

    private void ShowTestPanel()
    {
        testPanel.SetActive(true);
        resultPanel.SetActive(false);
    }

    private void ShowResultPanel()
    {
        testPanel.SetActive(false);
        resultPanel.SetActive(true);
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
