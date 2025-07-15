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

    [Header("설정")]
    public PositiveSpeechData speechData;
    public PlantGrowthData PlantGrowthData;

    [Header("목표 시스템")]
    public int requiredRepeats = 3;

    // 상태
    private bool isListening = false;
    private string currentTarget = "";
    private int currentCount = 0;
    private int dailyTargetIndex = 0;

    // 하루 목표 문구들
    private string[] dailyTargets =
    {
        "나는 소중해",
        "잘하고 있어",
        "고마워",
        "무럭무럭 자라",
        "사랑해"
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
            // 모든 목표 완료
            if (statusText != null)
                statusText.text = "오늘의 목표를 모두 완료했어요!";
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
                statusText.text = "마이크 권한이 필요해요";
            return;
        }
        isListening = true;
        if (statusText != null)
            statusText.text = "듣고 있어요...";

        UpdateUI();

#if UNITY_ANDROID && !UNITY_EDITOR
StartAndroidVoicRecognition();
#else
        // 에디터에서는 시뮬레이션
        StartCoroutine(SimulateVoiceInput());
#endif
    }

    private void StopListening()
    {
        isListening = false;
        if (statusText != null)
            statusText.text = "음성 인식을 중지했어요";

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
            // 현재 목표 문구를 시뮬레이션으로 인식
            OnVoiceRecognitionResult(currentTarget);
        }
    }

    // Android에서 호출될 메서드
    public void OnVoiceRecognitionResult(string result)
    {
        if (!isListening) return;

        Debug.Log("Voice Reco result : " + result);

        if(statusText != null) 
            statusText.text = $"인식됨: \"{result}\"";

        ProcessVoiceInput(result);
        StopListening();
    }

    public void OnVoiceRecognitionError(string error)
    {
        if (statusText != null)
            statusText.text = "다시 시도해주세요";
        StopListening();
    }

    private void ProcessVoiceInput(string input)
    {
        if (speechData == null) return;

        var matchedPhraase = speechData.FindBestMatch(input);

        if(matchedPhraase != null)
        {
            // 긍정적 언어 감지됨
            AddGrowthPoints(matchedPhraase.growthPoints);

            // 목표 문구와 일치하는지 확인
            if(IsTargetMatch(input, currentTarget))
            {
                currentCount++;

                if(currentCount >= requiredRepeats)
                {
                    // 목표 완료
                    CompleteTarget();
                }
            }
            ShowPositiveFeedback(matchedPhraase);
        }
        else
        {
            if (statusText != null)
                statusText.text = "긍정적인 말을 따라 해보세요";
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
            statusText.text = $"완료! \"{currentTarget}\"";

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
        if (PlantGrowthData != null)
            PlantGrowthData.AddGrowthPoints(points, InteractionType.PositiveTalk);
    }

    private void ShowPositiveFeedback(PositivePhrase phrase)
    {
        string[] feedbacks =
        {
            "좋아요!",
            "휼륭해요",
            "따뜻한 말이에요!",
            "식물이 기뻐해요"
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
        // 목표 문구 표시
        if(targetText != null)
        {
            if(dailyTargetIndex < dailyTargets.Length)
            {
                targetText.text = $"목표 : \"{currentTarget}\"";
            }
            else
            {
                targetText.text = "모든 목표 완료!";
            }
        }

        // 진행률 표시
        if (progressText != null)
            progressText.text = $"진행 : {currentCount}/{requiredRepeats}회";

        // 버튼 텍스트 업데이트
        if(listenButton != null)
        {
            var buttonText = listenButton.GetComponentsInChildren<TextMeshProUGUI>();
            if (buttonText != null)
                buttonText.text = isListening ? "중지" : "음성 인식 시작";

            listenButton.interactable = dailyTargetIndex < dailyTargets.Length;
        }
    }

    // 수동 완료 (테스트용)
    public void ManualComplete()
    {
        if (!string.IsNullOrEmpty(currentTarget))
        {
            OnVoiceRecognitionResult(currentTarget);
        }
    }
}
