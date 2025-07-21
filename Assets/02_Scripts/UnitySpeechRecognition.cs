using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Data;


#if UNITY_ANDROID
using UnityEngine.Android;
#endif

public class UnitySpeechRecognition : MonoBehaviour
{
    [Header("UI")]
    public Button listenButton;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI targetText;
    public TextMeshProUGUI progressText;
    public TextMeshProUGUI recognizedText;

    [Header("설정")]
    public PositiveSpeechData speechData;
    public PlantGrowthData plantGrowthData;

    [Header("Resources 경로 설정")]
    [SerializeField]
    private string speechDataResourcePath = "Speech/PositiveSpeechData";

    [Header("목표 시스템")]
    public int requireRepeats = 3;

    // 상태
    private bool isListening = false;
    private string currentTarget = "";
    private int currentCount = 0;
    private int dailyTargetIndex = 0;

    // 하루 목표 문구들
    private List<string> dailyTargets = new List<string>
    {
        "나는 소중해",
        "잘하고 있어",
        "고마워",
        "무럭무럭 자라",
        "사랑해"
    };

#if UNITY_ANDROID
    private AndroidJavaObject speechRecognizer;
    private AndroidJavaObject currentActivity;
    private SpeechRecognitionCallback speechCallback;
#endif

    private void Start()
    {
        LoadDataFromResources();
        SetupUI();
        LoadNextTarget();
        InitializeSpeechRecognition();
    }

    private void LoadDataFromResources()
    {
        if(speechData == null)
        {
            speechData = Resources.Load<PositiveSpeechData>(speechDataResourcePath);
            if(speechData == null)
            {
                Debug.LogError($"Resources/{speechDataResourcePath} cannot find PositiveSpeechData");
            }
            else
            {
                Debug.Log("Resources folder load PositiveSpeechData");
            }
        }
    }
    private void SetupUI()
    {
        if (listenButton != null)
            listenButton.onClick.AddListener(ToggleListening);

        UpdateUI();
    }

