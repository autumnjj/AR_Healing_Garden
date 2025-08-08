using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;

#if UNITY_ANDROID
using UnityEngine.Android;
#endif

public class ARPlantVoiceController : MonoBehaviour
{
    [Header("UI")]
    public Button voiceButton;
    public TextMeshProUGUI targetText;
    public TextMeshProUGUI messageText;

    [Header("목표 문장들")]
    public List<string> positiveTargets = new List<string>
    {
        "힘내", "잘하고 있어", "할 수 있어", "있는 그대로도 완벽해", "사랑해",
        "괜찮아", "예쁘다"
    };

    // 상태 관리
    private bool isListening = false;
    private int currentTargetIndex = 0;
    private List<int> remainingTargets = new List<int>();

    // Android 음성 인식
#if UNITY_ANDROID
    private AndroidJavaObject speechRecognizer;
    private AndroidJavaObject currentActivity;
    private SpeechRecognitionCallback speechCallback;
#endif

    // 이벤트
    public System.Action<string> OnVoiceRecognitionSuccess;

    private void Start()
    {
        if (voiceButton != null)
            voiceButton.onClick.AddListener(StartListening);

        InitializeTargets();
        ShowCurrentTarget();

        // 마이크 권한 확인
        StartCoroutine(CheckAndRequestPermissions());
    }

    private IEnumerator CheckAndRequestPermissions()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            Permission.RequestUserPermission(Permission.Microphone);
            
            // 권한 응답 대기
            float timeout = 0f;
            while (!Permission.HasUserAuthorizedPermission(Permission.Microphone) && timeout < 10f)
            {
                timeout += Time.deltaTime;
                yield return null;
            }
            
            if (Permission.HasUserAuthorizedPermission(Permission.Microphone))
            {
                InitializeAndroidSpeechRecognition();
            }
        }
        else
        {
            InitializeAndroidSpeechRecognition();
        }
#endif
        yield break;
    }

