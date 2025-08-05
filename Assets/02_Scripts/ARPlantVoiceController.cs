using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using NUnit.Framework;
using System.Transactions;

public class ARPlantVoiceController : MonoBehaviour
{
    [Header("�ٽ� ������Ʈ")]
    public ARPlantManager arPlantManager;

    [Header("UI")]
    public Button voiceButton;
    public TextMeshProUGUI voiceStatusText;
    public TextMeshProUGUI currentTargetText;

    [Header("������ �ܾ� ���")]
    public List<string> positiveWords = new List<string>
    {
        "���ڴ�", "����", "���Ѵ�", "����", "�����",
        "������", "�Ǹ��ϴ�", "�ְ��", "�Ƹ����", "�����ϴ�"
    };

    [Header("��ǥ �����")]
    public List<string> targetPhrases = new List<string>
    {
        "�ʴ� ������", "���ϰ� �־�", "����", "�������� �ڶ�"
    };


    // ���� ����
    private bool isListening = false;
    private int currentTargetIndex = 0;
    private int completedCount = 0;
    private const int REQUIRED_COUNT = 3;

    // ���� �ν� (�ùķ��̼ǿ�)
    private bool isSimulationMode = true;

    private void Start()
    {
        SetupUI();
        ShowCurrentTarget();
    }


    private void SetupUI()
    {
        // ��ư �̺�Ʈ ����
        if (voiceButton != null)
            voiceButton.onClick.AddListener(ToggleListening);

        UpdateVoiceStatus("���� �ν� �غ��");
    }

    
    private void ShowCurrentTarget()
    {
        if (currentTargetIndex < targetPhrases.Count)
        {
            string target = targetPhrases[currentTargetIndex];
            if (currentTargetText != null)
            {
                currentTargetText.text = $"���� ���غ�����: \"{target}\" ({completedCount}/{REQUIRED_COUNT})";
             }
        }
        else
        {
            if (currentTargetText != null)
            {
                currentTargetText.text = "��� ��ǥ�� �Ϸ��߽��ϴ�!";
            }
        }

    }

    public void ToggleListening()
    {
        if (isListening)
        {
            StopListening();
        }
        else
        {
            StartListening();
        }
    }

    private void StartListening()
    {
        isListening = true;
        UpdateVoiceStatus("��� �ֽ��ϴ�...");

        // ���� ���� �ν� ������ ���⿡ �߰�
    }

    private void StopListening()
    {
        isListening = false;
        UpdateVoiceStatus("���� �ν� ������");
    }

    public void OnVoiceRecognitionSuccess(string recognizedText)
    {
        if (!isListening) return;

        Debug.Log($"Voice Reocogn Result : {recognizedText}");

        // ���� ��ǥ ����� ��
        if (currentTargetIndex < targetPhrases.Count)
        {
            string currentTarget = targetPhrases[currentTargetIndex];

            if (IsTextMatch(recognizedText, currentTarget))
            {
                completedCount++;

                // �Ĺ����� ���� ����Ʈ �߰�
                if (arPlantManager != null)
                    arPlantManager.AddGrowthPoints(15f, $"����: {recognizedText}");

                UpdateVoiceStatus($"����! \"{recognizedText}\"");

                // ��ǥ �Ϸ� üũ
                if (completedCount >= REQUIRED_COUNT)
                {
                    completedCount = 0;
                    currentTargetIndex++;

                    if (currentTargetIndex >= targetPhrases.Count)
                    {
                        UpdateVoiceStatus("��� ��ǥ�� �Ϸ��߽��ϴ�!");
                    }
                }
                ShowCurrentTarget();
            }
            else
            {
                UpdateVoiceStatus("�ٽ� �õ��غ�����");
            }
        }
        StopListening();
    }
    
    private bool IsTextMatch(string input, string target)
    {
        // ������ ��Ī ����
        string cleanInput = input.ToLower().Replace(" ", "");
        string cleanTarget = target.ToLower().Replace(" ", "");

        // ���� ��ġ
        if (cleanInput == cleanTarget) return true;

        // �κ� ��ġ (70% �̻�)
        if (cleanInput.Contains(cleanTarget) || cleanTarget.Contains(cleanInput))
            return true;

        // ������ �ܾ ���ԵǾ� ������ ����
        foreach(string positiveWord in positiveWords)
        {
            if (cleanInput.Contains(positiveWord.ToLower()))
                return true;
        }
        return false;
    }

    
    private void UpdateVoiceStatus(string status)
    {
        if (voiceStatusText != null)
            voiceStatusText.text = status;

        Debug.Log($"���� ����: {status}");
    }

    // ���� �޼����
    public void ResetVoiceTargets()
    {
        currentTargetIndex = 0;
        completedCount = 0;
        ShowCurrentTarget();
        UpdateVoiceStatus("���� �ν� �ʱ�ȭ��");
    }

}
