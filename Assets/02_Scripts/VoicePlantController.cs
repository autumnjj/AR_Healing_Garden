using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class VoicePlantController : MonoBehaviour
{
    [Header("핵심 컴포넌트")]
    public UnitySpeechRecognition voiceRecognition;
    public PlantGrowthManager plantGrowthManager;

    [Header("UI")]
    public TextMeshProUGUI welcomeText;
    public TextMeshProUGUI plantStatusText;
    public Button startButton;
    public GameObject voicePanel;
    public GameObject mainPanel;

    private void Start()
    {
        SetupUI();
        ShowWelcomeMessage();
    }

    private void SetupUI()
    {
        if (startButton != null)
            startButton.onClick.AddListener(StartVoiceActivity);

        // 초기 상태 설정
        if(voicePanel != null) voicePanel.SetActive(false);
        if(mainPanel != null) mainPanel.SetActive(true);
    }

    private void ShowWelcomeMessage()
    {
        if (welcomeText != null)
            welcomeText.text = "AR 힐링 가든 \n긍정적인 말로 식물을 키워보세요!";

        UpdatePlantStatus();
    }

    private void UpdatePlantStatus()
    {
        if(plantGrowthManager != null && plantStatusText != null)
        {
            var plantState = plantGrowthManager.GetCurrentPlantState();
            if (plantState != null)
            {
                string stageName = GetStageKoreanName(plantState.currentStage);
                plantStatusText.text = $"현재 식물 : {stageName} 단계 \n성장 포인트 : {plantState.currentGrowthPoints:F0}";

            } 
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

    public void StartVoiceActivity()
    {
        if (mainPanel != null) mainPanel.SetActive(false);
        if(voicePanel != null) voicePanel.SetActive(true);
    }

    public void ReturToMain()
    {
        if (voicePanel != null) voicePanel.SetActive(false);
        if (mainPanel != null) mainPanel.SetActive(true);

        UpdatePlantStatus();
    }
}
