using UnityEngine;
using TMPro;
using System.Collections;

[System.Serializable]
public class DialogueLine
{
    [TextArea(2, 4)]
    public string text;
    public AudioClip voiceClip;

    [Header("타이밍 설정")]
    public float startDelay = 0f;
    public float typingSpeed = 0.04f;
    public bool waitForAudioComplete = true;
    public float pauseAfter = 1f;

    [Header("고급 설정")]
    public float audioStartOffset = 0f;
    public float textCompletionRatio = 0.7f;
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
    private bool isTyping = false;
    private bool isAudioPlaying = false;
    private Coroutine dialogueCoroutine;
    private Coroutine typingCoroutine;
    private Coroutine audioCoroutine;

    // 이벤트
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
                text = "안녕하세요!\n 저는 Blee예요. BloomSpeak 가이드 친구랍니다!",
                voiceClip = line1Audio,
                startDelay = 0.2f,
                textCompletionRatio = 0.75f,
                pauseAfter = 0.5f,
                audioStartOffset = 0f,
                waitForAudioComplete = true
            },
            new DialogueLine
            {
                text = "여기서는 AR로 나만의 작은 치유 공간을 만들고, 따뜻한 말 한마디로 식물과 함께 성장할 수 있어요!",
                voiceClip = line2Audio,
                startDelay = 0.1f,
                textCompletionRatio = 0.8f,
                pauseAfter = 0.5f,
                audioStartOffset = 0f,
                waitForAudioComplete = true
            },
            new DialogueLine
            {
                text = "3가지 방법으로 BloomSpeak를 즐길 수 있어요",
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
        if (isPlaying) 
        {
            SkipToEnd();
            return;
        }

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

            if (line.startDelay > 0f)
            {
                yield return new WaitForSeconds(line.startDelay);
            }

            yield return StartCoroutine(PlayLineWithSync(line));

            OnLineComplete?.Invoke(i);

            // 다음 라인으로 넘어가기 전 대기
            if (i < dialogueLines.Length - 1 && line.pauseAfter > 0)
            {
                yield return new WaitForSeconds(line.pauseAfter);
            }
        }

        // 대화 완료
        CompleteDialogue();
    }

    private IEnumerator PlayLineWithSync(DialogueLine line)
    {
        // 1. 오디오와 텍스트 동시 시작
        audioCoroutine = StartCoroutine(PlayAudio(line));
        typingCoroutine = StartCoroutine(TypeTextSynced(line));

        // 2. 핵심 : 오디오 완료까지 반드시 대기
        if (line.waitForAudioComplete && line.voiceClip != null)
        {
            // 오디오 완료까지 대기
            yield return audioCoroutine;
        }

        // 3. 텍스트 타이핑이 아직 진행 중이면 완료 대기
        if (isTyping)
        {
            yield return typingCoroutine;
        }
    }

    private IEnumerator PlayAudio(DialogueLine line)
    {
        if (line.voiceClip == null) yield break;

        // 오디오 시작 오프셋 적용
        if (line.audioStartOffset > 0)
            yield return new WaitForSeconds(line.audioStartOffset);

        if (voiceAudioSource != null)
        {
            isAudioPlaying = true;
            voiceAudioSource.clip = line.voiceClip;
            voiceAudioSource.Play();

            // 실제 오디오 완료까지 정확히 대기
            float audioLength = line.voiceClip.length;
            yield return new WaitForSeconds(audioLength);

            isAudioPlaying = false;

            // 오디오 완료 이벤트 발생
            OnAudioComplete?.Invoke(currentLineIndex);
        }
    }

    private IEnumerator TypeTextSynced(DialogueLine line)
    {
        if (dialogueText == null) yield break;

        isTyping = true;
        dialogueText.text = "";

        // 텍스트 완료 타이밍을 오디오 길이에 맞춰 조정
        float targetDuration;
        if(line.voiceClip != null)
        {
            targetDuration = line.voiceClip.length * line.textCompletionRatio;
        }
        else
        {
            targetDuration = line.text.Length * 0.05f;
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
        if (text.Contains("안녕")) return "greeting";
        if (text.Contains("마법")) return "excited";
        if (text.Contains("터치") || text.Contains("상호작용")) return "curious";
        return "happy";
    }

    private void CompleteDialogue()
    {
        isPlaying = false;
        isTyping = false;
        isAudioPlaying = false;

        OnDialogueComplete?.Invoke();
    }

    public void SkipToEnd()
    {
        if (dialogueCoroutine != null) StopCoroutine(dialogueCoroutine);
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        if(audioCoroutine != null) StopCoroutine(audioCoroutine);

        // 음성 정지
        if (voiceAudioSource != null && voiceAudioSource.isPlaying)
            voiceAudioSource.Stop();

        if (dialogueLines.Length > 0 && dialogueText != null)
            dialogueText.text = dialogueLines[dialogueLines.Length - 1].text;

        CompleteDialogue();
    }

    // 공개 메서드들
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