    private void InitializeSpeechRecognition()
    {
#if UNITY_ANDROID 
    SetupAndroidSpeechRecognition();
#endif
    }

#if UNITY_ANDROID
    private void SetupAndroidSpeechRecognition()
    {
        try
        {
            // Unity Activity 가져오기
            AndroidJavaClass unityClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            currentActivity = unityClass.GetStatic<AndroidJavaObject>("currentActivity");

            // SpeechRecognizer 생성
            AndroidJavaClass speechClass = new AndroidJavaClass("android.speech.SpeechRecognizer");

            // SpeechRecognizer가 사용 가능한지 확인
            bool isAvailable = speechClass.CallStatic<bool>("isRecognitionAvailable", currentActivity);

            if (isAvailable)
            {
                speechRecognizer = speechClass.CallStatic<AndroidJavaObject>("createSpeechRecognizer", currentActivity);

                // 콜백 리스너 생성
                speechCallback = new SpeechRecognitionCallback(this);
                speechRecognizer.Call("setRecognitionListener", speechCallback);

                UpdateStatus("음성 인식 준비 완료");
            }
            else
            {
                UpdateStatus("음성 인식을 사용할 수 없습니다");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Speech Recognition 초기화 실패: " + e.Message);
            UpdateStatus("음성 인식 초기화 실패");
        }
    }
#endif

    private void LoadNextTarget()
    {
        if (dailyTargetIndex < dailyTargets.Count)
        {
            currentTarget = dailyTargets[dailyTargetIndex];
            currentCount = 0;
            UpdateUI();
        }
        else
        {
            // 모든 목표 완료
            if (statusText != null)
                statusText.text = "오늘의 목표를 모두 완료했어요!";
        }
    }

    public void ToggleListening()
    {
        if (isListening)
            StopListening();
        else
            StartListening();
    }

    private void StartListening()
    {
#if UNITY_ANDROID

        if (speechRecognizer == null)
        {
            UpdateStatus("음성 인식기가 준비되지 않았습니다.");
            return;
        }

        try
        {
            // Intent 생성
            AndroidJavaClass intentClass = new AndroidJavaClass("android.content.Intent");
            AndroidJavaClass recognizerIntentClass = new AndroidJavaClass("android.speech.RecognizerIntent");
            
            AndroidJavaObject intent = new AndroidJavaObject("android.content.Intent", 
                recognizerIntentClass.GetStatic<string>("ACTION_RECOGNIZE_SPEECH"));
            
            // 설정 추가
            intent.Call<AndroidJavaObject>("putExtra", 
                recognizerIntentClass.GetStatic<string>("EXTRA_LANGUAGE_MODEL"),
                recognizerIntentClass.GetStatic<string>("LANGUAGE_MODEL_FREE_FORM"));
            
            intent.Call<AndroidJavaObject>("putExtra", 
                recognizerIntentClass.GetStatic<string>("EXTRA_LANGUAGE"), "ko-KR");
            
            intent.Call<AndroidJavaObject>("putExtra", 
                recognizerIntentClass.GetStatic<string>("EXTRA_MAX_RESULTS"), 5);
            
            intent.Call<AndroidJavaObject>("putExtra", 
                recognizerIntentClass.GetStatic<string>("EXTRA_PARTIAL_RESULTS"), true);
            
            // 음성 인식 시작
            speechRecognizer.Call("startListening", intent);
            
            isListening = true;
            UpdateStatus("듣고 있습니다... ??");
            UpdateUI();
        }
        catch (System.Exception e)
        {
            Debug.LogError("음성 인식 시작 실패: " + e.Message);
            UpdateStatus("음성 인식 시작 실패");
        }

#else
        // 에디터에서는 시뮬레이션
        StartCoroutine(SimulateVoiceInput());
#endif
    }

    private void StopListening()
    {
#if UNITY_ANDROID 
        if(speechRecognizer != null)
        {
            try
            {
                speechRecognizer.Call("stopListening");
            }
            catch(System.Exception e)
            {
                Debug.LogError("음성 인식 중지 실패 : " + e.Message);
            }
        }
#endif
        isListening = false;
        UpdateStatus("음성 인식 중지");
        UpdateUI();
    }

    // 에디터용 시뮬레이션
    private IEnumerator SimulateVoiceInput()
    {
        isListening = true;
        UpdateStatus("듣고 있습니다...(시뮬레이션)");
        UpdateUI();

        yield return new WaitForSeconds(2f);

        if (isListening)
            OnSpeechRecognitionResult(currentTarget);
    }

    // 음성 인식 결과 처리
    public void OnSpeechRecognitionResult(string result)
    {
        if (!isListening) return;

        Debug.Log("음성 인식 결과 : " + result);

        if (recognizedText != null)
            recognizedText.text = "인식됨 : \"" + result + "\"";

        ProcessVoiceInput(result);
        StopListening();
    }

    public void OnSpeechRecognitionError(string errorCode)
    {
        Debug.LogError("음성 인식 오류: " + errorCode);
        UpdateStatus("음성을 다시 말해주세요");
        StopListening();
    }

    public void OnPartialResult(string partialResult)
    {
        if (recognizedText != null)
            recognizedText.text = "듣는 중: \"" + partialResult + "...\"";
    }

    private void ProcessVoiceInput(string input)
    {
        if (speechData == null) return;

        var matchedPhrase = speechData.FindBestMatch(input);

        if (matchedPhrase != null)
        {
            // 긍정적 언어 감지됨
            AddGrowthPoints(matchedPhrase.growthPoints);

            // 목표 문구와 일치하는지 확인
            if (IsTargetMatch(input, currentTarget))
            {
                currentCount++;

                if (currentCount >= requireRepeats)
                {
                    // 목표 완료
                    CompleteTarget();
                }
                else
                {
                    UpdateStatus("좋이요!" + (requireRepeats - currentCount) + "번 더!");
                }
            }
            else
            {
                UpdateStatus("긍정적인 말이에요!");
            }
        }
        else
        {
            UpdateStatus("더 긍정적인 말을 해보세요");
        }

        UpdateUI();
    }

    private bool IsTargetMatch(string input, string target)
    {
        string inputLower = input.ToLower().Replace(" ", "");
        string targetLower = target.ToLower().Replace(" ", "");

        return inputLower.Contains(targetLower) || targetLower.Contains(inputLower);
    }

    private void CompleteTarget()
    {
        UpdateStatus("완료! \"" + currentTarget + "\"");

        // 다음 목표로 이동
        dailyTargetIndex++;

        StartCoroutine(DelayedNextTarget());
    }

    private IEnumerator DelayedNextTarget()
    {
        yield return new WaitForSeconds(2f);
        LoadNextTarget();
    }

    private void AddGrowthPoints(float points)
    {
        if (plantGrowthData != null)
        {
            plantGrowthData.AddGrowthPoints(points, InteractionType.PositiveTalk);
        }
    }

    private void UpdateStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;

        Debug.Log("Speech Status: " + message);
    }

