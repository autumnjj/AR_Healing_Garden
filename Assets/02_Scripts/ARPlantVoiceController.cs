using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.UIElements;


#if UNITY_ANDROID
using UnityEngine.Android;
#endif

public class ARPlantVoiceController : MonoBehaviour
{
    [Header("UI")]
    public UnityEngine.UI.Button voiceButton;
    public TextMeshProUGUI targetText;
    public TextMeshProUGUI messageText;
    private UnityEngine.UI.Image buttonImage;

    [Header("Recognition Settings")]
    [SerializeField] private bool useSpeechRecognition = true;
    [SerializeField] private int maxSpeechAttempts = 3;
    [SerializeField] private float speechTimeout = 4f;
    [SerializeField] private float standardGrowthPoints = 15f;

    [Header("Volume Detection Settings (FallBack)")]
    [SerializeField] private float volumeThreshold = 0.01f;
    [SerializeField] private float minSpeakTime = 0.5f;
    [SerializeField] private float recordingTime = 3f;

    [Header("User Messages")]
    public string[] encouragementMessages =
    {
        "잘 안들렸어요. 다시 말해보세요!",
        "좀 더 또렷하게 말해보세요!",
        "더 크게 말해보세요!"
    };
    public string[] successMessages =
    {
        "훌륭해요! 식물이 기뻐해요!",
        "잘했어요! 식물이 자라고 있어요!",
        "완벽해요! 식물이 사랑을 느꼈어요!"
    };

    [Header("긍정 문장들")]
    public List<PositiveKeyword> positiveKeywords = new List<PositiveKeyword>();

    [System.Serializable]
    public class PositiveKeyword
    {
        public string keyword;
        public List<string> variations = new List<string>();
    }

    // 현재 상태
    private int currentTargetIndex = 0;
    private List<int> remainingTargets = new List<int>();
    private int currentAttemptCount = 0;
    private bool isRecognizing = false;

    // 마이크 관련
    private AudioClip microphoneClip;
    private string microphoneDevice;
    private float[] samples;
    private float currentVolume = 0f;
    private float speakingTime = 0f;

    // Android Speech Recognition
    private AndroidJavaObject speechRecognizer;
    private AndroidJavaObject unityActivity;

    // 통계(백그라운드 수집)
    private Dictionary<string, int> speechSuccessCount = new Dictionary<string, int>();
    private Dictionary<string, int> totalAttemptCount = new Dictionary<string, int>();

    // 이벤트
    public System.Action<string, float, string> OnRecognitionSuccess;

    private void Start()
    {
        SetupKeywords();
        InitializeComponents();
        InitializeTargets();
        ShowCurrentTarget();

        StartCoroutine(CheckPermission());
    }

    private void SetupKeywords()
    {
        positiveKeywords = new List<PositiveKeyword>()
        {
            new PositiveKeyword
            {
                keyword = "사랑해",
                variations = new List<string> {"사랑한다", "사랑해요", "럽유"}
                
            },
            new PositiveKeyword
            {
                keyword = "예쁘다",
                variations = new List<string> {"예쁘다", "예뻐요", "이뻐", "아름다워" }
                
            },
            new PositiveKeyword
            {
                keyword = "잘하고 있어",
                variations = new List<string> {"잘했어", "잘해", "잘했다", "좋아"}
                
            },
            new PositiveKeyword
            {
                keyword = "힘내",
                variations = new List<string> {"화이팅", "파이팅", "힘내요" }
                
            },
            new PositiveKeyword
            {
                keyword = "고마워",
                variations = new List<string> {"고맙다", "감사해", "감사합니다" }
                
            },
            new PositiveKeyword
            {
                keyword = "괜찮아",
                variations = new List<string> { "괜찮다", "문제없어" }
                
            },
            new PositiveKeyword
            {
                keyword = "대단해",
                variations = new List<string> { "대단하다", "훌륭해", "멋져" }
                
            }
        };

        // 통계 초기화
        foreach (var keyword in positiveKeywords)
        {
            speechSuccessCount[keyword.keyword] = 0;
            totalAttemptCount[keyword.keyword] = 0;
        }
    }

    private void InitializeComponents()
    {
        if (voiceButton != null)
        {
            voiceButton.onClick.AddListener(StartRecognition);
        }
#if UNITY_ANDROID && !Unity_Editor
        InitializeAndroidSpeechRecognizer();
#endif
        CheckMicrophoneDevices();
    }

    private IEnumerator CheckPermission()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            Debug.Log("Requesting microphone permission...");
            ShowMessage("마이크 접근 권한이 필요해요", Color.yellow);
                
            Permission.RequestUserPermission(Permission.Microphone);
            
            float timeout = 0f;
            while (!Permission.HasUserAuthorizedPermission(Permission.Microphone) && timeout < 10f)
            {
                timeout += Time.deltaTime;
                yield return null;
            }
            
