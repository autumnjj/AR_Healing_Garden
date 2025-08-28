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
        "�� �ȵ�Ⱦ��. �ٽ� ���غ�����!",
        "�� �� �Ƿ��ϰ� ���غ�����!",
        "�� ũ�� ���غ�����!"
    };
    public string[] successMessages =
    {
        "�Ǹ��ؿ�! �Ĺ��� �⻵�ؿ�!",
        "���߾��! �Ĺ��� �ڶ�� �־��!",
        "�Ϻ��ؿ�! �Ĺ��� ����� �������!"
    };

    [Header("���� �����")]
    public List<PositiveKeyword> positiveKeywords = new List<PositiveKeyword>();

    [System.Serializable]
    public class PositiveKeyword
    {
        public string keyword;
        public List<string> variations = new List<string>();
    }

    // ���� ����
    private int currentTargetIndex = 0;
    private List<int> remainingTargets = new List<int>();
    private int currentAttemptCount = 0;
    private bool isRecognizing = false;

    // ����ũ ����
    private AudioClip microphoneClip;
    private string microphoneDevice;
    private float[] samples;
    private float currentVolume = 0f;
    private float speakingTime = 0f;

    // Android Speech Recognition
    private AndroidJavaObject speechRecognizer;
    private AndroidJavaObject unityActivity;

    // ���(��׶��� ����)
    private Dictionary<string, int> speechSuccessCount = new Dictionary<string, int>();
    private Dictionary<string, int> totalAttemptCount = new Dictionary<string, int>();

    // �̺�Ʈ
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
                keyword = "�����",
                variations = new List<string> {"����Ѵ�", "����ؿ�", "����"}
                
            },
            new PositiveKeyword
            {
                keyword = "���ڴ�",
                variations = new List<string> {"���ڴ�", "������", "�̻�", "�Ƹ��ٿ�" }
                
            },
            new PositiveKeyword
            {
                keyword = "���ϰ� �־�",
                variations = new List<string> {"���߾�", "����", "���ߴ�", "����"}
                
            },
            new PositiveKeyword
            {
                keyword = "����",
                variations = new List<string> {"ȭ����", "������", "������" }
                
            },
            new PositiveKeyword
            {
                keyword = "����",
                variations = new List<string> {"����", "������", "�����մϴ�" }
                
            },
            new PositiveKeyword
            {
                keyword = "������",
                variations = new List<string> { "������", "��������" }
                
            },
            new PositiveKeyword
            {
                keyword = "�����",
                variations = new List<string> { "����ϴ�", "�Ǹ���", "����" }
                
            }
        };

        // ��� �ʱ�ȭ
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
            ShowMessage("����ũ ���� ������ �ʿ��ؿ�", Color.yellow);
                
            Permission.RequestUserPermission(Permission.Microphone);
            
            float timeout = 0f;
            while (!Permission.HasUserAuthorizedPermission(Permission.Microphone) && timeout < 10f)
            {
                timeout += Time.deltaTime;
                yield return null;
            }
            
            if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
            {
               ShowMessage("����ũ ������ �ʿ��մϴ�. �������� ������ ������ּ���", Color.red);
                yield break;
            }
            else
            {
                ShowMessage("����ũ �غ� �Ϸ�! �Ĺ��� ��ȭ�غ�����."), Color.green);
            }
        }

#endif

        // ����ũ ��ġ Ȯ��
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
            Debug.Log("Android Speech Recognition �ʱ�ȭ ����");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Android Speech Recognition �ʱ�ȭ ����: {e.Message}");
            useSpeechRecognition = false; // ����
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
            // ��� ������ �� ��������� �Ϸ�
            OnAllComplete();
            return;
        }

        // ���� Ÿ�� ����
        currentTargetIndex = remainingTargets[0];
        currentAttemptCount = 0;

        var currentKeyword = positiveKeywords[currentTargetIndex];

        if (targetText != null)
        {
           targetText.text = $"���� ���غ�����:\n\"{currentKeyword.keyword}\"";
        }

        ShowMessage("����ũ ��ư�� ���� ���غ�����!", Color.white);
    }


    public void StartRecognition()
    {
        if (!isRecognizing) return;

        var currentKeyword = positiveKeywords[remainingTargets[0]];
        totalAttemptCount[currentKeyword.keyword]++;

        // 3�� �õ��߰ų� �����ν��� ������� �ʴ� ��� -> ��ȭ ����
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

        ShowMessage("��� �־��...", Color.green);
        ChangeButtonColor(Color.green);

        // �⺻������ ���� ó��
        string recognizedText = "";

#if UNITY_ANDROID && !UNITY_EDITOR
        // Android �Ǳ�⿡���� ���� ���� �ν�
        yield return StartCoroutine(AndroidSpeechRecognitionCoroutine((result) => recognizedText = result));
#else
        // �����Ϳ����� ������ ��⸸ �ϰ� ���� ó��
        yield return new WaitForSeconds(speechTimeout);
        Debug.Log("[VoiceRecognizer] ������ ��� - ���� �ν� ��ŵ");
#endif

        if (IsKeywordMatched(recognizedText, targetKeyword))
        {
            // ���� �ν� ����!
            speechSuccessCount[targetKeyword.keyword]++;
            OnSuccess(targetKeyword, "speech");
        }
        else
        {
            // ���� - �ڿ������� ��õ� ����
            OnSpeechRecognitionFailed();
        }

        ChangeButtonColor(Color.white);
        isRecognizing = false;
    }

    private IEnumerator VolumeDetectionMode(PositiveKeyword targetKeyword)
    {
        isRecognizing = true;

        ShowMessage("��Ҹ��� ��� �־��...", Color.green);
        ChangeButtonColor(Color.green);

        bool volumeSuccess = false;
        yield return StartCoroutine(VolumeDetectionCoroutine((result) => volumeSuccess = result));

        if (volumeSuccess)
        {
            OnSuccess(targetKeyword, "volume");
        }
        else
        {
            ShowMessage("���� �� ũ�� ���غ�����!", Color.orange);
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
            
            // ���� ���������� �ݹ��� ���� ����� �޾ƾ� �մϴ�
            // �ӽ÷� �� ���ڿ� ��ȯ
            callback("");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[VoiceRecognizer] ���� �ν� ����: {e.Message}");
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

        // ����� �ݹ����� ����
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

        // RMS (Root Mean Square) ������� ���� ����
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

        // ������ ��ġ�ϰų� ����
        if (recognizedText.Contains(targetKeyword.keyword.ToLower())) return true;

        // ������� ��Ī
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
            Debug.Log("[VoiceRecognizer] Next turn ��ȭ ���� mode");
        }
    }

    private void OnSuccess(PositiveKeyword keyword, string method)
    {
        string successMessage = successMessages[Random.Range(0, successMessages.Length)];
        ShowMessage(successMessage, Color.green);

        Debug.Log($"[VoiceRecognizer] ����! Ű����: {keyword.keyword}, ���: {method}, �õ�: {currentAttemptCount}");

        // ���� �̺�Ʈ �߻�
        OnRecognitionSuccess?.Invoke(keyword.keyword, standardGrowthPoints, method);


        // ���� Ÿ������
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
            targetText.text = "��� ���� �Ϸ�!";

        ShowMessage("�����ؿ�! �Ĺ��� ����� ������� ���� �ڶ����!", Color.gold);

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


