using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class ARPlantVoiceController : MonoBehaviour
{
    [Header("핵심 컴포넌트")]
    public ARPlantManager arPlantManager;
    public PlantGrowthManager plantGrowthManager;
    public UnitySpeechRecognition voiceRecognition;
    public PlantInteractionSystem interactionSystem;

    [Header("UI 패널")]
    public GameObject mainARPanel;
    public GameObject voiceInputPanel;
    public GameObject plantStatusPanel;
    public GameObject instructionPanel;

    [Header("Main UI")]
    public TextMeshProUGUI instructionText;
    public Button voiceInputButton;
    public Button manualInteractionButton;
    public Button resetPlantButton;

    [Header("음성 인식 UI")]
    public TextMeshProUGUI voiceStatusText;
    public TextMeshProUGUI recognizedText;
    public TextMeshProUGUI targetPhraseText;
    public Button voiceToggleButton;
    public Button closeVoiceButton;

    [Header("식물 상태 UI")]
    public TextMeshProUGUI plantNameText;
    public TextMeshProUGUI plantStageText;
    public TextMeshProUGUI growthPointsText;
    public Slider growthProgressBar;
    public TextMeshProUGUI careMessageText;

    [Header("설정")]
    public bool startWithPlantPlacement = true;
    public bool enableVoiceAfterPlacement = true;

    // 상태 관리
    private bool isPlantPlaced = false;
    private bool isVoiceActive = false;
    private bool isInitialized = false;

    // 이벤트
    public System.Action OnPlantPlaced;
    public System.Action OnVoiceSessionStarted;
    public System.Action OnVoiceSessionEnded;

    private void Start()
    {
        StartCoroutine(InitializeSystem());
    }

    private IEnumerator InitializeSystem()
    {
        // 1. 컴포넌트 초기화
        yield return StartCoroutine(InitializeComponents());

        // 2. UI 초기화
        SetupUI();

        // 3. 이벤트 연결
        ConnectEvents();

        // 4. 초기 상태 설정
        SetInitialState();
        
        isInitialized = true;
        Debug.Log("ARPlantVoiceController initialized successfully.");
    }

    private IEnumerator InitializeComponents()
    {
        // ARPlantManager 초기화
        if (arPlantManager == null) arPlantManager = FindAnyObjectByType<ARPlantManager>();
        
        // PlantGrowthManager 초기화
        if (plantGrowthManager == null) plantGrowthManager = FindAnyObjectByType<PlantGrowthManager>();

        // UnitySpeechRecognition 초기화
        if (voiceRecognition == null) voiceRecognition = FindAnyObjectByType<UnitySpeechRecognition>();

        // PlantInteractionSystem 초기화
        if (interactionSystem == null) interactionSystem = FindAnyObjectByType<PlantInteractionSystem>();
        
        yield return new WaitForSeconds(0.5f); 

        // 음성 인식과 식물 성장 시스템 연결
        if (voiceRecognition != null && plantGrowthManager != null)
        {
            var plantGrowthData = plantGrowthManager.GetComponent<PlantGrowthData>();
            if (plantGrowthData != null)
            {
                voiceRecognition.plantGrowthData = plantGrowthData;
            }
        }
    }

    private void SetupUI()
    {
        // 버튼 이벤트 연결
        if (voiceInputButton != null)
            voiceInputButton.onClick.AddListener(OpenVoiceInput);

        if (manualInteractionButton != null) 
            manualInteractionButton.onClick.AddListener(OpenManualInteraction);

        if (resetPlantButton != null) 
            resetPlantButton.onClick.AddListener(ResetPlant);

        if (voiceToggleButton != null)
            voiceToggleButton.onClick.AddListener(ToggleVoiceRecognition);

        if (closeVoiceButton != null)
            closeVoiceButton.onClick.AddListener(CloseVoiceInput);

        // 초기 UI 설정
        UpdateUI();
    }

    private void ConnectEvents()
    {
        // ARPlantManager 이벤트
        if (arPlantManager != null)
        {
            StartCoroutine(CheckPlantPlacement());
        }

        // PlantGrowthManager 이벤트
        if (plantGrowthManager != null)
        {
            var plantGrowthData = plantGrowthManager.GetComponent<PlantGrowthData>();
            if (plantGrowthData != null)
            {
                plantGrowthData.OnStageChanged += OnPlantStageChanged;
                plantGrowthData.OnGrowthPointsChanged += OnGrowthPointsChanged;
                plantGrowthData.OnMessageUpdate += OnCareMessageUpdated;
            }
        }
    }

    private IEnumerator CheckPlantPlacement()
    {
        while (!isPlantPlaced)
        {
            if (arPlantManager != null && arPlantManager.IsPlantPlaced())
            {
                OnPlantPlacedSuccessfully();
                yield break;
            } 
            yield return new WaitForSeconds(0.5f);
        }
    }

    private void OnPlantPlacedSuccessfully()
    {
        isPlantPlaced = true;
        OnPlantPlaced.Invoke();

        UpdateInstructionText("식물이 배치되었습니다! 이제 음성으로 식물과 대화해보세요.");

        if (enableVoiceAfterPlacement)
        {
            // 자동으로 음성 입력 패널 활성화
            StartCoroutine(DelayedVoiceActivation());
        }

        UpdateUI();
        Debug.Log("Plant has been successfully placed.");
    }

    private IEnumerator DelayedVoiceActivation()
    {
        yield return new WaitForSeconds(1f);
        OpenVoiceInput();
    }

    private void SetInitialState()
    {
        if (startWithPlantPlacement)
        {
            UpdateInstructionText("화면을 터치하여 식물을 배치할 공간을 찾아보세요");
        }

        // 초기에는 음성 기능 비활성화
        SetVoiceUIEnabled(false);
    }

    public void OpenVoiceInput()
    {
        if (!isInitialized)
        {
            UpdateInstructionText("먼저 식물을 배치해주세요!");
            return;
        }

        ShowVoicePanel();
        isVoiceActive = true;
        OnVoiceSessionStarted?.Invoke();

        UpdateUI();
        Debug.Log("Voice input panel opened.");
    }

    public void CloseVoiceInput()
    {
        isVoiceActive = false;
        OnVoiceSessionEnded?.Invoke();

        // 음성 인식 중지
        if (voiceRecognition != null && voiceRecognition.IsListening())
        {
            voiceRecognition.ToggleListening();
        }

        UpdateUI();
        Debug.Log("Voice input panel closed.");
    }

    public void OpenManualInteraction()
    {
        if (!isPlantPlaced)
        {
            UpdateInstructionText("먼저 식물을 배치해주세요!");
            return;
        }

        // 수동 상호작용 시스템 활성화
        if (interactionSystem != null)
        {
            interactionSystem.SetInteractionEnabled(true);
        }

        UpdateInstructionText("버튼을 눌러 식물과 상호작용하세요.");
    }

    public void ToggleVoiceRecognition()
    {
        if (voiceRecognition != null)
        {
            voiceRecognition.ToggleListening();
            UpdateVoiceButtonText();
        }
    }

    public void ResetPlant()
    {
        if (arPlantManager != null)
            arPlantManager.ResetPlantPlacement();

        if (plantGrowthManager != null)
        {
            var plantGrowthData = plantGrowthManager.GetComponent<PlantGrowthData>();
            if (plantGrowthData != null)
            {
                plantGrowthData.ResetToDefault();
            }
        }

        isPlantPlaced = false;
        isVoiceActive = false;

        SetInitialState();
        UpdateInstructionText("식물이 초기화되었습니다. 다시 배치해주세요.");
    }

    private void OnPlantStageChanged(PlantGrowthStage newStage)
    {
        UpdatePlantStatusUI();

        string stageMessage = GetStageMessage(newStage);
        UpdateInstructionText(stageMessage);

        Debug.Log($"식물 성장 단계 변경: {newStage}");
    }

    private void OnGrowthPointsChanged(float newPoints)
    {
        UpdatePlantStatusUI();
    }

    private void OnCareMessageUpdated(string message)
    {
        if (careMessageText != null)
        {
            careMessageText.text = message;
        }
    }

    private string GetStageMessage(PlantGrowthStage stage)
    {
        switch (stage)
        {
            case PlantGrowthStage.Seed:
                return "씨앗이 조용히 기다리고 있어요.";
            case PlantGrowthStage.Sprout:
                return "새싹이 나왔어요! 계속 사랑해주세요.";
            case PlantGrowthStage.Growing:
                return "무럭무럭 자라고 있어요";
            case PlantGrowthStage.Blooming:
                return "아름답게 꽃이 피었어요!";
            default:
                return "식물과 함께 성장해보세요!";
        }
    }

    private void UpdateUI()
    {
        SetVoiceUIEnabled(isPlantPlaced);
        UpdatePlantStatusUI();
        UpdateVoiceStatusUI();
        UpdateVoiceButtonText();
    }

    private void SetVoiceUIEnabled(bool enabled)
    {
        if (voiceInputButton != null)
            voiceInputButton.interactable = enabled;

        if (manualInteractionButton != null)
            manualInteractionButton.interactable = enabled;
    }

    private void UpdatePlantStatusUI()
    {
        if (!isPlantPlaced) return;

        if (plantGrowthManager != null)
        {
            var plantState = plantGrowthManager.GetCurrentPlantState();
            if (plantState != null)
            {
                if (plantNameText != null)
                {
                    var plantData = plantGrowthManager.GetComponent<PlantGrowthData>()?.plantData;
                    plantNameText.text = plantData?.koreanName ?? "식물";
                }
                    
                if (plantStageText != null)
                {
                    plantStageText.text = $"성장 단계: {GetStageKoreanName(plantState.currentStage)}";
                }
                    
                if (growthPointsText != null)
                {
                    growthPointsText.text = $"성장 포인트: {plantState.currentGrowthPoints:F0}";
                }
                   
                if (growthProgressBar != null)
                {
                    var growthData = plantGrowthManager.GetComponent<PlantGrowthData>();
                    if (growthData != null)
                    {
                        float maxPoints = growthData.growthSettings.maxGrowthPoints;
                        growthProgressBar.value = plantState.currentGrowthPoints / maxPoints;
                    }
                        
                }
            }
        }
    }

    private void UpdateVoiceStatusUI()
    {
        if (voiceRecognition == null) return;
    }

    private void UpdateVoiceButtonText()
    {
        if (voiceToggleButton != null && voiceRecognition != null)
        {
            var buttonText = voiceToggleButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                bool isListening = voiceRecognition.IsListening();
                buttonText.text = isListening ? "음성 인식 중지" : "음성 인식 시작";
            }
        }
    }

    private string GetStageKoreanName(PlantGrowthStage stage)
    {
        switch (stage)
        {
            case PlantGrowthStage.Seed:
                return "씨앗";
            case PlantGrowthStage.Sprout:
                return "새싹";
            case PlantGrowthStage.Growing:
                return "성장";
            case PlantGrowthStage.Blooming:
                return "개화";
            default:
                return "알 수 없음";
        }
    }

    private void ShowVoicePanel()
    {
        if (voiceInputPanel != null) voiceInputPanel.SetActive(true);
        if (plantStatusPanel != null) plantStatusPanel.SetActive(true);
    }

    private void UpdateInstructionText(string message)
    {
        if (instructionText != null) 
            instructionText.text = message;
    }

    public bool IsPlantPlaced() => isPlantPlaced;
    public bool IsVoiceActive() => isVoiceActive;
    public bool IsInitialized() => isInitialized;

    public bool IsListening()
    {
        return voiceRecognition != null ? voiceRecognition.IsListening() : false;
    }

    private void OnDestroy()
    {
        if (plantGrowthManager != null)
        {
            var plantGrowthData = plantGrowthManager.GetComponent<PlantGrowthData>();
            if (plantGrowthData != null)
            {
                plantGrowthData.OnStageChanged -= OnPlantStageChanged;
                plantGrowthData.OnGrowthPointsChanged -= OnGrowthPointsChanged;
                plantGrowthData.OnMessageUpdate -= OnCareMessageUpdated;
            }
        }
    }

}
