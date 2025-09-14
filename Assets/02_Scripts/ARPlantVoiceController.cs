using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;
using System;

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

    [Header("진행 버튼")]
    public Button progressButton;  // 동적으로 변경되는 진행 버튼

    [Header("3일 시스템 설정")]
    [SerializeField] private int dailyTargetCount = 3;
    [SerializeField] private int currentDay = 1;
    private bool isDayCompleted = false;

    [Header("Recognition Settings")]
    [SerializeField] private bool useSpeechRecognition = true;
    [SerializeField] private int maxSpeechAttempts = 3;
    [SerializeField] private float speechTimeout = 4f;
    [SerializeField] private float standardGrowthPoints = 15f;

    [Header("Volume Detection Settings")]
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

    // 통계
    private Dictionary<string, int> speechSuccessCount = new Dictionary<string, int>();
    private Dictionary<string, int> totalAttemptCount = new Dictionary<string, int>();

    // 이벤트
    public System.Action<string, float, string> OnRecognitionSuccess;

    // 캘린더 연동
    private CalendarManager calendarManager;

    private void Start()
    {
        calendarManager = FindAnyObjectByType<CalendarManager>();

        InitializeThreeDaySystem();
        SetupKeywords();
        InitializeComponents();
        SetupProgressButton();
        ShowCurrentTarget();

        StartCoroutine(CheckPermission());
    }

    private void SetupProgressButton()
    {
        UpdateProgressButton();
    }

    private void UpdateProgressButton()
    {
        if (progressButton == null) return;

        var buttonText = progressButton.GetComponentInChildren<TextMeshProUGUI>();

        switch (currentDay)
        {
            case 1:
                if (isDayCompleted)
                {
                    progressButton.gameObject.SetActive(true);
                    progressButton.interactable = true;
                    if (buttonText) buttonText.text = "Day 2 시작 🌿";

                    progressButton.onClick.RemoveAllListeners();
                    progressButton.onClick.AddListener(() => StartNextDay(2));

                    Debug.Log("Day 1 완료 - Day 2 버튼 활성화");
                }
                else
                {
                    progressButton.gameObject.SetActive(false);
                }
                break;

            case 2:
                if (isDayCompleted)
                {
                    progressButton.gameObject.SetActive(true);
                    progressButton.interactable = true;
                    if (buttonText) buttonText.text = "Day 3 시작 🌸";

                    progressButton.onClick.RemoveAllListeners();
                    progressButton.onClick.AddListener(() => StartNextDay(3));

                    Debug.Log("Day 2 완료 - Day 3 버튼 활성화");
                }
                else
                {
                    progressButton.gameObject.SetActive(false);
                }
                break;

            case 3:
                if (isDayCompleted)
                {
                    progressButton.gameObject.SetActive(true);
                    progressButton.interactable = true;
                    if (buttonText) buttonText.text = "새 식물 시작 🌱";

                    progressButton.onClick.RemoveAllListeners();
                    progressButton.onClick.AddListener(StartNewPlant);

                    Debug.Log("Day 3 완료 - 새 식물 버튼 활성화");
                }
                else
                {
                    progressButton.gameObject.SetActive(false);
                }
                break;
        }
    }

    private void StartNextDay(int day)
    {
        currentDay = day;
        isDayCompleted = false;

        PlayerPrefs.SetInt("Plant_CurrentDay", currentDay);
        PlayerPrefs.SetInt("Day_Completed", 0);
        PlayerPrefs.Save();

        NotifyPlantGrowthController(day);

        InitializeDailyTargets();
        ShowCurrentTarget();
        UpdateProgressButton();

        if (voiceButton != null)
            voiceButton.interactable = true;

        string message = GetDayStartMessage(day);
        ShowMessage(message, Color.green);

        Debug.Log($"Day {currentDay} 시작!");
    }

    private void StartNewPlant()
    {
        PlayerPrefs.SetString("First_Play_Date", DateTime.Now.ToString("yyyy-MM-dd"));
        PlayerPrefs.DeleteKey("Plant_CurrentDay");
        PlayerPrefs.DeleteKey("Day_Completed");
        PlayerPrefs.Save();

        currentDay = 1;
        isDayCompleted = false;

        var plantController = FindAnyObjectByType<ARPlantGrowthController>();
        if (plantController != null)
        {
            plantController.ResetGrowth();
        }

        InitializeDailyTargets();
        ShowCurrentTarget();
        UpdateProgressButton();

        if (voiceButton != null)
            voiceButton.interactable = true;

        ShowMessage("새로운 식물과 함께 새로운 여정을 시작해요! 🌱", Color.green);

        Debug.Log("새 식물 시작 - 모든 데이터 리셋됨");
    }

    private string GetDayStartMessage(int day)
    {
        switch (day)
        {
            case 2:
                return "Day 2 시작! 어제의 새싹이 더 자라기를 기다려요 🌿";
            case 3:
                return "Day 3 시작! 드디어 아름다운 꽃이 필 차례예요 🌸";
            default:
                return $"Day {day} 시작! 식물과 대화해보세요 🌱";
        }
    }

    private void NotifyPlantGrowthController(int day)
    {
        var plantController = FindAnyObjectByType<ARPlantGrowthController>();
        if (plantController != null)
        {
            plantController.SetStartingStage(day);
        }
    }

    private void InitializeThreeDaySystem()
    {
        currentDay = GetOrCreateCurrentDay();
        isDayCompleted = GetDayCompletedStatus();

        Debug.Log($"3일 시스템 초기화: Day {currentDay}, Completed: {isDayCompleted}");

        InitializeDailyTargets();
    }

    private int GetOrCreateCurrentDay()
    {
        string today = DateTime.Now.ToString("yyyy-MM-dd");
        string lastDate = PlayerPrefs.GetString("Last_Play_Date", "");

        if (lastDate != today)
        {
            int savedDay = PlayerPrefs.GetInt("Plant_CurrentDay", 1);

            if (lastDate != "" && savedDay < 3)
            {
                savedDay++;
                Debug.Log($"새로운 날: Day {savedDay}로 진행");
            }
            else if (savedDay >= 3)
            {
                savedDay = 1;
                Debug.Log("3일 완료 - 새로운 식물 시작");
            }

            PlayerPrefs.SetInt("Plant_CurrentDay", savedDay);
            PlayerPrefs.SetString("Last_Play_Date", today);
            PlayerPrefs.SetInt("Day_Completed", 0);
            PlayerPrefs.Save();

            return savedDay;
        }

        return PlayerPrefs.GetInt("Plant_CurrentDay", 1);
    }

    private bool GetDayCompletedStatus()
    {
        return PlayerPrefs.GetInt("Day_Completed", 0) == 1;
    }

    private void InitializeDailyTargets()
    {
        remainingTargets.Clear();

        if (isDayCompleted)
        {
            return;
        }

        for (int i = 0; i < dailyTargetCount; i++)
        {
            remainingTargets.Add(i % 3);
        }

        Debug.Log($"Day {currentDay} 타겟 설정: {remainingTargets.Count}개");
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
            }
        };

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
#if UNITY_ANDROID && !UNITY_EDITOR
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
                ShowMessage("마이크 준비 완료! 식물과 대화해보세요.", Color.green);
            }
        }
