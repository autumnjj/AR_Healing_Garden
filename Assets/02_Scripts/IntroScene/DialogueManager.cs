using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.UI;

[System.Serializable]
public class DialogueLine
{
    [TextArea(2, 4)]
    public string text;
    public AudioClip voiceClip;

    [Header("Ÿ�̹� ����")]
    public float startDelay = 0f;
    public float typingSpeed = 0.04f;
    public float textCompletionRatio = 0.7f;
    public float pauseAfter = 0.8f;

    [Header("��� ����")]
    public float audioStartOffset = 0f;
    public bool waitForAudioComplete = true;
}
public class DialogueManager : MonoBehaviour
{
    [Header("���� ����")]
    public AudioSource voiceAudioSource;
    public float voiceVolume = 0.8f;

    [Header("��ȭ ����")]
    public DialogueLine[] dialogueLines;

    [Header("Skip ��ư")]
    public Button skipButton;
    public float skipButtonDelayTime = 5f;

    private TextMeshProUGUI dialogueText;

    // ����
    private int currentLineIndex = 0;
    private bool isPlaying = false;
    private bool isTyping = false;
    private bool isAudioPlaying = false;
    private bool isSkipped = false;

    private Coroutine dialogueCoroutine;
    private Coroutine typingCoroutine;
    private Coroutine audioCoroutine;

    // �̺�Ʈ
    public System.Action OnDialogueStart;
    public System.Action OnDialogueComplete;
    public System.Action<string> OnLineStart;
    public System.Action<int> OnLineComplete;
    public System.Action<int> OnAudioComplete;

    private void Start()
    {
        SetupDialogue();
        SetupDefaultDialogue();
        SetupSkipButton();
    }

    private void SetupDialogue()
    {
        if(voiceAudioSource != null)
            voiceAudioSource.volume = voiceVolume;

        if (dialogueText != null)
            dialogueText.text = "";
    }

    private void SetupSkipButton()
    {
        if (skipButton != null)
        {
            skipButton.gameObject.SetActive(false);
            skipButton.onClick.AddListener(SkipDialogue);
        }
    }

    private void SetupDefaultDialogue()
    {
        AudioClip line1Audio = Resources.Load<AudioClip>("Voice/Blee_Line1");
        AudioClip line2Audio = Resources.Load<AudioClip>("Voice/Blee_Line2");
        AudioClip line3Audio = Resources.Load<AudioClip>("Voice/Blee_Line3");

        dialogueLines = new DialogueLine[]
        {
            new DialogueLine
            {
                text = "�ȳ��ϼ���!\n ���� Blee����.\nBloomSpeak ���̵� ģ�����ϴ�!",
                voiceClip = line1Audio,
                startDelay = 0.2f,
                textCompletionRatio = 0.75f,
                pauseAfter = 0.5f,
                audioStartOffset = 0f,
                waitForAudioComplete = true
            },
            new DialogueLine
            {
                text = "AR�� ������Ư���� �Ĺ� ģ���� Ű�� �� �־��!\n �������� �ڱ��ȭ ������ �ڿ������� ������.",
                voiceClip = line2Audio,
                startDelay = 0.1f,
                textCompletionRatio = 0.8f,
                pauseAfter = 0.5f,
                audioStartOffset = 0f,
                waitForAudioComplete = true
            },
            new DialogueLine
            {
                text = "���� �����غ����?\n���ϴ� ����� ��󺸼���!",
                voiceClip = line3Audio,
                startDelay = 0.1f,
                textCompletionRatio = 0.85f,
                pauseAfter = 1f,
                audioStartOffset = 0f,
                waitForAudioComplete = true
            }
        };
    }

    public void SetDialogueText(TextMeshProUGUI newDialogueText)
    {
        dialogueText = newDialogueText;
        if (dialogueText != null)
            dialogueText.text = "";
    }

    public void StartDialogue()
    {
        if (isPlaying) return;

        isPlaying = true;
        isSkipped = false;
        currentLineIndex = 0;

        StartCoroutine(ShowSkipButtonAfterDelay());

        OnDialogueStart?.Invoke();
        dialogueCoroutine = StartCoroutine(PlayDialogueSequence());

        Debug.Log("Dialogue Started");
    }

    public void SkipDialogue()
    {
        if (!isPlaying) return;

        Debug.Log("Dialogue Skipped by user");

        isSkipped = true;

        StopAllDialogueCoroutine();

        if (voiceAudioSource != null && voiceAudioSource.isPlaying)
        {
            voiceAudioSource.Stop();
        }

        CompleteDialogue();
    }

    private IEnumerator ShowSkipButtonAfterDelay()
    {
        float elapsedTime = 0f;
        while (elapsedTime < skipButtonDelayTime && !isSkipped && isPlaying)
        {
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        if (!isSkipped && isPlaying && skipButton != null)
        {
            skipButton.gameObject.SetActive(true);
            Debug.Log("Skip button is now available");
        }
    }

    private void StopAllDialogueCoroutine()
    {
        if (dialogueCoroutine != null)
        {
            StopCoroutine(dialogueCoroutine);
            dialogueCoroutine = null;
        }
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }
        if(audioCoroutine != null)
        {
            StopCoroutine(audioCoroutine);
            audioCoroutine = null;
        }
    }


