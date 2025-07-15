using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Android;
using TMPro;
using System.Collections;

public class SimpleVoiceRecognition : MonoBehaviour
{
    [Header("UI")]
    public Button listenButton;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI targetText;
    public TextMeshProUGUI progressText;

    [Header("����")]
    public PositiveSpeechData speechData;
    public PlantGrowthData PlantGrowthData;

    [Header("��ǥ �ý���")]
    public int requiredRepeats = 3;

    // ����
    private bool isListening = false;
    private string currentTarget = "";
    private int currentCount = 0;
    private int dailyTargetIndex = 0;

    // �Ϸ� ��ǥ ������
    private string[] dailyTargets =
    {
        "���� ������",
        "���ϰ� �־�",
        "����",
        "�������� �ڶ�",
        "�����"
    };

    private void Start()
    {
        SetupUI();
        LoadNextTarget();
        RequestPermission();
    }

    private void SetupUI()
    {
        if (listenButton != null)
            listenButton.onClick.AddListener(ToggleListening);

        UpdateUI();
    }

    private void RequestPermission()
    {
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            Permission.RequestUserPermissions(Permission.Microphone);
        }
    }

    private void LoadNextTarget()
    {
        if(dailyTargetIndex < dailyTargets.Length)
        {
            currentTarget = dailyTargets[dailyTargetIndex];
            currentCount = 0;
            UpdateUI();
        }
        else
        {
            // ��� ��ǥ �Ϸ�
            if (statusText != null)
                statusText.text = "������ ��ǥ�� ��� �Ϸ��߾��!";
        }
    }

    public void ToggleListening()
    {
        if (!isListening)
            StopListening();
        else
            StartListening();
    }

    private void StartListening()
    {
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            if (statusText != null)
                statusText.text = "����ũ ������ �ʿ��ؿ�";
            return;
        }
        isListening = true;
        if (statusText != null)
            statusText.text = "��� �־��...";

        UpdateUI();

#if UNITY_ANDROID && !UNITY_EDITOR
StartAndroidVoicRecognition();
#else
        // �����Ϳ����� �ùķ��̼�
        StartCoroutine(SimulateVoiceInput());
#endif
    }

    private void StopListening()
    {
        isListening = false;
        if (statusText != null)
            statusText.text = "���� �ν��� �����߾��";

        UpdateUI();
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private void StartAndroidVoiceRecognition()
    {
        try
        {
            AndroidJavaClass unityClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject activity = unityClass.GetStatic<AndroidJavaObject>("currentActivity");
            
            AndroidJavaClass intentClass = new AndroidJavaClass("android.content.Intent");
            AndroidJavaObject intent = new AndroidJavaObject("android.content.Intent");
            
            AndroidJavaClass recognizerIntentClass = new AndroidJavaClass("android.speech.RecognizerIntent");
            intent.Call<AndroidJavaObject>("setAction", 
                recognizerIntentClass.GetStatic<string>("ACTION_RECOGNIZE_SPEECH"));
            
            intent.Call<AndroidJavaObject>("putExtra", 
                recognizerIntentClass.GetStatic<string>("EXTRA_LANGUAGE"), "ko-KR");
            
            activity.Call("startActivityForResult", intent, 1234);
        }
        catch(System.Exception e)
        {
                Debug.LogError("Voice recognition start failed : " + e.Message);
                OnVoiceRecognitionError("Voice recognition can not start!);
        }
    }
#endif

    private IEnumerator SimulateVoiceInput()
    {
        yield return new WaitForSeconds(2f);

        if (isListening)
        {
            // ���� ��ǥ ������ �ùķ��̼����� �ν�
            OnVoiceRecognitionResult(currentTarget);
        }
    }

    // Android���� ȣ��� �޼���
    public void OnVoiceRecognitionResult(string result)
    {
        if (!isListening) return;

        Debug.Log("Voice Reco result : " + result);

        if(statusText != null) 
            statusText.text = $"�νĵ�: \"{result}\"";

        ProcessVoiceInput(result);
        StopListening();
    }

    public void OnVoiceRecognitionError(string error)
    {
        if (statusText != null)
            statusText.text = "�ٽ� �õ����ּ���";
        StopListening();
    }

    private void ProcessVoiceInput(string input)
    {
        if (speechData == null) return;

        var matchedPhraase = speechData.FindBestMatch(input);

        if(matchedPhraase != null)
        {
            // ������ ��� ������
            AddGrowthPoints(matchedPhraase.growthPoints);

            // ��ǥ ������ ��ġ�ϴ��� Ȯ��
            if(IsTargetMatch(input, currentTarget))
            {
                currentCount++;

                if(currentCount >= requiredRepeats)
                {
                    // ��ǥ �Ϸ�
                    CompleteTarget();
                }
            }
            ShowPositiveFeedback(matchedPhraase);
        }
        else
        {
            if (statusText != null)
                statusText.text = "�������� ���� ���� �غ�����";
        }

        UpdateUI();
    }

    private bool IsTargetMatch(string input, string target)
    {
        return input.ToLower().Contains(target.ToLower()) ||
            target.ToLower().Contains(input.ToLower());
    }

    private void CompleteTarget()
    {
        if(statusText != null)
            statusText.text = $"�Ϸ�! \"{currentTarget}\"";

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
        if (PlantGrowthData != null)
            PlantGrowthData.AddGrowthPoints(points, InteractionType.PositiveTalk);
    }

    private void ShowPositiveFeedback(PositivePhrase phrase)
    {
        string[] feedbacks =
        {
            "���ƿ�!",
            "�Ḣ�ؿ�",
            "������ ���̿���!",
            "�Ĺ��� �⻵�ؿ�"
        };

        string feedback = feedbacks[Random.Range(0, feedbacks.Length)];

        if(statusText != null)
            StartCoroutine(ShowTemporaryMessage(feedback, 2f));
    }

    private IEnumerator ShowTemporaryMessage(string message, float duration)
    {
        string originalText = statusText.text;
        statusText.text = message;

        yield return new WaitForSeconds(duration);

        statusText.text = originalText;
    }

    private void UpdateUI()
    {
        // ��ǥ ���� ǥ��
        if(targetText != null)
        {
            if(dailyTargetIndex < dailyTargets.Length)
            {
                targetText.text = $"��ǥ : \"{currentTarget}\"";
            }
            else
            {
                targetText.text = "��� ��ǥ �Ϸ�!";
            }
        }

        // ����� ǥ��
        if (progressText != null)
            progressText.text = $"���� : {currentCount}/{requiredRepeats}ȸ";

        // ��ư �ؽ�Ʈ ������Ʈ
        if(listenButton != null)
        {
            var buttonText = listenButton.GetComponentsInChildren<TextMeshProUGUI>();
            if (buttonText != null)
                buttonText.text = isListening ? "����" : "���� �ν� ����";

            listenButton.interactable = dailyTargetIndex < dailyTargets.Length;
        }
    }

    // ���� �Ϸ� (�׽�Ʈ��)
    public void ManualComplete()
    {
        if (!string.IsNullOrEmpty(currentTarget))
        {
            OnVoiceRecognitionResult(currentTarget);
        }
    }
}