#endif
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
            useSpeechRecognition = false;
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

    private void ShowCurrentTarget()
    {
        if (isDayCompleted)
        {
            OnDayCompleted();
            return;
        }

        if (remainingTargets.Count == 0)
        {
            OnDayCompleted();
            return;
        }

        currentTargetIndex = remainingTargets[0];
        currentAttemptCount = 0;

        var currentKeyword = positiveKeywords[currentTargetIndex];

        if (targetText != null)
        {
            targetText.text = $"Day {currentDay}\n\n따라 말해보세요:\n\"{currentKeyword.keyword}\"";
        }

        ShowMessage("마이크 버튼을 눌러 말해보세요!", Color.white);
    }

    public void StartRecognition()
    {
        if (isRecognizing || isDayCompleted) return;

        var currentKeyword = positiveKeywords[remainingTargets[0]];
        totalAttemptCount[currentKeyword.keyword]++;

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

        string recognizedText = "";

#if UNITY_ANDROID && !UNITY_EDITOR
        yield return StartCoroutine(AndroidSpeechRecognitionCoroutine((result) => recognizedText = result));
#else
        yield return new WaitForSeconds(speechTimeout);
        Debug.Log("[VoiceRecognizer] 에디터 모드 - 음성 인식 스킵");
#endif

        if (IsKeywordMatched(recognizedText, targetKeyword))
        {
            speechSuccessCount[targetKeyword.keyword]++;
            OnSuccess(targetKeyword, "speech");
        }
        else
        {
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
        bool hasError = false;
        
        // try-catch를 yield return 밖에서 사용
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
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[VoiceRecognizer] 음성 인식 오류: {e.Message}");
            hasError = true;
        }
        
        // yield return은 try-catch 밖에서
        yield return new WaitForSeconds(speechTimeout);
        
        // 결과 반환
        if (hasError)
        {
            callback("");
        }
        else
        {
            callback(""); // 실제 구현에서는 음성인식 결과 반환
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

        while (recordingTimer < recordingTime)
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

        if (recognizedText.Contains(targetKeyword.keyword.ToLower())) return true;

        foreach (var variation in targetKeyword.variations)
        {
            if (recognizedText.Contains(variation.ToLower())) return true;
        }

        return false;
    }

    private void OnSpeechRecognitionFailed()
    {
        string encouragement = encouragementMessages[UnityEngine.Random.Range(0, encouragementMessages.Length)];
        ShowMessage(encouragement, Color.orange);

        Debug.Log($"[VoiceRecognizer] Voice Rec Failed - Try {currentAttemptCount}/{maxSpeechAttempts}");
    }

    private void OnSuccess(PositiveKeyword keyword, string method)
    {
        string successMessage = successMessages[UnityEngine.Random.Range(0, successMessages.Length)];
        ShowMessage(successMessage, Color.green);

        Debug.Log($"[VoiceRecognizer] 성공! 키워드: {keyword.keyword}, 방법: {method}, 시도: {currentAttemptCount}");

        OnRecognitionSuccess?.Invoke(keyword.keyword, standardGrowthPoints, method);

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
            var buttonImage = voiceButton.GetComponent<UnityEngine.UI.Image>();
            if (buttonImage != null)
                buttonImage.color = color;
        }
    }

    private IEnumerator DelayedNextTarget()
    {
        yield return new WaitForSeconds(2f);
        ShowCurrentTarget();
    }

    public void OnDayCompleted()
    {
        isDayCompleted = true;

        PlayerPrefs.SetInt("Day_Completed", 1);
        PlayerPrefs.Save();

        if (calendarManager != null)
        {
            calendarManager.RecordTodaySpeech();
        }

        string message = GetDayCompletedMessage();

        if (targetText != null)
            targetText.text = $"Day {currentDay} 완료! 🎉";

        ShowMessage(message, Color.gold);

        if (voiceButton != null)
            voiceButton.interactable = false;

        UpdateProgressButton();

        Debug.Log($"Day {currentDay} 완료! 진행 버튼 업데이트됨");
    }

    private string GetDayCompletedMessage()
    {
        switch (currentDay)
        {
            case 1:
                return "오늘 할당량 완료! 작은 새싹이 나왔어요 🌱\nDay 2 버튼을 눌러 계속하세요!";
            case 2:
                return "2일차 완료! 식물이 쑥쑥 자라고 있어요 🌿\nDay 3 버튼을 눌러 마지막 단계로!";
            case 3:
                return "축하해요! 아름다운 꽃이 피었어요 🌸\n새 식물 버튼을 눌러 다시 시작해보세요!";
            default:
                return "오늘 목표 달성! 🌱";
        }
    }

    public bool IsAllComplete()
    {
        return isDayCompleted || remainingTargets.Count == 0;
    }

    public int GetCurrentDay()
    {
        return currentDay;
    }

    public void SetSpeechRecognition(bool enabled)
    {
        useSpeechRecognition = enabled;
    }

    public int GetRemainingCount()
    {
        return remainingTargets.Count;
    }

    
}