            if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
            {
               ShowMessage("마이크 권한이 필요합니다. 설정에서 권한을 허용해주세요", Color.red);
                yield break;
            }
            else
            {
                ShowMessage("마이크 준비 완료! 식물과 대화해보세요."), Color.green);
            }
        }

#endif

        // 마이크 장치 확인
        CheckMicrophoneDevices();
        yield return null;
    }


    private void InitializeAndroidSpeechRecognizer()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            unityActivity = new AndroidJavaClass("com.unity3d.player.UnityPlayer")
                .GetStatic<AndroidJavaObject>("currentActivity");
            Debug.Log("Android Speech Recognition 초기화 성공");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Android Speech Recognition 초기화 실패: {e.Message}");
            useSpeechRecognition = false; // 폴백
        }
    
#endif
    }

    private void CheckMicrophoneDevices()
    {
        if (Microphone.devices.Length > 0)
        {
            microphoneDevice = Microphone.devices[0];
            Debug.Log($"Mic : {microphoneDevice}");
        }
        else
        {
            Debug.LogError("There is no Mic device");
            useSpeechRecognition = false;
        }
    }

    private void InitializeTargets()
    {
        remainingTargets.Clear();
        for (int i = 0; i < positiveKeywords.Count; i++)
            remainingTargets.Add(i);

        for (int i = 0; i < remainingTargets.Count; i++)
        {
            int temp = remainingTargets[i];
            int randomIndex = Random.Range(i, remainingTargets.Count);
            remainingTargets[i] = remainingTargets[randomIndex];
            remainingTargets[randomIndex] = temp;
        }
    }

    private void ShowCurrentTarget()
    {
        if (remainingTargets.Count == 0)
        {
            // 모든 문장을 다 사용했으면 완료
            OnAllComplete();
            return;
        }

        // 현재 타겟 설정
        currentTargetIndex = remainingTargets[0];
        currentAttemptCount = 0;

        var currentKeyword = positiveKeywords[currentTargetIndex];

        if (targetText != null)
        {
           targetText.text = $"따라 말해보세요:\n\"{currentKeyword.keyword}\"";
        }

        ShowMessage("마이크 버튼을 눌러 말해보세요!", Color.white);
    }


    public void StartRecognition()
    {
        if (!isRecognizing) return;

        var currentKeyword = positiveKeywords[remainingTargets[0]];
        totalAttemptCount[currentKeyword.keyword]++;

        // 3번 시도했거나 음성인식을 사용하지 않는 경우 -> 발화 감지
        if (currentAttemptCount >= maxSpeechAttempts || !useSpeechRecognition)
        {
            StartCoroutine(VolumeDetectionMode(currentKeyword));
        }
        else
        {
            StartCoroutine(SpeechRecognitionMode(currentKeyword));
        }
    }

    private IEnumerator SpeechRecognitionMode(PositiveKeyword targetKeyword)
    {
        isRecognizing = true;
        currentAttemptCount++;

        ShowMessage("듣고 있어요...", Color.green);
        ChangeButtonColor(Color.green);

        // 기본값으로 실패 처리
        string recognizedText = "";

#if UNITY_ANDROID && !UNITY_EDITOR
        // Android 실기기에서만 실제 음성 인식
        yield return StartCoroutine(AndroidSpeechRecognitionCoroutine((result) => recognizedText = result));
#else
        // 에디터에서는 간단히 대기만 하고 실패 처리
        yield return new WaitForSeconds(speechTimeout);
        Debug.Log("[VoiceRecognizer] 에디터 모드 - 음성 인식 스킵");
#endif

        if (IsKeywordMatched(recognizedText, targetKeyword))
        {
            // 음성 인식 성공!
            speechSuccessCount[targetKeyword.keyword]++;
            OnSuccess(targetKeyword, "speech");
        }
        else
        {
            // 실패 - 자연스럽게 재시도 유도
            OnSpeechRecognitionFailed();
        }

        ChangeButtonColor(Color.white);
        isRecognizing = false;
    }

    private IEnumerator VolumeDetectionMode(PositiveKeyword targetKeyword)
    {
        isRecognizing = true;

        ShowMessage("목소리를 듣고 있어요...", Color.green);
        ChangeButtonColor(Color.green);

        bool volumeSuccess = false;
        yield return StartCoroutine(VolumeDetectionCoroutine((result) => volumeSuccess = result));

        if (volumeSuccess)
        {
            OnSuccess(targetKeyword, "volume");
        }
        else
        {
            ShowMessage("조금 더 크게 말해보세요!", Color.orange);
        }

        ChangeButtonColor(Color.white);
        isRecognizing = false;
    }


