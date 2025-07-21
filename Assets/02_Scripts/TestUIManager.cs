using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TestUIManager : MonoBehaviour
{
    [Header("���� �ν� �ý���")]
    public UnitySpeechRecognition speechRecognition;

    [Header("UI ��ҵ�")]
    public Canvas mainCanvas;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI instructionText;

    [Header("���� �ν� �г�")]
    public GameObject voicePanel;
    public Button startVoiceButton;
    public Button testButton;

    [Header("���� ǥ��")]
    public TextMeshProUGUI statusDisplay;
    public TextMeshProUGUI targetDisplay;
    public TextMeshProUGUI progressDisplay;
    public TextMeshProUGUI recognizedDisplay;

    [Header("�Ĺ� ����")]
    public TextMeshProUGUI plantStatusText;
    public Slider plantGrowthBar;

    [Header("�׽�Ʈ ��ư��")]
    public Button resetButton;
    public Button skipButton;

    private void Start()
    {
        SetupUI();
        InitalizeTest();
    }

    private void SetupUI()
    {
        // ���� ����
        if (titleText != null)
            titleText.text = "���� �ν� �׽�Ʈ";

        if (instructionText != null)
            instructionText.text = "���� �ν� �ý����� �׽�Ʈ�غ�����. " +
                "\n��ǥ ������ 3�� ���ϸ� �Ĺ��� �����մϴ�.";

        // ��ư �̺�Ʈ ����
        if (startVoiceButton != null)
            startVoiceButton.onClick.AddListener(StartVoiceTest);

        if (testButton != null)
            testButton.onClick.AddListener(TestSpeechRecognition);

        if (resetButton != null)
            resetButton.onClick.AddListener(ResetTest);
        if (skipButton != null)
            skipButton.onClick.AddListener(SkipCurrentTarget);

        // �ʱ� UI ����
        if (voicePanel != null)
            voicePanel.SetActive(true);

        UpdateUI();
    }

    private void InitalizeTest()
    {
        if (speechRecognition == null)
        {
            speechRecognition = FindAnyObjectByType<UnitySpeechRecognition>();

            if (speechRecognition == null)
            {
                Debug.LogError("Cannot find UnitySpeechRecognition");
                if (statusDisplay != null)
                    statusDisplay.text = "���� �ν� �ý����� ã�� �� �����ϴ�.";
                return;
            }
        }
        // UI ����
        ConnectSpeechRecognitionUI();

        if (statusDisplay != null)
            statusDisplay.text = "���� �ν� �ý��� �غ� �Ϸ�";
    }

    private void ConnectSpeechRecognitionUI()
    {
        if (speechRecognition != null) return;

        // UnitySpeechRecognition�� UI ��ҵ��� �� UI�� ����
        speechRecognition.statusText = statusDisplay;
        speechRecognition.targetText = targetDisplay;
        speechRecognition.progressText = progressDisplay;
        speechRecognition.recognizedText = recognizedDisplay;

        Debug.Log("Voice recognition UI connect done");
    }

    public void StartVoiceTest()
    {
        if (speechRecognition != null)
        {
            if (statusDisplay != null)
                statusDisplay.text = "���� �ν� �׽�Ʈ ����...";

            UpdateUI();
        }
        else
        {
            if (statusDisplay != null)
                statusDisplay.text = "���� �ν� �ý����� �غ���� �ʾҽ��ϴ�.";
        }
    }

    public void TestSpeechRecognition()
    {
        if (speechRecognition != null)
            speechRecognition.ToggleListening();
    }

    public void ResetTest()
    {
        if (speechRecognition != null)
        {
            if (statusDisplay != null)
                statusDisplay.text = "�׽�Ʈ�� �ʱ�ȭ�߽��ϴ�.";
        }
        UpdateUI();
    }

    public void SkipCurrentTarget()
    {
        if (speechRecognition != null)
            speechRecognition.ManualComplete();
    }

    private void UpdateUI()
    {
        // �Ĺ� ���� ������Ʈ (PlantGrowthData�� �ִٸ�)
        UpdatePlantStatus();

        // ��ư ���� ������Ʈ
        if (speechRecognition != null)
        {
            if (testButton != null)
            {
                var buttonText = testButton.GetComponent<TextMeshProUGUI>();
                if (buttonText != null)
                    buttonText.text = "���� �ν� ����/����";
            }
        }
    }

    private void UpdatePlantStatus()
    {
        // PlantGrowthData�� ����Ǿ� �ִٸ� ���� ǥ��
        if (speechRecognition != null && speechRecognition.plantGrowthData != null)
        {
            var plantData = speechRecognition.plantGrowthData;
            var currenState = plantData.GetCurrentState();

            if (plantStatusText != null && currenState != null)
            {
                string stageName = GetStageKoreanName(currenState.currentStage);
                plantStatusText.text = $"�Ĺ� ����: {stageName}\n���� ����Ʈ: {currenState.currentGrowthPoints:F1}";

            }

            if (plantGrowthBar != null && currenState != null)
            {
                float maxPoints = plantData.growthSettings.maxGrowthPoints;
                plantGrowthBar.value = currenState.currentGrowthPoints / maxPoints;
            }
        }
        else
        {
            if (plantStatusText != null)
                plantStatusText.text = "�Ĺ� �ý��� ���� �ȵ�";
        }
    }

    private string GetStageKoreanName(PlantGrowthStage stage)
    {
        switch (stage)
        {
            case PlantGrowthStage.Seed: return "����";
            case PlantGrowthStage.Sprout: return "����";
            case PlantGrowthStage.Growing: return "����";
            case PlantGrowthStage.Blooming: return "��ȭ";
            default: return "�� �� ����";
        }
    }

    private void Update()
    {
        if (Time.time % 1f < Time.deltaTime)
            UpdatePlantStatus();
    }

    // ���� �׽�Ʈ�� - ������ ���� ���� �Է�
    public void TestPositivePhrase(string phrase)
    {
        if (speechRecognition != null)
        {
            speechRecognition.OnSpeechRecognitionResult(phrase);
        }
    }

    // Inspector���� ȣ���� �� �ִ� �׽�Ʈ �޼����
    [ContextMenu("�׽�Ʈ: '���� ������'")]
    public void Test1() { TestPositivePhrase("���� ������"); }

    [ContextMenu("�׽�Ʈ: '���ϰ� �־�'")]
    public void Test2() { TestPositivePhrase("���ϰ� �־�"); }

    [ContextMenu("�׽�Ʈ: '����'")]
    public void Test3() { TestPositivePhrase("����"); }
}