#if UNITY_ANDROID
    private void InitializeAndroidSpeechRecognition()
    {
        try
        {
            AndroidJavaClass unityClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            currentActivity = unityClass.GetStatic<AndroidJavaObject>("currentActivity");

            if (currentActivity == null)
            {
                Debug.LogError("Unity Activity를 가져올 수 없습니다.");
                return;
            }

            AndroidJavaClass speechClass = new AndroidJavaClass("android.speech.SpeechRecognizer");
            bool isAvailable = speechClass.CallStatic<bool>("isRecognitionAvailable", currentActivity);

            if (isAvailable)
            {
                speechRecognizer = speechClass.CallStatic<AndroidJavaObject>("createSpeechRecognizer", currentActivity);

                if (speechRecognizer != null)
                {
                    speechCallback = new SpeechRecognitionCallback(this);
                    speechRecognizer.Call("setRecognitionListener", speechCallback);
                    Debug.Log("Android 음성 인식 초기화 성공");
                }
            }
            else
            {
                Debug.LogWarning("이 기기에서는 음성 인식을 사용할 수 없습니다.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("음성 인식 초기화 실패: " + e.Message);
        }
    }
#endif

    private void OnEnable()
    {
        InitializeTargets();
        ShowCurrentTarget();
    }

    private void InitializeTargets()
    {
        remainingTargets.Clear();
        for (int i = 0; i < positiveTargets.Count; i++)
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
            OnAllTargetsComplete();
            return;
        }

        // 현재 타겟 설정
        currentTargetIndex = remainingTargets[0];

        if (targetText != null)
        {
            targetText.text = $"따라 말해보세요:\n\"{positiveTargets[currentTargetIndex]}\"";
        }

        if (messageText != null)
        {
            string encouragementMessagae = GetEncouragementMessage();
            messageText.text = encouragementMessagae;
        }
    }


    private void StartListening()
    {
        if (isListening) return;

        isListening = true;

        if (voiceButton != null)
        {
            var buttonText = voiceButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
                buttonText.text = "듣는 중...";
        }
#if UNITY_ANDROID && !UNITY_EDITOR
        StartAndroidSpeechRecognition();
#else
        // 에디터에서는 시뮬레이션
        StartCoroutine(SimulateVoiceRecognition());
#endif
    }

#if UNITY_ANDROID
    private void StartAndroidSpeechRecognition()
    {
        if (speechRecognizer == null)
        {
            Debug.LogError("음성 인식기가 초기화되지 않았습니다.");
            OnRecognitionError("초기화 오류");
            return;
        }

        try
        {
            AndroidJavaClass intentClass = new AndroidJavaClass("android.content.Intent");
            AndroidJavaClass recognizerIntentClass = new AndroidJavaClass("android.speech.RecognizerIntent");

            AndroidJavaObject intent = new AndroidJavaObject("android.content.Intent",
                recognizerIntentClass.GetStatic<string>("ACTION_RECOGNIZE_SPEECH"));

            intent.Call<AndroidJavaObject>("putExtra",
                recognizerIntentClass.GetStatic<string>("EXTRA_LANGUAGE_MODEL"),
                recognizerIntentClass.GetStatic<string>("LANGUAGE_MODEL_FREE_FORM"));

            intent.Call<AndroidJavaObject>("putExtra",
                recognizerIntentClass.GetStatic<string>("EXTRA_LANGUAGE"), "ko-KR");

            intent.Call<AndroidJavaObject>("putExtra",
                recognizerIntentClass.GetStatic<string>("EXTRA_MAX_RESULTS"), 5);

            speechRecognizer.Call("startListening", intent);

            Debug.Log("Android 음성 인식 시작");
        }
        catch (System.Exception e)
        {
            Debug.LogError("음성 인식 시작 실패: " + e.Message);
            OnRecognitionError("시작 실패");
        }
    }
#endif



    private IEnumerator SimulateVoiceRecognition()
    {
        // 2초 후 자동으로 현재 목표 문장 인식
        yield return new WaitForSeconds(2f);

        // 성공 처리
        if (isListening)
            OnRecognitionSuccess(positiveTargets[currentTargetIndex]);
    }

    public void OnSpeechRecognitionResult(string result)
    {
        if (!isListening) return;

        Debug.Log("음성 인식 결과: " + result);
        OnRecognitionSuccess(result);
    }

    public void OnRecognitionError(string error)
    {
        Debug.LogError("음성 인식 오류: " + error);

        if (messageText != null)
            messageText.text = "다시 시도해주세요.";

        ResetListeningState();
    }

    private void OnRecognitionSuccess(string recognized)
    {
        isListening = false;

        if (messageText != null)
            messageText.text = "잘했어요! 식물이 기뻐하고 있어요";

        // 성공 알림
        OnVoiceRecognitionSuccess?.Invoke(recognized);

        if (remainingTargets.Count > 0)
            remainingTargets.RemoveAt(0);

        // UI 업데이트
        ResetListeningState();

        StartCoroutine(DelayedNextTarget());
    }

    private void ResetListeningState()
    {
        isListening = false;

        if (voiceButton != null)
        {
            var buttonText = voiceButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
                buttonText.text = "말하기";
        }
    }

    private IEnumerator DelayedNextTarget()
    {
        yield return new WaitForSeconds(1.5f);
        ShowCurrentTarget();
    }

    public void OnAllTargetsComplete()
    {
        if (targetText != null)
            targetText.text = "모든 목표 완료!\n식물이 완전히 성장했어요!";

        if (voiceButton != null)
            voiceButton.interactable = false;

        if (messageText != null)
            messageText.text = "축하해요! 당신의 사랑으로 아름다운 꽃이 피었습니다.";
    }

    // 격려 메시지 생성
    private string GetEncouragementMessage()
    {
        int completed = positiveTargets.Count - remainingTargets.Count;

        string[] messages = {
            "따뜻한 마음으로 말해주세요",
            "식물이 당신의 목소리를 기다리고 있어요",
            "사랑을 담아 천천히 말해보세요",
            "긍정적인 에너지를 전달해주세요",
            "마음을 열고 진심으로 말해주세요",
            "식물도 당신의 따뜻함을 느끼고 있어요",
            "거의 다 왔어요! 조금만 더 힘내세요"
        };

        if (completed == 0)
            return "따뜻한 마음으로 말해주세요";
        else if (completed >= positiveTargets.Count - 2)
            return "거의 다 왔어요! 조금만 더 힘내세요";
        else
            return messages[completed % messages.Length];
    }

    public int GetRemainingTargetsCount()
    {
        return remainingTargets.Count;
    }

    public int GetCompletedTargetsCount()
    {
        return positiveTargets.Count - remainingTargets.Count;
    }

    public bool IsAllComplete()
    {
        return remainingTargets.Count == 0;
    }

    private void OnDestroy()
    {
#if UNITY_ANDROID
        if (speechRecognizer != null)
        {
            try
            {
                speechRecognizer.Call("destroy");
            }
            catch (System.Exception e)
            {
                Debug.LogError("SpeechRecognizer 해제 실패: " + e.Message);
            }
        }
#endif
    }
}

#if UNITY_ANDROID
public class SpeechRecognitionCallback : AndroidJavaProxy
{
    private ARPlantVoiceController voiceController;

    public SpeechRecognitionCallback(ARPlantVoiceController controller) : base("android.speech.RecognitionListener")
    {
        voiceController = controller;
    }

    public void onResults(AndroidJavaObject results)
    {
        try
        {
            AndroidJavaObject arrayList = results.Call<AndroidJavaObject>("getStringArrayList", "results_recognition");
            if (arrayList != null)
            {
                int size = arrayList.Call<int>("size");
                if (size > 0)
                {
                    string result = arrayList.Call<string>("get", 0);
                    voiceController.OnSpeechRecognitionResult(result);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("onResults 처리 중 오류: " + e.Message);
            voiceController.OnRecognitionError("결과 처리 오류");
        }
    }

    public void onError(int errorCode)
    {
        string errorMessage = "음성 인식 오류: " + errorCode;
        voiceController.OnRecognitionError(errorMessage);
    }

    public void onReadyForSpeech(AndroidJavaObject bundle)
    {
        Debug.Log("음성 인식 준비 완료");
    }

    public void onBeginningOfSpeech()
    {
        Debug.Log("음성 입력 시작");
    }

    public void onEndOfSpeech()
    {
        Debug.Log("음성 입력 종료");
    }

    public void onRmsChanged(float rms) { }
    public void onBufferReceived(byte[] buffer) { }
    public void onPartialResults(AndroidJavaObject partialResults) { }
    public void onEvent(int eventType, AndroidJavaObject bundle) { }
}
#endif
