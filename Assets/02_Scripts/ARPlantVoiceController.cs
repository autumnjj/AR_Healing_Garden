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

    [Header("��ǥ �����")]
    public List<string> positiveTargets = new List<string>
    {
        "����", "���ϰ� �־�", "�� �� �־�", "�ִ� �״�ε� �Ϻ���", "�����",
        "������", "���ڴ�"
    };

    // ���� ����
    private bool isListening = false;
    private int currentTargetIndex = 0;
    private List<int> remainingTargets = new List<int>();

    // Android ���� �ν�
#if UNITY_ANDROID
    private AndroidJavaObject speechRecognizer;
    private AndroidJavaObject currentActivity;
    private SpeechRecognitionCallback speechCallback;
#endif

    // �̺�Ʈ
    public System.Action<string> OnVoiceRecognitionSuccess;

    private void Start()
    {
        if (voiceButton != null)
            voiceButton.onClick.AddListener(StartListening);

        InitializeTargets();
        ShowCurrentTarget();

        // ����ũ ���� Ȯ��
        StartCoroutine(CheckAndRequestPermissions());
    }

    private IEnumerator CheckAndRequestPermissions()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            Permission.RequestUserPermission(Permission.Microphone);
            
            // ���� ���� ���
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
                Debug.LogError("Unity Activity�� ������ �� �����ϴ�.");
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
                    Debug.Log("Android ���� �ν� �ʱ�ȭ ����");
                }
            }
            else
            {
                Debug.LogWarning("�� ��⿡���� ���� �ν��� ����� �� �����ϴ�.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("���� �ν� �ʱ�ȭ ����: " + e.Message);
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
            // ��� ������ �� ��������� �Ϸ�
            OnAllTargetsComplete();
            return;
        }

        // ���� Ÿ�� ����
        currentTargetIndex = remainingTargets[0];

        if (targetText != null)
        {
            targetText.text = $"���� ���غ�����:\n\"{positiveTargets[currentTargetIndex]}\"";
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
                buttonText.text = "��� ��...";
        }
#if UNITY_ANDROID && !UNITY_EDITOR
        StartAndroidSpeechRecognition();
#else
        // �����Ϳ����� �ùķ��̼�
        StartCoroutine(SimulateVoiceRecognition());
#endif
    }

#if UNITY_ANDROID
    private void StartAndroidSpeechRecognition()
    {
        if (speechRecognizer == null)
        {
            Debug.LogError("���� �νıⰡ �ʱ�ȭ���� �ʾҽ��ϴ�.");
            OnRecognitionError("�ʱ�ȭ ����");
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

            Debug.Log("Android ���� �ν� ����");
        }
        catch (System.Exception e)
        {
            Debug.LogError("���� �ν� ���� ����: " + e.Message);
            OnRecognitionError("���� ����");
        }
    }
#endif



    private IEnumerator SimulateVoiceRecognition()
    {
        // 2�� �� �ڵ����� ���� ��ǥ ���� �ν�
        yield return new WaitForSeconds(2f);

        // ���� ó��
        if (isListening)
            OnRecognitionSuccess(positiveTargets[currentTargetIndex]);
    }

    public void OnSpeechRecognitionResult(string result)
    {
        if (!isListening) return;

        Debug.Log("���� �ν� ���: " + result);
        OnRecognitionSuccess(result);
    }

    public void OnRecognitionError(string error)
    {
        Debug.LogError("���� �ν� ����: " + error);

        if (messageText != null)
            messageText.text = "�ٽ� �õ����ּ���.";

        ResetListeningState();
    }

    private void OnRecognitionSuccess(string recognized)
    {
        isListening = false;

        if (messageText != null)
            messageText.text = "���߾��! �Ĺ��� �⻵�ϰ� �־��";

        // ���� �˸�
        OnVoiceRecognitionSuccess?.Invoke(recognized);

        if (remainingTargets.Count > 0)
            remainingTargets.RemoveAt(0);

        // UI ������Ʈ
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
                buttonText.text = "���ϱ�";
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
            targetText.text = "��� ��ǥ �Ϸ�!\n�Ĺ��� ������ �����߾��!";

        if (voiceButton != null)
            voiceButton.interactable = false;

        if (messageText != null)
            messageText.text = "�����ؿ�! ����� ������� �Ƹ��ٿ� ���� �Ǿ����ϴ�.";
    }

    // �ݷ� �޽��� ����
    private string GetEncouragementMessage()
    {
        int completed = positiveTargets.Count - remainingTargets.Count;

        string[] messages = {
            "������ �������� �����ּ���",
            "�Ĺ��� ����� ��Ҹ��� ��ٸ��� �־��",
            "����� ��� õõ�� ���غ�����",
            "�������� �������� �������ּ���",
            "������ ���� �������� �����ּ���",
            "�Ĺ��� ����� �������� ������ �־��",
            "���� �� �Ծ��! ���ݸ� �� ��������"
        };

        if (completed == 0)
            return "������ �������� �����ּ���";
        else if (completed >= positiveTargets.Count - 2)
            return "���� �� �Ծ��! ���ݸ� �� ��������";
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
                Debug.LogError("SpeechRecognizer ���� ����: " + e.Message);
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
            Debug.LogError("onResults ó�� �� ����: " + e.Message);
            voiceController.OnRecognitionError("��� ó�� ����");
        }
    }

    public void onError(int errorCode)
    {
        string errorMessage = "���� �ν� ����: " + errorCode;
        voiceController.OnRecognitionError(errorMessage);
    }

    public void onReadyForSpeech(AndroidJavaObject bundle)
    {
        Debug.Log("���� �ν� �غ� �Ϸ�");
    }

    public void onBeginningOfSpeech()
    {
        Debug.Log("���� �Է� ����");
    }

    public void onEndOfSpeech()
    {
        Debug.Log("���� �Է� ����");
    }

    public void onRmsChanged(float rms) { }
    public void onBufferReceived(byte[] buffer) { }
    public void onPartialResults(AndroidJavaObject partialResults) { }
    public void onEvent(int eventType, AndroidJavaObject bundle) { }
}
#endif
