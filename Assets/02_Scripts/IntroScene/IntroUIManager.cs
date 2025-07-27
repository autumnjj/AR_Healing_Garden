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

    [Header("Setting UI")]
    public Button settingsButton;
    public Button settingsCloseButton;
    public Slider volumeSlider;

    [Header("대화 UI")]
    public TextMeshProUGUI dialogueText;

    [Header("Choice UI")]
    public TextMeshProUGUI choiceTitle;
    public TextMeshProUGUI choiceDescription;
    public Button mbtiButton;
    public Button arButton;
    public Button choiceCloseButton;

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

        // 선택 패널 텍스트 설정
        SetupChoicePanel();
    }

    private void HideAllPanles()
    {
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (choicePanel != null) choicePanel.SetActive(false);

    }

    private void ConnectButtonEvents()
    {
        // Setting 관련
        if (settingsButton != null)
            settingsButton.onClick.AddListener(ToggleSettings);
        if (settingsCloseButton != null)
            settingsCloseButton.onClick.AddListener(CloseSettings);

        // 선택 관련
        if (mbtiButton != null)
            mbtiButton.onClick.AddListener(GoToMBTI);
        if (arButton != null)
            arButton.onClick.AddListener(GoToAR);
        if (choiceCloseButton != null)
            choiceCloseButton.onClick.AddListener(CloseChoicePanel);

        // 컨트롤 관련
        if (SkipButton != null)
            SkipButton.onClick.AddListener(SkipIntro);
        if (QuitButton != null)
            QuitButton.onClick.AddListener(QuitApp);

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

    private void SetupChoicePanel()
    {
        if (choiceTitle != null)
            choiceTitle.text = "";

        if (choiceDescription != null)
            choiceDescription.text = "";
    }

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

    #region Intro Sequence
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

    private void OnSparrowSpawned()
    {
        Debug.Log("Show Sparrow");
    }

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

    private void OnSparrowTouched()
    {
        Debug.Log("Sparrow happy");
    }
    #endregion

    #region UI Panel 관리
    private void ShowDialoguePanel()
    {
        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);

        Debug.Log("Conversation Start!");
    }

    private void HideDialoguePanel()
    {
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
    }

    private void OnIntroComplete()
    {
        introComplete = true;

        HideDialoguePanel();
        ShowChoicePanel();

        Debug.Log("Intro Done. Show ChoicePanel");
    }

    private void ShowChoicePanel()
    {
        if (choicePanel != null)
            choicePanel.SetActive(true);
    }

    private void CloseChoicePanel()
    {
        if (choicePanel != null)
            choicePanel.SetActive(false);
    }
    #endregion

    #region Setting 관리
    private void OpenSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(true);

        Debug.Log("Open Settings Panel");
    }

    private void CloseSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        Debug.Log("Close Settings Panel");
    }

    private void OnVolumeChanged(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat("AppVolume", value);

        if (dialogueManager != null)
            dialogueManager.SetVoiceVolume(value);

        Debug.Log($"Change Volume : {value * 100:F0}%");
    }
    #endregion

    #region Scene Change
    private void GoToMBTI()
    {
        Debug.Log("Load MBTI Scene...");
        StartCoroutine(LoadSceneWithDelay("MBTIScene", 0.5f));
    }

    private void GoToAR()
    {
        Debug.Log("Load AR Scene...");

        // 기본 식물 설정
        PlayerPrefs.SetString("DirectStart", "true");
        PlayerPrefs.SetString("Matched_Plant", "default_plant");
        PlayerPrefs.Save();

        StartCoroutine(LoadSceneWithDelay("ARScene", 0.5f));
    }

    private IEnumerator LoadSceneWithDelay(string sceneName, float delay)
    {
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene(sceneName);
    }
    #endregion

    #region 컨트롤 Buttons
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

    private void QuitApp()
    {
        Debug.Log("Quit App");

        Application.Quit();
    }
    #endregion

    private void ToggleSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(!settingsPanel.activeSelf);
    }

}
