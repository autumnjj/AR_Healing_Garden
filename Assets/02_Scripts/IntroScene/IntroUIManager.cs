using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class IntroUIManager : MonoBehaviour
{
    [Header("�ٽ� ������Ʈ")]
    public DialogueManager dialogueManager;
    public SparrowController sparrowController;
    public ARInputHandler inputHandler;

    [Header("UI Panels")]
    public GameObject dialoguePanel;
    public GameObject settingsPanel;
    public GameObject choicePanel;

    [Header("���� UI ��Ʈ�ѷ���")]
    public MBTIDropdownUI mbtiDropdownUI;
    public ChoiceUIController choiceUIController;

    [Header("Setting UI")]
    public Button settingsButton;
    public Button settingsCloseButton;
    public Slider volumeSlider;

    [Header("��ȭ UI")]
    public TextMeshProUGUI dialogueText;

    [Header("��Ʈ�� Buttons")]
    public Button SkipButton;
    public Button QuitButton;

    // ����
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
        // ��ư �̺�Ʈ ����
        ConnectButtonEvents();

        // ���� �ʱ�ȭ
        InitializeVolume();

        HideAllPanles();

        // ���� ��Ʈ�ѷ��� �ʱ�ȭ
        SetupSubControllers();
    }

    private void SetupSubControllers()
    {
        // Choice UI Controller �̺�Ʈ ����
        if (choiceUIController != null)
        {
            choiceUIController.OnDirectInputSelected += () => mbtiDropdownUI.ShowPanel();
            choiceUIController.OnQuickTestSelected += GoToMBTI;
            choiceUIController.OnQuickStartSelected += GoToAR;
        }

        // MBTI Dropdown UI �̺�Ʈ ����
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

    #region ���� ��Ʈ�ѷ� �̺�Ʈ ó��
    private void OnMBTIConfirmed(string mbtiCode)
    {
        Debug.Log($"MBTI Confired: {mbtiCode}");

        // ������ ����
        PlayerPrefs.SetString("MBTI_Type", mbtiCode);
        PlayerPrefs.SetString("InputMethod", "DirectInput");
        PlayerPrefs.Save();

        // AR ������ �̵�
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

        // �⺻ �Ĺ� ����
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
        // ��ȭ �̺�Ʈ
        if (dialogueManager != null)
        {
            if (dialogueText != null)
                dialogueManager.SetDialogueText(dialogueText);

            dialogueManager.OnDialogueStart += ShowDialoguePanel;
            dialogueManager.OnDialogueComplete += OnIntroComplete;
        }

        // ���� �̺�Ʈ
        if (sparrowController != null)
        {
            sparrowController.OnSparrowSpawned += OnSparrowSpawned;
            sparrowController.OnSparrowTouched += OnSparrowTouched;
            sparrowController.OnEntranceComplete += OnSparrowReady;
        }

        // ��ġ �̺�Ʈ
        if (inputHandler != null)
            inputHandler.OnSparrowTouched += OnSparrowInteraction;
    }

    private void StartIntroSequence()
    {
        Debug.Log("Ready For AR Environment");

        // 1���� ���� ����
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

        // ������ �غ�Ǹ� 3�� �� �ڵ����� ��ȭ ����
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

        // ���� ��ȭ�� ���� �ȵ����� ��� ����
        if (!dialogueManager.IsPlaying() && introStarted)
        {
            CancelInvoke(nameof(StartDialogue));
            StartDialogue();
        }
    }

    private void OnSparrowTouched() => Debug.Log("Sparrow happy");
    #endregion

    #region UI Panel ����
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
        // Setting ����
        if (settingsButton != null)
            settingsButton.onClick.AddListener(ToggleSettings);
        if (settingsCloseButton != null)
            settingsCloseButton.onClick.AddListener(() => settingsPanel.SetActive(false));

        // ��Ʈ�� ����
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
