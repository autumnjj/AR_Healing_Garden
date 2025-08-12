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

    private Image buttonImage;

    [Header("목표 문장들")]
    public List<string> positiveTargets = new List<string>
    {
        "힘내", "잘하고 있어", "할 수 있어", "있는 그대로도 완벽해", "대단해",
        "최고야", "예쁘다"
    };

    [Header("음성 설정")]
    public float recordingTime = 3f;
    public float volumeThreshold = 0.01f;
    public float minSpeakTime = 0.5f;

    // 상태 관리
    private bool isRecording = false;
    private int currentTargetIndex = 0;
    private List<int> remainingTargets = new List<int>();

    // 마이크 관련
    private AudioClip microphoneClip;
    private string microphoneDevice;
    private float[] samples;
    private float currentVolume = 0f;
    private float speakingTime = 0f;

    // 이벤트
    public System.Action<string> OnVoiceRecognitionSuccess;

    private void Start()
    {
        SetupButtonComponents();
        InitializeTargets();
        ShowCurrentTarget();

        // 마이크 권한 확인
        StartCoroutine(CheckMicrophonePermission());
    }

    private void SetupButtonComponents()
    {
        if (voiceButton != null)
        {
            voiceButton.onClick.AddListener(StartVoiceRecording);
            buttonImage = voiceButton.GetComponent<Image>();

            if (buttonImage == null)
            {
                Debug.LogError("voiceButton에 Image 컴포넌트가 없습니다!");
            }
            else
            {
                Debug.Log("Button Image component found successfully");
            }
        }
        else
        {
            Debug.LogError("voiceButton이 할당되지 않았습니다!");
        }
    }

    private IEnumerator CheckMicrophonePermission()
    {
        Debug.Log("Checking microphone permission...");

#if UNITY_ANDROID && !UNITY_EDITOR
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            Debug.Log("Requesting microphone permission...");
            if (messageText != null)
                messageText.text = "마이크 권한을 요청합니다...";
                
            Permission.RequestUserPermission(Permission.Microphone);
            
            float timeout = 0f;
            while (!Permission.HasUserAuthorizedPermission(Permission.Microphone) && timeout < 10f)
            {
                timeout += Time.deltaTime;
                yield return null;
            }
            
            if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
            {
                Debug.LogError("마이크 권한이 거부되었습니다!");
                if (messageText != null)
                    messageText.text = "마이크 권한이 필요합니다. 설정에서 권한을 허용해주세요.";
                yield break;
            }
            else
            {
                Debug.Log("마이크 권한이 허용되었습니다!");
            }
        }