#if UNITY_ANDROID && !UNITY_EDITOR
    private IEnumerator AndroidSpeechRecognitionCoroutine(System.Action<string> callback)
    {
        try
        {
            AndroidJavaObject intent = new AndroidJavaObject("android.content.Intent", 
                "android.speech.action.RECOGNIZE_SPEECH");
            
            intent.Call<AndroidJavaObject>("putExtra", 
                "android.speech.extra.LANGUAGE_MODEL", "free_form");
            intent.Call<AndroidJavaObject>("putExtra", 
                "android.speech.extra.LANGUAGE", "ko-KR");
            intent.Call<AndroidJavaObject>("putExtra", 
                "android.speech.extra.PARTIAL_RESULTS", true);

            yield return new WaitForSeconds(speechTimeout);
            
            // 실제 구현에서는 콜백을 통해 결과를 받아야 합니다
            // 임시로 빈 문자열 반환
            callback("");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[VoiceRecognizer] 음성 인식 오류: {e.Message}");
            callback("");
        }
    }
#endif

    private IEnumerator VolumeDetectionCoroutine(System.Action<bool> callback)
    {
        speakingTime = 0f;
        currentVolume = 0f;

        if (string.IsNullOrEmpty(microphoneDevice))
        {
            callback(false);
            yield break;
        }

        microphoneClip = Microphone.Start(microphoneDevice, false, (int)recordingTime, 44100);

        float recordingTimer = 0f;
        bool voiceDetected = false;

        while(recordingTimer < recordingTime)
        {
            recordingTimer += Time.deltaTime;
            CheckMicrophoneVolume();

            if (currentVolume > volumeThreshold)
            {
                speakingTime += Time.deltaTime;
                voiceDetected = true;
            }

            yield return null;
        }
        Microphone.End(microphoneDevice);

        // 결과를 콜백으로 전달
        bool result = voiceDetected && speakingTime >= minSpeakTime;
        callback(result);
    }

    private void CheckMicrophoneVolume()
    {
        if (microphoneClip == null || string.IsNullOrEmpty(microphoneDevice)) return;
       
        int micPosition = Microphone.GetPosition(microphoneDevice);
        if (micPosition <= 0) return;

        samples = new float[128];
        int startPosition = Mathf.Max(0, micPosition - 128);
        microphoneClip.GetData(samples, startPosition);

        // RMS (Root Mean Square) 계산으로 볼륨 측정
        float sum = 0f;
        for (int i = 0; i < samples.Length; i++)
        {
            sum += samples[i] * samples[i];
        }
        currentVolume = Mathf.Sqrt(sum / samples.Length);
    }

    private bool IsKeywordMatched(string recognizedText, PositiveKeyword targetKeyword)
    {
        if (string.IsNullOrEmpty(recognizedText)) return false;

        recognizedText = recognizedText.ToLower().Trim();

        // 정학히 일치하거나 포함
        if (recognizedText.Contains(targetKeyword.keyword.ToLower())) return true;

        // 변형들과 매칭
        foreach(var variation in targetKeyword.variations)
        {
            if (recognizedText.Contains(variation.ToLower())) return true;
        }

        return false;
    }

    private void OnSpeechRecognitionFailed()
    {
        string encouragement = encouragementMessages[Random.Range(0, encouragementMessages.Length)];
        ShowMessage(encouragement, Color.orange);

        Debug.Log($"[VoiceRecognizer] Voice Rec Failed - Try {currentAttemptCount}/{maxSpeechAttempts}");

        if (currentAttemptCount >= maxSpeechAttempts)
        {
            Debug.Log("[VoiceRecognizer] Next turn 발화 감지 mode");
        }
    }

    private void OnSuccess(PositiveKeyword keyword, string method)
    {
        string successMessage = successMessages[Random.Range(0, successMessages.Length)];
        ShowMessage(successMessage, Color.green);

        Debug.Log($"[VoiceRecognizer] 성공! 키워드: {keyword.keyword}, 방법: {method}, 시도: {currentAttemptCount}");

        // 성공 이벤트 발생
        OnRecognitionSuccess?.Invoke(keyword.keyword, standardGrowthPoints, method);


        // 다음 타겟으로
        remainingTargets.RemoveAt(0);
        StartCoroutine(DelayedNextTarget());
    }

    private void ShowMessage(string message, Color color)
    {
        if (messageText != null)
        {
            messageText.text = message;
            messageText.color = color;
        }
    }

    private void ChangeButtonColor(Color color)
    {
        if (voiceButton != null)
        {
            var buttonImageg = voiceButton.GetComponent<UnityEngine.UI.Image>();
            if (buttonImageg != null)
                buttonImage.color = color;  
        }
    }

    private IEnumerator DelayedNextTarget()
    {
        yield return new WaitForSeconds(2f);
        ShowCurrentTarget();
    }

    public void OnAllComplete()
    {
        if (targetText != null)
            targetText.text = "모든 응원 완료!";

        ShowMessage("축하해요! 식물이 당신의 사랑으로 가득 자랐어요!", Color.gold);

        if (voiceButton != null)
            voiceButton.interactable = false;
    }

    public void SetSpeechRecognition(bool enabled)
    {
        useSpeechRecognition = enabled;
    }
    
    public int GetRemainingCount()
    {
        return remainingTargets.Count;
    }

    public bool IsAllComplete()
    {
        return remainingTargets.Count == 0;
    }
}