    private IEnumerator PlayDialogueSequence()
    {
        for (int i = 0; i < dialogueLines.Length; i++)
        {
            if (isSkipped) yield break;

            currentLineIndex = i;
            var line = dialogueLines[i];

            Debug.Log($"Playing Line {i + 1}/{dialogueLines.Length}");

            // ���� �ִϸ��̼� Ʈ����
            OnLineStart?.Invoke(GetEmotionFromText(line.text));

            if (line.startDelay > 0f)
            {
                yield return new WaitForSeconds(line.startDelay);
            }

            yield return StartCoroutine(PlayLineWithSync(line));

            OnLineComplete?.Invoke(i);

            // ���� �������� �Ѿ�� �� ���
            if (i < dialogueLines.Length - 1 && line.pauseAfter > 0)
            {
                yield return new WaitForSeconds(line.pauseAfter);
            }
        }

        // ��ȭ �Ϸ�
        CompleteDialogue();
    }

    private IEnumerator PlayLineWithSync(DialogueLine line)
    {
        if (isSkipped) yield break;

        // 1. ������� �ؽ�Ʈ ���� ����
        audioCoroutine = StartCoroutine(PlayAudio(line));
        typingCoroutine = StartCoroutine(TypeTextSynced(line));

        // 2. �ٽ� : ����� �Ϸ���� �ݵ�� ���
        if (line.waitForAudioComplete && line.voiceClip != null)
        {
            // ����� �Ϸ���� ���
            yield return audioCoroutine;
        }

        // 3. �ؽ�Ʈ Ÿ������ ���� ���� ���̸� �Ϸ� ���
        if (isTyping)
        {
            yield return typingCoroutine;
        }
    }

    private IEnumerator PlayAudio(DialogueLine line)
    {
        if (line.voiceClip == null || isSkipped) yield break;

        // ����� ���� ������ ����
        if (line.audioStartOffset > 0)
            yield return new WaitForSeconds(line.audioStartOffset);

        if (isSkipped) yield break;

        if (voiceAudioSource != null)
        {
            isAudioPlaying = true;
            voiceAudioSource.clip = line.voiceClip;
            voiceAudioSource.Play();

            // ���� ����� �Ϸ���� ��Ȯ�� ���
            float audioLength = line.voiceClip.length;
            float elapsed = 0f;

            while (elapsed < audioLength && !isSkipped)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
            isAudioPlaying = false;

            // ����� �Ϸ� �̺�Ʈ �߻�
            OnAudioComplete?.Invoke(currentLineIndex);
        }
    }

    private IEnumerator TypeTextSynced(DialogueLine line)
    {
        if (dialogueText == null || isSkipped) yield break;

        isTyping = true;
        dialogueText.text = "";

        // �ؽ�Ʈ �Ϸ� Ÿ�̹��� ����� ���̿� ���� ����
        float targetDuration;
        if(line.voiceClip != null)
        {
            targetDuration = line.voiceClip.length * line.textCompletionRatio;
        }
        else
        {
            targetDuration = line.text.Length * 0.04f;
        }

        float actualTypingSpeed = targetDuration / line.text.Length;
        actualTypingSpeed = Mathf.Clamp(actualTypingSpeed, 0.02f, 0.15f);

        for (int i = 0; i <= line.text.Length; i++)
        {
            if (!isTyping || isSkipped) break;

            dialogueText.text = line.text.Substring(0, i);
            yield return new WaitForSeconds(actualTypingSpeed);
        }

        isTyping = false;
    }

    private string GetEmotionFromText(string text)
    {
        if (text.Contains("�ȳ�")) return "greeting";
        if (text.Contains("�Բ�") || text.Contains("����")) return "excited";
        if (text.Contains("ġ��") || text.Contains("����")) return "curious";
        return "happy";
    }

    private void CompleteDialogue()
    {
        isPlaying = false;
        isTyping = false;
        isAudioPlaying = false;

        if (skipButton != null)
        {
            skipButton.gameObject.SetActive(false);
        }

        OnDialogueComplete?.Invoke();
    }


    // ���� �޼����
    public bool IsPlaying() => isPlaying;
    public bool IsTyping() => isTyping;
    public bool IsAudioPlaying() => isAudioPlaying;
    public int GetCurrentLineIndex() => currentLineIndex;
    public int GetTotalLines() => dialogueLines?.Length ?? 0;

    public void SetVoiceVolume(float volume)
    {
        voiceVolume = Mathf.Clamp01(volume);
        if(voiceAudioSource != null) 
            voiceAudioSource.volume = voiceVolume;
    }
}