#endif

        // 마이크 장치 확인
        CheckMicrophoneDevices();

        if (messageText != null)
            messageText.text = "마이크 준비 완료! 식물과 대화해보세요.";

        yield return null;
    }

    private void CheckMicrophoneDevices()
    {
        for (int i = 0; i < Microphone.devices.Length; i++)
        {
            Debug.Log($"Microphone {i}: {Microphone.devices[i]}");
        }

        if (Microphone.devices.Length > 0)
        {
            microphoneDevice = Microphone.devices[0];
            Debug.Log($"Mic : {microphoneDevice}");
        }
        else
        {
            Debug.LogError("There is no Mic device");
            if (messageText != null)
                messageText.text = "마이크를 찾을 수 없습니다.";
        }
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
            string encouragementMessage = GetEncouragementMessage();
            messageText.text = encouragementMessage;
        }
    }


    private void StartVoiceRecording()
    {
        if (isRecording)
        {
            StopVoiceRecording();
            return;
        }

        if (Microphone.devices.Length == 0)
        {
            if (messageText != null)
                messageText.text = "마이크를 찾을 수 없습니다.";
            return;
        }

        StartCoroutine(RecordVoice());
    }


    private IEnumerator RecordVoice()
    {
        isRecording = true;
        speakingTime = 0f;

        if (buttonImage != null)
        {
            Color recordingColor = new Color(90f / 255f, 139f / 255f, 90f / 255f, 1f);
            buttonImage.color = recordingColor;
        }

        if (messageText != null)
            messageText.text = "듣고 있어요!";

        microphoneClip = Microphone.Start(microphoneDevice, false, (int)recordingTime, 44100);

        if (microphoneClip == null)
        {
            Debug.LogError("Mic record start failed");
            StopVoiceRecording();
            yield break;
        }

        float recordingTimer = 0f;
        bool voiceDetected = false;

        while(recordingTimer < recordingTime && isRecording)
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

        StopVoiceRecording();

        if (voiceDetected && speakingTime >= minSpeakTime)
        {
            OnVoiceSuccess();
        }
        else
        {
            OnVoiceFailed();
        }
    }

    private void CheckMicrophoneVolume()
    {
        if (microphoneClip == null || string.IsNullOrEmpty(microphoneDevice))
        {
            currentVolume = 0f;
            return;
        }

        int micPosition = Microphone.GetPosition(microphoneDevice);
        if (micPosition <= 0) return;

        // 오디오 데이터 가져오기
        int sampleLength = 128;
        samples = new float[sampleLength];

        int startPosition = Mathf.Max(0, micPosition - sampleLength);
        microphoneClip.GetData(samples, startPosition);

        // RMS (Root Mean Square) 계산으로 볼륨 측정
        float sum = 0f;
        for (int i = 0; i < samples.Length; i++)
        {
            sum += samples[i] * samples[i];
        }
        currentVolume = Mathf.Sqrt(sum / samples.Length);
    }

    private void StopVoiceRecording()
    {
        if (isRecording)
        {
            Microphone.End(microphoneDevice);
            isRecording = false;
        }

        if (buttonImage != null)
            buttonImage.color = Color.white;
    }

    private void OnVoiceSuccess()
    {
        Debug.Log($"음성 인식 성공! 말한 시간: {speakingTime:F1}초, 최대 볼륨: {currentVolume:F3}");

        if (messageText != null)
            messageText.text = "잘했어요! 식물이 당신의 목소리를 들었어요!";

        // 성공 이벤트 발생
        OnVoiceRecognitionSuccess?.Invoke(positiveTargets[currentTargetIndex]);


        if (remainingTargets.Count > 0)
            remainingTargets.RemoveAt(0);

        StartCoroutine(DelayedNextTarget());
    }

    private void OnVoiceFailed()
    {
        Debug.Log($"음성 감지 실패. 말한 시간: {speakingTime:F1}초, 최대 볼륨: {currentVolume:F3}");

        if (messageText != null)
            messageText.text = "더 크게 말해주세요!";
    }

    private IEnumerator DelayedNextTarget()
    {
        yield return new WaitForSeconds(1.5f);
        ShowCurrentTarget();
    }

    public void OnAllTargetsComplete()
    {
        if (targetText != null)
            targetText.text = "모든 응원 완료!";

        if (voiceButton != null)
            voiceButton.interactable = false;

        if (messageText != null)
            messageText.text = "축하해요! 따뜻한 말로 식물이 잘 자라고 있어요!";
    }

    // 격려 메시지 생성
    private string GetEncouragementMessage()
    {
        int completed = positiveTargets.Count - remainingTargets.Count;

        string[] messages = {
            "버튼을 눌러 말해보세요",
            "식물이 기다리고 있어요",
            "따뜻한 목소리로 말해주세요",
            "마음을 담아 말해보세요",
            "거의 다 왔어요!",
            "마지막이에요!"
        };

        if (completed == 0)
            return messages[0];
        else if (completed >= positiveTargets.Count - 1)
            return messages[5];
        else if (completed >= positiveTargets.Count - 2)
            return messages[4];
        else
            return messages[(completed - 1) % 3 + 1];
    }

    // 디버그용 설정 조정 메서드들
    public void SetVolumeThreshold(float threshold)
    {
        volumeThreshold = threshold;
        Debug.Log($"볼륨 임계값 변경: {threshold:F3}");
    }

    public void SetMinSpeakTime(float time)
    {
        minSpeakTime = time;
        Debug.Log($"최소 말하기 시간 변경: {time:F1}초");
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
        if (isRecording)
        {
            Microphone.End(microphoneDevice);
        }
    }
}


