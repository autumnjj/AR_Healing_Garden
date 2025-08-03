using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class IntroUIManager : MonoBehaviour
{
    [Header("핵심 컴포넌트")]
    public DialogueManager dialogueManager;
    public SparrowController sparrowController;
    public ChoiceUIController choiceUIController;
    public MBTIDropdownUI mbtiDropdownUI;

    [Header("UI Panels")]
    public GameObject dialoguePanel;
    public GameObject settingsPanel;
    public GameObject choicePanel;

    [Header("Setting UI")]
    public Button settingsButton;
    public Button settingsCloseButton;
    public Slider volumeSlider;

    [Header("대화 UI")]
    public TextMeshProUGUI dialogueText;

    [Header("컨트롤 Buttons")]
    public Button QuitButton;

    private void Start()
    {
        SetupUI();
    }

    private void SetupUI()
    {
        // 패널 초기 상태
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (choicePanel != null) choicePanel.SetActive(false);

        // Settings 버튼 연결
        SetupSettings();

        // DialogueManager 연결
        if (dialogueManager != null && dialogueText != null)
        {
            dialogueManager.SetDialogueText(dialogueText);
            dialogueManager.OnDialogueStart += () => dialoguePanel?.SetActive(true);
            dialogueManager.OnDialogueComplete += OnDialogueComplete;
        }

        // SaprrowController 연결
        if (sparrowController != null)
        {
            sparrowController.OnEntranceComplete += StartDialogue;
        }
        

        // Choice UI 연결
        if (choiceUIController != null)
        {
            choiceUIController.OnDirectInputSelected += () => mbtiDropdownUI.ShowPanel();
            choiceUIController.OnQuickTestSelected += () =>
            {
                PlayerPrefs.SetString("InputMethod", "QuickTest");
                PlayerPrefs.Save();
                SceneManager.LoadScene("MBTIScene");
            };
            choiceUIController.OnQuickStartSelected += () =>
            {
                PlayerPrefs.SetString("InputMethod", "QuickStart");
                PlayerPrefs.SetString("MBTI_Type", "ENFP");
                PlayerPrefs.SetString("Matched_Plant", "Sunflower");
                PlayerPrefs.Save();
                SceneManager.LoadScene("ARScene");
            };
        }

        // MBTI Dropdown UI 연결
        if (mbtiDropdownUI != null)
        {
            mbtiDropdownUI.OnMBTIConfirmed += (mbti) =>
            {
                PlayerPrefs.SetString("MBTI_Type", mbti);
                PlayerPrefs.SetString("InputMethod", "DirectInput");
                PlayerPrefs.Save();
                SceneManager.LoadScene("ARScene");
            };
            mbtiDropdownUI.OnCancelled += () => choicePanel?.SetActive(true);
        }
    }

    private void SetupSettings()
    {
        // Settings 버튼 이벤트
        if (settingsButton != null)
            settingsButton.onClick.AddListener(ToggleSettings);
        if (settingsCloseButton != null)
            settingsCloseButton.onClick.AddListener(() => settingsPanel.SetActive(false));

        // 볼륨 초기화 및 이벤트
        InitializeVolume();

        if (volumeSlider != null)
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);

        // Quit button 관련
        if (QuitButton != null)
            QuitButton.onClick.AddListener(() => Application.Quit());

    }

    private void InitializeVolume()
    {
        float savedVolume = PlayerPrefs.GetFloat("AppVolume", 0.8f);
        if (volumeSlider != null) volumeSlider.value = savedVolume;
        AudioListener.volume = savedVolume;
    }

    private void ToggleSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(!settingsPanel.activeSelf);
    }

    private void OnVolumeChanged(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat("AppVolume", value);

        if (dialogueManager != null)
            dialogueManager.SetVoiceVolume(value);

        Debug.Log($"Change Volume : {value * 100:F0}%");
    }

    private void StartDialogue()
    {
        Debug.Log("Starting dialogue...");
        // 참새 준비되면 바로 대화 시작
        if (dialogueManager != null)
        {
            dialogueManager.StartDialogue();
            sparrowController.StartSpeaking();
        }
    }

    private void OnDialogueComplete()
    {
        Debug.Log("All dialogue completed");

        // 대화 끝나면 선택 화면으로
        sparrowController?.StopSpeaking();

        if (sparrowController != null)
        {
            sparrowController.DestroySparrow();
        }

        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (choicePanel != null) choicePanel.SetActive(true);
        
    }

    private void OnDestroy()
    {
        // 이벤트 해제
        if (dialogueManager != null)
        {
            dialogueManager.OnDialogueStart -= () => dialoguePanel?.SetActive(true);
            dialogueManager.OnDialogueComplete -= OnDialogueComplete;
        }
        
        if (sparrowController != null)
        {
            sparrowController.OnEntranceComplete -= StartDialogue;
        }
    }

}
