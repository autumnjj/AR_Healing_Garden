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

    [Header("����")]
    public PositiveSpeechData speechData;
    public PlantGrowthData plantGrowthData;

    [Header("Resources ��� ����")]
    [SerializeField]
    private string speechDataResourcePath = "Speech/PositiveSpeechData";

    [Header("��ǥ �ý���")]
    public int requireRepeats = 3;

    // ����
    private bool isListening = false;
    private bool isInitialized = false;
    private string currentTarget = "";
    private int currentCount = 0;
    private int dailyTargetIndex = 0;

    // �Ϸ� ��ǥ ������
    private List<string> dailyTargets = new List<string>
    {
        "���� ������",
        "���ϰ� �־�",
        "����",
        "�������� �ڶ�",
        "�����"
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
        // ���� ��û
        yield return StartCoroutine(RequestPermissions());

        // ���� �ν� �ʱ�ȭ
        InitializeSpeechRecognition();

        // ù ��° ��ǥ �ε�
        LoadNextTarget();
    }

    private IEnumerator RequestPermissions()
    {
        UpdateStatus("������ Ȯ���ϴ� ��...");

#if UNITY_ANDROID
        // ����ũ ���� Ȯ�� �� ��û
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            UpdateStatus("����ũ ������ ��û�մϴ�...");
            Permission.RequestUserPermission(Permission.Microphone);

            // ���� ���� ���
            float timeout = 0f;
            while(!Permission.HasUserAuthorizedPermission(Permission.Microphone) && timeout < 10f)
            {
                timeout += Time.deltaTime;
                yield return null;
            }

            if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
            {
                UpdateStatus("����ũ ������ �ʿ��մϴ�. �������� ������ ������ּ���.");
                yield break;
            }
        }
        UpdateStatus("������ ���εǾ����ϴ�.");
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
    UpdateStatus("�����Ϳ����� �ùķ��̼� ���� �����մϴ�.");
    isInitialized = true;
#endif
    }

#if UNITY_ANDROID
    private void SetupAndroidSpeechRecognition()
    {
        try
        {
            // Unity Activity ��������
            AndroidJavaClass unityClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            currentActivity = unityClass.GetStatic<AndroidJavaObject>("currentActivity");

            if (currentActivity == null)
            {
                UpdateStatus("Unity Activity�� ������ �� �����ϴ�.");
                return;
            }

            // SpeechRecognizer ����
            AndroidJavaClass speechClass = new AndroidJavaClass("android.speech.SpeechRecognizer");

            // SpeechRecognizer�� ��� �������� Ȯ��
            bool isAvailable = speechClass.CallStatic<bool>("isRecognitionAvailable", currentActivity);

            if (isAvailable)
            {
                speechRecognizer = speechClass.CallStatic<AndroidJavaObject>("createSpeechRecognizer", currentActivity);

                if(speechRecognizer != null)
                {
                    // �ݹ� ������ ����
                    speechCallback = new SpeechRecognitionCallback(this);
                    speechRecognizer.Call("setRecognitionListener", speechCallback);

                    isInitialized = true;
                    UpdateStatus("���� �ν� �غ� �Ϸ�");
                    Debug.Log("Android ���� �ν� �ʱ�ȭ ����");
                }
                else
                {
                    UpdateStatus("SpeechRecognizer ������ �����߽��ϴ�.");
                }
            }
            else
            {
                UpdateStatus("�� ��⿡���� ���� �ν��� ����� �� �����ϴ�");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Speech Recognition �ʱ�ȭ ����: " + e.Message);
            UpdateStatus("���� �ν� �ʱ�ȭ ����: " + e.Message);
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
            // ��� ��ǥ �Ϸ�
            UpdateStatus("������ ��ǥ�� ��� �Ϸ��߾��!");
            if (statusText != null)
                statusText.text = "������ ��ǥ�� ��� �Ϸ��߾��!";
        }
    }

    public void ToggleListening()
    {
        if (!isInitialized)
        {
            UpdateStatus("���� �ν��� �ʱ�ȭ���� �ʾҽ��ϴ�.");
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
            UpdateStatus("���� �νıⰡ �غ���� �ʾҽ��ϴ�.");
            return;
        }

        try
        {
            // Intent ����
            AndroidJavaClass intentClass = new AndroidJavaClass("android.content.Intent");
            AndroidJavaClass recognizerIntentClass = new AndroidJavaClass("android.speech.RecognizerIntent");
            
            AndroidJavaObject intent = new AndroidJavaObject("android.content.Intent", 
                recognizerIntentClass.GetStatic<string>("ACTION_RECOGNIZE_SPEECH"));
            
            // ���� �߰�
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

            // ���� �ν� ����
            speechRecognizer.Call("startListening", intent);
            
            isListening = true;
            UpdateStatus("��� �ֽ��ϴ�...");
            UpdateUI();

            Debug.Log("Voice Recog start");
        }
        catch (System.Exception e)
        {
            Debug.LogError("���� �ν� ���� ����: " + e.Message);
            UpdateStatus("���� �ν� ���� ����");
        }

#else
        // �����Ϳ����� �ùķ��̼�
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
                Debug.LogError("���� �ν� ���� ���� : " + e.Message);
            }
        }
