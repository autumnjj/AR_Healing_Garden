using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TestUIManager : MonoBehaviour
{
    [Header("음성 인식 시스템")]
    public UnitySpeechRecognition speechRecognition;

    [Header("UI 요소들")]
    public Canvas mainCanvas;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI instructionText;

    [Header("음성 인식 패널")]
    public GameObject voicePanel;
    public Button startVoiceButton;
    public Button testButton;

    [Header("상태 표시")]
    public TextMeshProUGUI statusDisplay;
    public TextMeshProUGUI targetDisplay;
    public TextMeshProUGUI progressDisplay;
    public TextMeshProUGUI recognizedDisplay;

    [Header("식물 상태")]
    public TextMeshProUGUI plantStatusText;
    public Slider plantGrowthBar;

    [Header("테스트 버튼들")]
    public Button resetButton;
    public Button skipButton;

    private void Start()
    {
        SetupUI();
        InitalizeTest();
    }

    private void SetupUI()
    {
        // 제목 설정
        if (titleText != null)
            titleText.text = "음성 인식 테스트";

        if (instructionText != null)
            instructionText.text = "음성 인식 시스템을 테스트해보세요. " +
                "\n목표 문구를 3번 말하면 식물이 성장합니다.";

        // 버튼 이벤트 연결
        if (startVoiceButton != null)
            startVoiceButton.onClick.AddListener(StartVoiceTest);

        if (testButton != null)
            testButton.onClick.AddListener(TestSpeechRecognition);

        if (resetButton != null)
            resetButton.onClick.AddListener(ResetTest);
        if (skipButton != null)
            skipButton.onClick.AddListener(SkipCurrentTarget);

        // 초기 UI 상태
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
                    statusDisplay.text = "음성 인식 시스템을 찾을 수 없습니다.";
                return;
            }
        }
        // UI 연결
        ConnectSpeechRecognitionUI();

        if (statusDisplay != null)
            statusDisplay.text = "음성 인식 시스템 준비 완료";
    }

    private void ConnectSpeechRecognitionUI()
    {
        if (speechRecognition != null) return;

        // UnitySpeechRecognition의 UI 요소들을 이 UI로 연결
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
                statusDisplay.text = "음성 인식 테스트 시작...";

            UpdateUI();
        }
        else
        {
            if (statusDisplay != null)
                statusDisplay.text = "음성 인식 시스템이 준비되지 않았습니다.";
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
                statusDisplay.text = "테스트를 초기화했습니다.";
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
        // 식물 상태 업데이트 (PlantGrowthData가 있다면)
        UpdatePlantStatus();

        // 버튼 상태 업데이트
        if (speechRecognition != null)
        {
            if (testButton != null)
            {
                var buttonText = testButton.GetComponent<TextMeshProUGUI>();
                if (buttonText != null)
                    buttonText.text = "음성 인식 시작/중지";
            }
        }
    }

    private void UpdatePlantStatus()
    {
        // PlantGrowthData가 연결되어 있다면 상태 표시
        if (speechRecognition != null && speechRecognition.plantGrowthData != null)
        {
            var plantData = speechRecognition.plantGrowthData;
            var currenState = plantData.GetCurrentState();

            if (plantStatusText != null && currenState != null)
            {
                string stageName = GetStageKoreanName(currenState.currentStage);
                plantStatusText.text = $"식물 상태: {stageName}\n성장 포인트: {currenState.currentGrowthPoints:F1}";

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
                plantStatusText.text = "식물 시스템 연결 안됨";
        }
    }

    private string GetStageKoreanName(PlantGrowthStage stage)
    {
        switch (stage)
        {
            case PlantGrowthStage.Seed: return "씨앗";
            case PlantGrowthStage.Sprout: return "새싹";
            case PlantGrowthStage.Growing: return "성장";
            case PlantGrowthStage.Blooming: return "개화";
            default: return "알 수 없음";
        }
    }

    private void Update()
    {
        if (Time.time % 1f < Time.deltaTime)
            UpdatePlantStatus();
    }

    // 수동 테스트용 - 긍정적 문구 직접 입력
    public void TestPositivePhrase(string phrase)
    {
        if (speechRecognition != null)
        {
            speechRecognition.OnSpeechRecognitionResult(phrase);
        }
    }

    // Inspector에서 호출할 수 있는 테스트 메서드들
    [ContextMenu("테스트: '나는 소중해'")]
    public void Test1() { TestPositivePhrase("나는 소중해"); }

    [ContextMenu("테스트: '잘하고 있어'")]
    public void Test2() { TestPositivePhrase("잘하고 있어"); }

    [ContextMenu("테스트: '고마워'")]
    public void Test3() { TestPositivePhrase("고마워"); }
}
