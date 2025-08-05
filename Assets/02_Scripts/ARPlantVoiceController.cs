using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class ARPlantVoiceController : MonoBehaviour
{
    [Header("�ٽ� ������Ʈ")]
    public ARPlantManager arPlantManager;
    public PlantGrowthManager plantGrowthManager;
    public UnitySpeechRecognition voiceRecognition;
    public PlantInteractionSystem interactionSystem;

    [Header("UI �г�")]
    public GameObject mainARPanel;
    public GameObject voiceInputPanel;
    public GameObject plantStatusPanel;
    public GameObject instructionPanel;

    [Header("Main UI")]
    public TextMeshProUGUI instructionText;
    public Button voiceInputButton;
    public Button manualInteractionButton;
    public Button resetPlantButton;

    [Header("���� �ν� UI")]
    public TextMeshProUGUI voiceStatusText;
    public TextMeshProUGUI recognizedText;
    public TextMeshProUGUI targetPhraseText;
    public Button voiceToggleButton;
    public Button closeVoiceButton;

    [Header("�Ĺ� ���� UI")]
    public TextMeshProUGUI plantNameText;
    public TextMeshProUGUI plantStageText;
    public TextMeshProUGUI growthPointsText;
    public Slider growthProgressBar;
    public TextMeshProUGUI careMessageText;

    [Header("����")]
    public bool startWithPlantPlacement = true;
    public bool enableVoiceAfterPlacement = true;

    // ���� ����
    private bool isPlantPlaced = false;
    private bool isVoiceActive = false;
    private bool isInitialized = false;

    // �̺�Ʈ
    public System.Action OnPlantPlaced;
    public System.Action OnVoiceSessionStarted;
    public System.Action OnVoiceSessionEnded;

    private void Start()
    {
        StartCoroutine(InitializeSystem());
    }

    private IEnumerator InitializeSystem()
    {
        // 1. ������Ʈ �ʱ�ȭ
        yield return StartCoroutine(InitializeComponents());

        // 2. UI �ʱ�ȭ
        SetupUI();

        // 3. �̺�Ʈ ����
        ConnectEvents();

        // 4. �ʱ� ���� ����
        SetInitialState();
        
        isInitialized = true;
        Debug.Log("ARPlantVoiceController initialized successfully.");
    }

    private IEnumerator InitializeComponents()
    {
        // ARPlantManager �ʱ�ȭ
        if (arPlantManager == null) arPlantManager = FindAnyObjectByType<ARPlantManager>();
        
        // PlantGrowthManager �ʱ�ȭ
        if (plantGrowthManager == null) plantGrowthManager = FindAnyObjectByType<PlantGrowthManager>();

        // UnitySpeechRecognition �ʱ�ȭ
        if (voiceRecognition == null) voiceRecognition = FindAnyObjectByType<UnitySpeechRecognition>();

        // PlantInteractionSystem �ʱ�ȭ
        if (interactionSystem == null) interactionSystem = FindAnyObjectByType<PlantInteractionSystem>();
        
        yield return new WaitForSeconds(0.5f); 

        // ���� �νİ� �Ĺ� ���� �ý��� ����
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
        // ��ư �̺�Ʈ ����
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

        // �ʱ� UI ����
        UpdateUI();
    }

    private void ConnectEvents()
    {
        // ARPlantManager �̺�Ʈ
        if (arPlantManager != null)
        {
            StartCoroutine(CheckPlantPlacement());
        }

        // PlantGrowthManager �̺�Ʈ
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

        UpdateInstructionText("�Ĺ��� ��ġ�Ǿ����ϴ�! ���� �������� �Ĺ��� ��ȭ�غ�����.");

        if (enableVoiceAfterPlacement)
        {
            // �ڵ����� ���� �Է� �г� Ȱ��ȭ
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
            UpdateInstructionText("ȭ���� ��ġ�Ͽ� �Ĺ��� ��ġ�� ������ ã�ƺ�����");
        }

        // �ʱ⿡�� ���� ��� ��Ȱ��ȭ
        SetVoiceUIEnabled(false);
    }

    public void OpenVoiceInput()
    {
        if (!isInitialized)
        {
            UpdateInstructionText("���� �Ĺ��� ��ġ���ּ���!");
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

        // ���� �ν� ����
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
            UpdateInstructionText("���� �Ĺ��� ��ġ���ּ���!");
            return;
        }

        // ���� ��ȣ�ۿ� �ý��� Ȱ��ȭ
        if (interactionSystem != null)
        {
            interactionSystem.SetInteractionEnabled(true);
        }

        UpdateInstructionText("��ư�� ���� �Ĺ��� ��ȣ�ۿ��ϼ���.");
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
        UpdateInstructionText("�Ĺ��� �ʱ�ȭ�Ǿ����ϴ�. �ٽ� ��ġ���ּ���.");
    }

    private void OnPlantStageChanged(PlantGrowthStage newStage)
    {
        UpdatePlantStatusUI();

        string stageMessage = GetStageMessage(newStage);
        UpdateInstructionText(stageMessage);

        Debug.Log($"�Ĺ� ���� �ܰ� ����: {newStage}");
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
                return "������ ������ ��ٸ��� �־��.";
            case PlantGrowthStage.Sprout:
                return "������ ���Ծ��! ��� ������ּ���.";
            case PlantGrowthStage.Growing:
                return "�������� �ڶ�� �־��";
            case PlantGrowthStage.Blooming:
                return "�Ƹ���� ���� �Ǿ����!";
            default:
                return "�Ĺ��� �Բ� �����غ�����!";
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
                    plantNameText.text = plantData?.koreanName ?? "�Ĺ�";
                }
                    
                if (plantStageText != null)
                {
                    plantStageText.text = $"���� �ܰ�: {GetStageKoreanName(plantState.currentStage)}";
                }
                    
                if (growthPointsText != null)
                {
                    growthPointsText.text = $"���� ����Ʈ: {plantState.currentGrowthPoints:F0}";
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
                buttonText.text = isListening ? "���� �ν� ����" : "���� �ν� ����";
            }
        }
    }

    private string GetStageKoreanName(PlantGrowthStage stage)
    {
        switch (stage)
        {
            case PlantGrowthStage.Seed:
                return "����";
            case PlantGrowthStage.Sprout:
                return "����";
            case PlantGrowthStage.Growing:
                return "����";
            case PlantGrowthStage.Blooming:
                return "��ȭ";
            default:
                return "�� �� ����";
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
