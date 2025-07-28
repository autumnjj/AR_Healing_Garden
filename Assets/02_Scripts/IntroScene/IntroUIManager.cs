using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class IntroUIManager : MonoBehaviour
{
    [Header("핵심 컴포넌트")]
    public DialogueManager dialogueManager;
    public SparrowController sparrowController;
    public ARInputHandler inputHandler;

    [Header("UI Panels")]
    public GameObject dialoguePanel;
    public GameObject settingsPanel;
    public GameObject choicePanel;

    [Header("하위 UI 컨트롤러들")]
    public MBTIDropdownUI mbtiDropdownUI;
    public ChoiceUIController choiceUIController;

    [Header("Setting UI")]
    public Button settingsButton;
    public Button settingsCloseButton;
    public Slider volumeSlider;

    [Header("대화 UI")]
    public TextMeshProUGUI dialogueText;

    [Header("컨트롤 Buttons")]
    public Button SkipButton;
    public Button QuitButton;

    // 상태
    private bool introStarted = false;
    private bool introComplete = false;

    private void Start()
    {
        SetupUI();
        ConnectEvents();
        StartIntroSequence();
    }

    private void SetupUI()
    {
        // 버튼 이벤트 연결
        ConnectButtonEvents();

        // 볼륨 초기화
        InitializeVolume();

        HideAllPanles();

        // 하위 컨트롤러들 초기화
        SetupSubControllers();
    }

    private void SetupSubControllers()
    {
        // Choice UI Controller 이벤트 연결
        if (choiceUIController != null)
        {
            choiceUIController.OnDirectInputSelected += () => mbtiDropdownUI.ShowPanel();
            choiceUIController.OnQuickTestSelected += GoToMBTI;
            choiceUIController.OnQuickStartSelected += GoToAR;
        }

        // MBTI Dropdown UI 이벤트 연결
        if (mbtiDropdownUI != null)
        {
            mbtiDropdownUI.OnMBTIConfirmed += OnMBTIConfirmed;
            mbtiDropdownUI.OnCancelled += () => ShowChoicePanel();
        }
    }

    private void HideAllPanles()
    {
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (choicePanel != null) choicePanel.SetActive(false);
    }

    #region 하위 컨트롤러 이벤트 처리
    private void OnMBTIConfirmed(string mbtiCode)
    {
        Debug.Log($"MBTI Confired: {mbtiCode}");

        // 데이터 저장
        PlayerPrefs.SetString("MBTI_Type", mbtiCode);
        PlayerPrefs.SetString("InputMethod", "DirectInput");
        PlayerPrefs.Save();

        // AR 씬으로 이동
        StartCoroutine(LoadSceneWithDelay("ARScene", 0.5f));
    }

    private void GoToMBTI()
    {
        Debug.Log("Load MBTI Scene...");
        PlayerPrefs.SetString("InputMethod", "QuickTest");
        PlayerPrefs.Save();
        StartCoroutine(LoadSceneWithDelay("MBTIScene", 0.5f));
    }

    private void GoToAR()
    {
        Debug.Log("Load AR Scene...");

        // 기본 식물 설정
        PlayerPrefs.SetString("InputMethod", "QuickTest");
        PlayerPrefs.SetString("MBTI_Type", "ENFP");
        PlayerPrefs.SetString("Matched_Plant", "default_plant");
        PlayerPrefs.Save();

        StartCoroutine(LoadSceneWithDelay("ARScene", 0.5f));
    }
    #endregion

    #region Intro Sequence
    private void ConnectEvents()
    {
        // 대화 이벤트
        if (dialogueManager != null)
        {
            if (dialogueText != null)
                dialogueManager.SetDialogueText(dialogueText);

            dialogueManager.OnDialogueStart += ShowDialoguePanel;
            dialogueManager.OnDialogueComplete += OnIntroComplete;
        }

        // 참새 이벤트
        if (sparrowController != null)
        {
            sparrowController.OnSparrowSpawned += OnSparrowSpawned;
            sparrowController.OnSparrowTouched += OnSparrowTouched;
            sparrowController.OnEntranceComplete += OnSparrowReady;
        }

        // 터치 이벤트
        if (inputHandler != null)
            inputHandler.OnSparrowTouched += OnSparrowInteraction;
    }

    private void StartIntroSequence()
    {
        Debug.Log("Ready For AR Environment");

        // 1초후 참새 스폰
        Invoke(nameof(SpawnSparrow), 1f);
    }

    private void SpawnSparrow()
    {
        if (sparrowController != null)
        {
            sparrowController.SpawnSparrow();
            introStarted = true;
        }
    }

    private void OnSparrowSpawned() => Debug.Log("Show Sparrow");

    private void OnSparrowReady()
    {
        Debug.Log("Sparrow Ready done. After 3 seconds auto start");

        // 참새가 준비되면 3초 후 자동으로 대화 시작
        Invoke(nameof(StartDialogue), 3f);
    }

    private void StartDialogue()
    {
        if (dialogueManager != null && !dialogueManager.IsPlaying())
            dialogueManager.StartDialogue();
    }

    private void OnSparrowInteraction(Vector2 touchPos)
    {
        Debug.Log("Sparrow Touch");

        // 아직 대화가 시작 안됐으면 즉시 시작
        if (!dialogueManager.IsPlaying() && introStarted)
        {
            CancelInvoke(nameof(StartDialogue));
            StartDialogue();
        }
    }

    private void OnSparrowTouched() => Debug.Log("Sparrow happy");
    #endregion

    #region UI Panel 관리
    private void ShowDialoguePanel()
    {
        if (dialoguePanel != null) dialoguePanel.SetActive(true);

        Debug.Log("Conversation Start!");
    }

    private void OnIntroComplete()
    {
        introComplete = true;
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        ShowChoicePanel();

        Debug.Log("Intro Done. Show ChoicePanel");
    }

    private void ShowChoicePanel()
    {
        if (choicePanel != null) choicePanel.SetActive(true);
    }
    #endregion

    #region Settings & Controls
    private void ConnectButtonEvents()
    {
        // Setting 관련
        if (settingsButton != null)
            settingsButton.onClick.AddListener(ToggleSettings);
        if (settingsCloseButton != null)
            settingsCloseButton.onClick.AddListener(() => settingsPanel.SetActive(false));

        // 컨트롤 관련
        if (SkipButton != null)
            SkipButton.onClick.AddListener(SkipIntro);
        if (QuitButton != null)
            QuitButton.onClick.AddListener(() => Application.Quit());

        if (volumeSlider != null)
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
    }

    private void InitializeVolume()
    {
        float savedVolume = PlayerPrefs.GetFloat("AppVolume", 0.8f);
        if (volumeSlider != null)
            volumeSlider.value = savedVolume;
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

    private void SkipIntro()
    {
        Debug.Log("Skip Intro");

        if (dialogueManager != null && dialogueManager.IsPlaying())
        {
            dialogueManager.SkipToEnd();
        }
        else if (!introComplete)
        {
            OnIntroComplete();
        }
    }

    private IEnumerator LoadSceneWithDelay(string sceneName, float delay)
    {
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene(sceneName);
    }
    #endregion

}
