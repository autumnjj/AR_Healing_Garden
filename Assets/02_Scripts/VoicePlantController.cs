using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class VoicePlantController : MonoBehaviour
{
    [Header("�ٽ� ������Ʈ")]
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

        // �ʱ� ���� ����
        if(voicePanel != null) voicePanel.SetActive(false);
        if(mainPanel != null) mainPanel.SetActive(true);
    }

    private void ShowWelcomeMessage()
    {
        if (welcomeText != null)
            welcomeText.text = "AR ���� ���� \n�������� ���� �Ĺ��� Ű��������!";

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
                plantStatusText.text = $"���� �Ĺ� : {stageName} �ܰ� \n���� ����Ʈ : {plantState.currentGrowthPoints:F0}";

            } 
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
