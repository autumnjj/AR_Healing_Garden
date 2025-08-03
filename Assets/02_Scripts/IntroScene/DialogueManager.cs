using UnityEngine;
using TMPro;
using System.Collections;

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

    private TextMeshProUGUI dialogueText;

    // ����
    private int currentLineIndex = 0;
    private bool isPlaying = false;
    private bool isTyping = false;
    private bool isAudioPlaying = false;

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
    }

    private void SetupDialogue()
    {
        if(voiceAudioSource != null)
            voiceAudioSource.volume = voiceVolume;

        if (dialogueText != null)
            dialogueText.text = "";
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
        currentLineIndex = 0;

        OnDialogueStart?.Invoke();
        dialogueCoroutine = StartCoroutine(PlayDialogueSequence());

        Debug.Log("Dialogue Started");
    }

    private IEnumerator PlayDialogueSequence()
    {
        for (int i = 0; i < dialogueLines.Length; i++)
        {
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
        if (line.voiceClip == null) yield break;

        // ����� ���� ������ ����
        if (line.audioStartOffset > 0)
            yield return new WaitForSeconds(line.audioStartOffset);

        if (voiceAudioSource != null)
        {
            isAudioPlaying = true;
            voiceAudioSource.clip = line.voiceClip;
            voiceAudioSource.Play();

            // ���� ����� �Ϸ���� ��Ȯ�� ���
            float audioLength = line.voiceClip.length;
            yield return new WaitForSeconds(audioLength);

            isAudioPlaying = false;

            // ����� �Ϸ� �̺�Ʈ �߻�
            OnAudioComplete?.Invoke(currentLineIndex);
        }
    }

    private IEnumerator TypeTextSynced(DialogueLine line)
    {
        if (dialogueText == null) yield break;

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
            if (!isTyping) break;

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
