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
    public Button restartButton;

    private MBTIManager mbtiManager;

    private void Start()
    {
        mbtiManager = FindAnyObjectByType<MBTIManager>();

        if(mbtiManager == null)
        {
            Debug.LogError("MBTIManager�� ã�� �� �����ϴ�.");
            return;
        }

        // �̺�Ʈ ����
        mbtiManager.OnQuestionChanged += DisplayQuestion;
        mbtiManager.OnTestComplete += OnTestComplete;

        // ��ư �̺�Ʈ ����
        optionAButton.onClick.AddListener(() => mbtiManager.AnswerQuestion(0));
        optionBButton.onClick.AddListener(() => mbtiManager.AnswerQuestion(1));
        restartButton.onClick.AddListener(() => mbtiManager.RestartTest());

        // �ʱ� ���� ����
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

        // ��� ǥ��
        resultMBTIText.text = $"����� ���� ���� : {userData.mbtiType}";
        resultPlantNameText.text = matchedPlant.koreanName;
        if(matchedPlant.plantImage != null)
        {
            resultPlantImage.sprite = matchedPlant.plantImage;
        }
        resultDescriptionText.text = matchedPlant.symolism;

        Debug.Log($"�׽�Ʈ �Ϸ�! {userData.mbtiType} - {matchedPlant.koreanName}");

        StartCoroutine(TransitionToGrowthScene());
    }

    IEnumerator TransitionToGrowthScene()
    {
        // ��ȯ �޽��� ǥ��
        if(resultDescriptionText != null)
        {
            resultDescriptionText.text = "�Ĺ� ģ���� �Բ� �����غ�����!\n��� �� �̵��մϴ�...";
        }

        yield return new WaitForSeconds(3f); 

        // �� ��ȯ
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
        // �̺�Ʈ ���� ����
        if(mbtiManager != null)
        {
            mbtiManager.OnQuestionChanged -= DisplayQuestion;
            mbtiManager.OnTestComplete -= OnTestComplete;
        }
    }
}
