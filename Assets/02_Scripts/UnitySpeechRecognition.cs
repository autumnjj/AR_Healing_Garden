using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

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
    private bool isInitialized = false;
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
        StartCoroutine(InitializeWithPermissions());
        InitializeSpeechRecognition();
    }

    private IEnumerator InitializeWithPermissions()
    {
        // 권한 요청
        yield return StartCoroutine(RequestPermissions());

        // 음성 인식 초기화
        InitializeSpeechRecognition();

        // 첫 번째 목표 로드
        LoadNextTarget();
    }

    private IEnumerator RequestPermissions()
    {
        UpdateStatus("권한을 확인하는 중...");

#if UNITY_ANDROID
        // 마이크 권한 확인 및 요청
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            UpdateStatus("마이크 권한을 요청합니다...");
            Permission.RequestUserPermission(Permission.Microphone);

            // 권한 응답 대기
            float timeout = 0f;
            while(!Permission.HasUserAuthorizedPermission(Permission.Microphone) && timeout < 10f)
            {
                timeout += Time.deltaTime;
                yield return null;
            }

            if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
            {
                UpdateStatus("마이크 권한이 필요합니다. 설정에서 권한을 허용해주세요.");
                yield break;
            }
        }
        UpdateStatus("권한이 승인되었습니다.");
#endif
        yield return new WaitForSeconds(0.5f);
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
#else
    UpdateStatus("에디터에서는 시뮬레이션 모드로 동작합니다.");
    isInitialized = true;
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

            if (currentActivity == null)
            {
                UpdateStatus("Unity Activity를 가져올 수 없습니다.");
                return;
            }

            // SpeechRecognizer 생성
            AndroidJavaClass speechClass = new AndroidJavaClass("android.speech.SpeechRecognizer");

            // SpeechRecognizer가 사용 가능한지 확인
            bool isAvailable = speechClass.CallStatic<bool>("isRecognitionAvailable", currentActivity);

            if (isAvailable)
            {
                speechRecognizer = speechClass.CallStatic<AndroidJavaObject>("createSpeechRecognizer", currentActivity);

                if(speechRecognizer != null)
                {
                    // 콜백 리스너 생성
                    speechCallback = new SpeechRecognitionCallback(this);
                    speechRecognizer.Call("setRecognitionListener", speechCallback);

                    isInitialized = true;
                    UpdateStatus("음성 인식 준비 완료");
                    Debug.Log("Android 음성 인식 초기화 성공");
                }
                else
                {
                    UpdateStatus("SpeechRecognizer 생성에 실패했습니다.");
                }
            }
            else
            {
                UpdateStatus("이 기기에서는 음성 인식을 사용할 수 없습니다");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Speech Recognition 초기화 실패: " + e.Message);
            UpdateStatus("음성 인식 초기화 실패: " + e.Message);
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
            UpdateStatus("오늘의 목표를 모두 완료했어요!");
            if (statusText != null)
                statusText.text = "오늘의 목표를 모두 완료했어요!";
        }
    }

    public void ToggleListening()
    {
        if (!isInitialized)
        {
            UpdateStatus("음성 인식이 초기화되지 않았습니다.");
            return;
        }

        if (isListening)
            StopListening();
        else
            StartListening();
    }

    private void StartListening()
    {
#if UNITY_ANDROID

        if (speechRecognizer == null || !isInitialized)
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

            intent.Call<AndroidJavaObject>("putExtra",
                recognizerIntentClass.GetStatic<string>("EXTRA_CALLING_PACKAGE"),
                currentActivity.Call<string>("getPackageName"));

            // 음성 인식 시작
            speechRecognizer.Call("startListening", intent);
            
            isListening = true;
            UpdateStatus("듣고 있습니다...");
            UpdateUI();

            Debug.Log("Voice Recog start");
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
                Debug.Log("Voice Recog Stop");
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

        string errorMessage = GetErrorMessage(errorCode);
        UpdateStatus("errorMessage");
        StopListening();
    }
    private string GetErrorMessage(string errorCode)
    {
        switch (errorCode)
        {
            case "ERROR_NETWORK_TIMEOUT":
            case "ERROR_NETWORK":
                return "네트워크 오류입니다. 인터넷 연결을 확인해주세요.";
            case "ERROR_AUDIO":
                return "오디오 오류입니다. 마이크를 확인해주세요.";
            case "ERROR_SERVER":
                return "서버 오류입니다. 잠시 후 다시 시도해주세요.";
            case "ERROR_CLIENT":
                return "클라이언트 오류입니다.";
            case "ERROR_SPEECH_TIMEOUT":
                return "음성을 감지하지 못했습니다. 다시 시도해주세요.";
            case "ERROR_NO_MATCH":
                return "음성을 인식하지 못했습니다. 더 명확하게 말해주세요.";
            case "ERROR_RECOGNIZER_BUSY":
                return "음성 인식기가 사용 중입니다. 잠시 후 다시 시도해주세요.";
            case "ERROR_INSUFFICIENT_PERMISSIONS":
                return "권한이 부족합니다. 마이크 권한을 확인해주세요.";
            default:
                return "음성을 다시 말해주세요.";
        }
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

    public bool IsListening()
    {
        return isListening;
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
            try
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
            catch (System.Exception e)
            {
                Debug.LogError("onResults 처리 중 오류: " + e.Message);
                speechRecognition.OnSpeechRecognitionError("RESULT_PROCESSING_ERROR");
            }
        }   

        public void onPartialResults(AndroidJavaObject partialResults)
        {
            try
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
            catch (System.Exception e)
            {
            Debug.LogError("onPartialResults 처리 중 Error: " + e.Message);
            }
        }

        public void onEvent(int eventType, AndroidJavaObject bundle)
        {
            // 추가 이벤트 처리
        }
    }
#endif