#endif
        isListening = false;
        UpdateStatus("���� �ν� ����");
        UpdateUI();
    }

    // �����Ϳ� �ùķ��̼�
    private IEnumerator SimulateVoiceInput()
    {
        isListening = true;
        UpdateStatus("��� �ֽ��ϴ�...(�ùķ��̼�)");
        UpdateUI();

        yield return new WaitForSeconds(2f);

        if (isListening)
            OnSpeechRecognitionResult(currentTarget);
    }

    // ���� �ν� ��� ó��
    public void OnSpeechRecognitionResult(string result)
    {
        if (!isListening) return;

        Debug.Log("���� �ν� ��� : " + result);

        if (recognizedText != null)
            recognizedText.text = "�νĵ� : \"" + result + "\"";

        ProcessVoiceInput(result);
        StopListening();
    }

    public void OnSpeechRecognitionError(string errorCode)
    {
        Debug.LogError("���� �ν� ����: " + errorCode);

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
                return "��Ʈ��ũ �����Դϴ�. ���ͳ� ������ Ȯ�����ּ���.";
            case "ERROR_AUDIO":
                return "����� �����Դϴ�. ����ũ�� Ȯ�����ּ���.";
            case "ERROR_SERVER":
                return "���� �����Դϴ�. ��� �� �ٽ� �õ����ּ���.";
            case "ERROR_CLIENT":
                return "Ŭ���̾�Ʈ �����Դϴ�.";
            case "ERROR_SPEECH_TIMEOUT":
                return "������ �������� ���߽��ϴ�. �ٽ� �õ����ּ���.";
            case "ERROR_NO_MATCH":
                return "������ �ν����� ���߽��ϴ�. �� ��Ȯ�ϰ� �����ּ���.";
            case "ERROR_RECOGNIZER_BUSY":
                return "���� �νıⰡ ��� ���Դϴ�. ��� �� �ٽ� �õ����ּ���.";
            case "ERROR_INSUFFICIENT_PERMISSIONS":
                return "������ �����մϴ�. ����ũ ������ Ȯ�����ּ���.";
            default:
                return "������ �ٽ� �����ּ���.";
        }
    }

    public void OnPartialResult(string partialResult)
    {
        if (recognizedText != null)
            recognizedText.text = "��� ��: \"" + partialResult + "...\"";
    }

    private void ProcessVoiceInput(string input)
    {
        if (speechData == null) return;

        var matchedPhrase = speechData.FindBestMatch(input);

        if (matchedPhrase != null)
        {
            // ������ ��� ������
            AddGrowthPoints(matchedPhrase.growthPoints);

            // ��ǥ ������ ��ġ�ϴ��� Ȯ��
            if (IsTargetMatch(input, currentTarget))
            {
                currentCount++;

                if (currentCount >= requireRepeats)
                {
                    // ��ǥ �Ϸ�
                    CompleteTarget();
                }
                else
                {
                    UpdateStatus("���̿�!" + (requireRepeats - currentCount) + "�� ��!");
                }
            }
            else
            {
                UpdateStatus("�������� ���̿���!");
            }
        }
        else
        {
            UpdateStatus("�� �������� ���� �غ�����");
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
        UpdateStatus("�Ϸ�! \"" + currentTarget + "\"");

        // ���� ��ǥ�� �̵�
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
        // ��ǥ ���� ǥ��
        if (targetText != null)
        {
            if (dailyTargetIndex < dailyTargets.Count)
            {
                targetText.text = "��ǥ: \"" + currentTarget + "\"";
            }
            else
            {
                targetText.text = "��� ��ǥ �Ϸ�!";
            }
        }

        // ����� ǥ��
        if (progressText != null)
        {
            progressText.text = "����: " + currentCount + "/" + requireRepeats + "ȸ";
        }

        // ��ư �ؽ�Ʈ ������Ʈ
        if (listenButton != null)
        {
            TextMeshProUGUI buttonText = listenButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
                buttonText.text = isListening ? "����" : "���� �ν� ����";

            listenButton.interactable = dailyTargetIndex < dailyTargets.Count;
        }
    }

    public bool IsListening()
    {
        return isListening;
    }

    // ���� �Ϸ�(�׽�Ʈ��)
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
                Debug.LogError("SpeechRecognizer destroy ���� : " + e.Message);
            }
        }
#endif
    }
}

#if UNITY_ANDROID 
    // Android �ݹ� Ŭ����
    public class SpeechRecognitionCallback : AndroidJavaProxy
    {
        private UnitySpeechRecognition speechRecognition;

        public SpeechRecognitionCallback(UnitySpeechRecognition recognition) : base("android.speech.RecognitionListener")
        {
            speechRecognition = recognition;
        }

        public void onReadyForSpeech(AndroidJavaObject bundle)
        {
            Debug.Log("���� �ν� �غ� �Ϸ�");
        }

        public void onBeginningOfSpeech()
        {
            Debug.Log("���� �Է� ����");
        }

        public void onRmsChanged(float rms)
        {
            // ���� ���� ��ȭ (�ʿ�� ���)
        }

        public void onBufferReceived(byte[] buffer)
        {
            // ����� ���� (�Ϲ������� ������� ����)
        }

        public void onEndOfSpeech()
        {
            Debug.Log("���� �Է� ����");
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
                Debug.LogError("onResults ó�� �� ����: " + e.Message);
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
            Debug.LogError("onPartialResults ó�� �� Error: " + e.Message);
            }
        }

        public void onEvent(int eventType, AndroidJavaObject bundle)
        {
            // �߰� �̺�Ʈ ó��
        }
    }
#endif
