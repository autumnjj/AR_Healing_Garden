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

    [Header("��ǥ �����")]
    public List<string> positiveTargets = new List<string>
    {
        "����", "���ϰ� �־�", "�� �� �־�", "�ִ� �״�ε� �Ϻ���", "�����",
        "�ְ��", "���ڴ�"
    };

    [Header("���� ����")]
    public float recordingTime = 3f;
    public float volumeThreshold = 0.01f;
    public float minSpeakTime = 0.5f;

    // ���� ����
    private bool isRecording = false;
    private int currentTargetIndex = 0;
    private List<int> remainingTargets = new List<int>();

    // ����ũ ����
    private AudioClip microphoneClip;
    private string microphoneDevice;
    private float[] samples;
    private float currentVolume = 0f;
    private float speakingTime = 0f;

    // �̺�Ʈ
    public System.Action<string> OnVoiceRecognitionSuccess;

    private void Start()
    {
        SetupButtonComponents();
        InitializeTargets();
        ShowCurrentTarget();

        // ����ũ ���� Ȯ��
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
                Debug.LogError("voiceButton�� Image ������Ʈ�� �����ϴ�!");
            }
            else
            {
                Debug.Log("Button Image component found successfully");
            }
        }
        else
        {
            Debug.LogError("voiceButton�� �Ҵ���� �ʾҽ��ϴ�!");
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
                messageText.text = "����ũ ������ ��û�մϴ�...";
                
            Permission.RequestUserPermission(Permission.Microphone);
            
            float timeout = 0f;
            while (!Permission.HasUserAuthorizedPermission(Permission.Microphone) && timeout < 10f)
            {
                timeout += Time.deltaTime;
                yield return null;
            }
            
            if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
            {
                Debug.LogError("����ũ ������ �źεǾ����ϴ�!");
                if (messageText != null)
                    messageText.text = "����ũ ������ �ʿ��մϴ�. �������� ������ ������ּ���.";
                yield break;
            }
            else
            {
                Debug.Log("����ũ ������ ���Ǿ����ϴ�!");
            }
        }

#endif

        // ����ũ ��ġ Ȯ��
        CheckMicrophoneDevices();

        if (messageText != null)
            messageText.text = "����ũ �غ� �Ϸ�! �Ĺ��� ��ȭ�غ�����.";

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
                messageText.text = "����ũ�� ã�� �� �����ϴ�.";
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
                messageText.text = "����ũ�� ã�� �� �����ϴ�.";
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
            messageText.text = "��� �־��!";

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

        // ����� ������ ��������
        int sampleLength = 128;
        samples = new float[sampleLength];

        int startPosition = Mathf.Max(0, micPosition - sampleLength);
        microphoneClip.GetData(samples, startPosition);

        // RMS (Root Mean Square) ������� ���� ����
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
        Debug.Log($"���� �ν� ����! ���� �ð�: {speakingTime:F1}��, �ִ� ����: {currentVolume:F3}");

        if (messageText != null)
            messageText.text = "���߾��! �Ĺ��� ����� ��Ҹ��� ������!";

        // ���� �̺�Ʈ �߻�
        OnVoiceRecognitionSuccess?.Invoke(positiveTargets[currentTargetIndex]);


        if (remainingTargets.Count > 0)
            remainingTargets.RemoveAt(0);

        StartCoroutine(DelayedNextTarget());
    }

    private void OnVoiceFailed()
    {
        Debug.Log($"���� ���� ����. ���� �ð�: {speakingTime:F1}��, �ִ� ����: {currentVolume:F3}");

        if (messageText != null)
            messageText.text = "�� ũ�� �����ּ���!";
    }

    private IEnumerator DelayedNextTarget()
    {
        yield return new WaitForSeconds(1.5f);
        ShowCurrentTarget();
    }

    public void OnAllTargetsComplete()
    {
        if (targetText != null)
            targetText.text = "��� ���� �Ϸ�!";

        if (voiceButton != null)
            voiceButton.interactable = false;

        if (messageText != null)
            messageText.text = "�����ؿ�! ������ ���� �Ĺ��� �� �ڶ�� �־��!";
    }

    // �ݷ� �޽��� ����
    private string GetEncouragementMessage()
    {
        int completed = positiveTargets.Count - remainingTargets.Count;

        string[] messages = {
            "��ư�� ���� ���غ�����",
            "�Ĺ��� ��ٸ��� �־��",
            "������ ��Ҹ��� �����ּ���",
            "������ ��� ���غ�����",
            "���� �� �Ծ��!",
            "�������̿���!"
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

    // ����׿� ���� ���� �޼����
    public void SetVolumeThreshold(float threshold)
    {
        volumeThreshold = threshold;
        Debug.Log($"���� �Ӱ谪 ����: {threshold:F3}");
    }

    public void SetMinSpeakTime(float time)
    {
        minSpeakTime = time;
        Debug.Log($"�ּ� ���ϱ� �ð� ����: {time:F1}��");
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