    private void UpdateUI()
    {
        // 목표 문구 표시
        if (targetText != null)
        {
            if (dailyTargetIndex < dailyTargets.Count)
            {
                targetText.text = "목표: \"" + currentTarget + "\"";
            }
            else
            {
                targetText.text = "모든 목표 완료!";
            }
        }

        // 진행률 표시
        if (progressText != null)
        {
            progressText.text = "진행: " + currentCount + "/" + requireRepeats + "회";
        }

        // 버튼 텍스트 업데이트
        if (listenButton != null)
        {
            TextMeshProUGUI buttonText = listenButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
                buttonText.text = isListening ? "중지" : "음성 인식 시작";

            listenButton.interactable = dailyTargetIndex < dailyTargets.Count;
        }
    }

    // 수동 완료(테스트용)
    public void ManualComplete()
    {
        if (!string.IsNullOrEmpty(currentTarget))
            OnSpeechRecognitionResult(currentTarget);
    }

    private void OnDestroy()
    {
#if UNITY_ANDROID
        if (speechRecognizer != null)
        {
            try
            {
                speechRecognizer.Call("destory");
            }
            catch(System.Exception e)
            {
                Debug.LogError("SpeechRecognizer destroy 실패 : " + e.Message);
            }
        }
#endif
    }
}

#if UNITY_ANDROID 
    // Android 콜백 클래스
    public class SpeechRecognitionCallback : AndroidJavaProxy
    {
        private UnitySpeechRecognition speechRecognition;

        public SpeechRecognitionCallback(UnitySpeechRecognition recognition) : base("android.speech.RecognitionListener")
        {
            speechRecognition = recognition;
        }

        public void onReadyForSpeech(AndroidJavaObject bundle)
        {
            Debug.Log("음성 인식 준비 완료");
        }

        public void onBeginningOfSpeech()
        {
            Debug.Log("음성 입력 시작");
        }

        public void onRmsChanged(float rms)
        {
            // 음성 레벨 변화 (필요시 사용)
        }

        public void onBufferReceived(byte[] buffer)
        {
            // 오디오 버퍼 (일반적으로 사용하지 않음)
        }

        public void onEndOfSpeech()
        {
            Debug.Log("음성 입력 종료");
        }

        public void onError(int errorCode)
        {
            string[] errorMessages = {
            "ERROR_NETWORK_TIMEOUT", "ERROR_NETWORK", "ERROR_AUDIO", "ERROR_SERVER",
            "ERROR_CLIENT", "ERROR_SPEECH_TIMEOUT", "ERROR_NO_MATCH", "ERROR_RECOGNIZER_BUSY",
            "ERROR_INSUFFICIENT_PERMISSIONS"
        };

            string errorMessage = errorCode < errorMessages.Length ? errorMessages[errorCode] : "UNKNOWN_ERROR";
            speechRecognition.OnSpeechRecognitionError(errorMessage);
        }

        public void onResults(AndroidJavaObject results)
        {
            AndroidJavaObject arrayList = results.Call<AndroidJavaObject>("getStringArrayList", "results_recognition");
            if (arrayList != null)
            {
                int size = arrayList.Call<int>("size");
                if (size > 0)
                {
                    string result = arrayList.Call<string>("get", 0);
                    speechRecognition.OnSpeechRecognitionResult(result);
                }
            }
        }

        public void onPartialResults(AndroidJavaObject partialResults)
        {
            AndroidJavaObject arrayList = partialResults.Call<AndroidJavaObject>("getStringArrayList", "results_recognition");
            if (arrayList != null)
            {
                int size = arrayList.Call<int>("size");
                if (size > 0)
                {
                    string result = arrayList.Call<string>("get", 0);
                    speechRecognition.OnPartialResult(result);
                }
            }
        }

        public void onEvent(int eventType, AndroidJavaObject bundle)
        {
            // 추가 이벤트 처리
        }
    }
#endif
