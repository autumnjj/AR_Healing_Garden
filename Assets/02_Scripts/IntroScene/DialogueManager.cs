using UnityEngine;
using TMPro;
using System.Collections;

[System.Serializable]
public class DialogueLine
{
    [TextArea(2, 4)]
    public string text;
    public AudioClip voiceClip;
    public float typingSpeed = 0.04f;
    public float pauseAfter = 1f;
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
    private Coroutine dialogueCoroutine;

    // �̺�Ʈ
    public System.Action OnDialogueStart;
    public System.Action OnDialogueComplete;
    public System.Action<string> OnLineStart;

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
                text = "�ȳ��ϼ���!\n ���� Blee����. BloomSpeak ���̵� ģ�����ϴ�!",
                voiceClip = line1Audio,
                typingSpeed = 0.05f,
                pauseAfter = 2.5f
            },
            new DialogueLine
            {
                text = "���⼭�� AR�� ������ ���� ġ�� ������ �����, ������ �� �Ѹ���� �Ĺ��� �Բ� ������ �� �־��!",
                voiceClip = line2Audio,
                typingSpeed = 0.04f,
                pauseAfter = 3f
            },
            new DialogueLine
            {
                text = " ���⼭�� AR�� ������ ���� ġ�� ������ �����, ������ �� �Ѹ���� �Ĺ��� �Բ� ������ �� �־�",
                voiceClip = line3Audio,
                typingSpeed = 0.04f,
                pauseAfter = 2f
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
    }

    private IEnumerator PlayDialogueSequence()
    {
        for (int i = 0; i < dialogueLines.Length; i++)
        {
            currentLineIndex = i;
            var line = dialogueLines[i];

            // ���� �ִϸ��̼� Ʈ����
            OnLineStart?.Invoke(GetEmotionFromText(line.text));

            // ������ Ÿ���� ���� ����
            if(line.voiceClip != null) 
                PlayVoice(line.voiceClip);

            yield return StartCoroutine(TypeText(line.text, line.typingSpeed));

            // ���� �������� �Ѿ�� �� ���
            if (i < dialogueLines.Length - 1)
            {
                yield return new WaitForSeconds(line.pauseAfter);
            }
        }

        // ��ȭ �Ϸ�
        CompleteDialogue();
    }


    private IEnumerator TypeText(string text, float speed)
    {
        if (dialogueText == null) yield break;

        dialogueText.text = "";

        for (int i = 0; i <= text.Length; i++)
        {
            dialogueText.text = text.Substring(0, i);
            yield return new WaitForSeconds(speed);
        }
    }

    private void PlayVoice(AudioClip voiceClip)
    {
        if (voiceAudioSource != null || voiceClip != null)
        {
            voiceAudioSource.clip = voiceClip;
            voiceAudioSource.Play();
        }
    }

    private string GetEmotionFromText(string text)
    {
        if (text.Contains("�ȳ�")) return "greeting";
        if (text.Contains("����")) return "excited";
        if (text.Contains("��ġ") || text.Contains("��ȣ�ۿ�")) return "curious";
        return "happy";
    }

    private void CompleteDialogue()
    {
        isPlaying = false;
        OnDialogueComplete?.Invoke();
    }

    public void SkipToEnd()
    {
        if (dialogueCoroutine != null)
            StopCoroutine(dialogueCoroutine);

        if (dialogueLines.Length > 0)
            dialogueText.text = dialogueLines[dialogueLines.Length - 1].text;

        CompleteDialogue();
    }

    // ���� �޼����
    public bool IsPlaying() => isPlaying;
    public void SetVoiceVolume(float volume)
    {
        voiceVolume = Mathf.Clamp01(volume);
        if(voiceAudioSource != null) 
            voiceAudioSource.volume = voiceVolume;
    }
}
