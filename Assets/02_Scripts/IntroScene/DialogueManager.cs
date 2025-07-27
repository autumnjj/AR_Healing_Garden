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
    [Header("음성 설정")]
    public AudioSource voiceAudioSource;
    public float voiceVolume = 0.8f;

    [Header("대화 내용")]
    public DialogueLine[] dialogueLines;

    private TextMeshProUGUI dialogueText;

    // 상태
    private int currentLineIndex = 0;
    private bool isPlaying = false;
    private Coroutine dialogueCoroutine;

    // 이벤트
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
                text = "안녕하세요!\n 저는 Blee예요. BloomSpeak 가이드 친구랍니다!",
                voiceClip = line1Audio,
                typingSpeed = 0.05f,
                pauseAfter = 2.5f
            },
            new DialogueLine
            {
                text = "여기서는 AR로 나만의 작은 치유 공간을 만들고, 따뜻한 말 한마디로 식물과 함께 성장할 수 있어요!",
                voiceClip = line2Audio,
                typingSpeed = 0.04f,
                pauseAfter = 3f
            },
            new DialogueLine
            {
                text = " 여기서는 AR로 나만의 작은 치유 공간을 만들고, 따뜻한 말 한마디로 식물과 함께 성장할 수 있어",
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

            // 참새 애니메이션 트리거
            OnLineStart?.Invoke(GetEmotionFromText(line.text));

            // 음성과 타이핑 동시 시작
            if(line.voiceClip != null) 
                PlayVoice(line.voiceClip);

            yield return StartCoroutine(TypeText(line.text, line.typingSpeed));

            // 다음 라인으로 넘어가기 전 대기
            if (i < dialogueLines.Length - 1)
            {
                yield return new WaitForSeconds(line.pauseAfter);
            }
        }

        // 대화 완료
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
        if (text.Contains("안녕")) return "greeting";
        if (text.Contains("마법")) return "excited";
        if (text.Contains("터치") || text.Contains("상호작용")) return "curious";
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

    // 공개 메서드들
    public bool IsPlaying() => isPlaying;
    public void SetVoiceVolume(float volume)
    {
        voiceVolume = Mathf.Clamp01(volume);
        if(voiceAudioSource != null) 
            voiceAudioSource.volume = voiceVolume;
    }
}
