using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using NUnit.Framework;
using System.Transactions;

public class ARPlantVoiceController : MonoBehaviour
{
    [Header("핵심 컴포넌트")]
    public ARPlantManager arPlantManager;

    [Header("UI")]
    public Button voiceButton;
    public TextMeshProUGUI voiceStatusText;
    public TextMeshProUGUI currentTargetText;

    [Header("긍정적 단어 목록")]
    public List<string> positiveWords = new List<string>
    {
        "예쁘다", "좋이", "잘한다", "고마워", "사랑해",
        "멋지다", "훌륭하다", "최고야", "아름답다", "소중하다"
    };

    [Header("목표 문장들")]
    public List<string> targetPhrases = new List<string>
    {
        "너는 소중해", "잘하고 있어", "고마워", "무럭무럭 자라"
    };


    // 상태 관리
    private bool isListening = false;
    private int currentTargetIndex = 0;
    private int completedCount = 0;
    private const int REQUIRED_COUNT = 3;

    // 음성 인식 (시뮬레이션용)
    private bool isSimulationMode = true;

    private void Start()
    {
        SetupUI();
        ShowCurrentTarget();
    }


    private void SetupUI()
    {
        // 버튼 이벤트 연결
        if (voiceButton != null)
            voiceButton.onClick.AddListener(ToggleListening);

        UpdateVoiceStatus("음성 인식 준비됨");
    }

    
    private void ShowCurrentTarget()
    {
        if (currentTargetIndex < targetPhrases.Count)
        {
            string target = targetPhrases[currentTargetIndex];
            if (currentTargetText != null)
            {
                currentTargetText.text = $"따라 말해보세요: \"{target}\" ({completedCount}/{REQUIRED_COUNT})";
             }
        }
        else
        {
            if (currentTargetText != null)
            {
                currentTargetText.text = "모든 목표를 완료했습니다!";
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
        UpdateVoiceStatus("듣고 있습니다...");

        // 실제 음성 인식 구현은 여기에 추가
    }

    private void StopListening()
    {
        isListening = false;
        UpdateVoiceStatus("음성 인식 중지됨");
    }

    public void OnVoiceRecognitionSuccess(string recognizedText)
    {
        if (!isListening) return;

        Debug.Log($"Voice Reocogn Result : {recognizedText}");

        // 현재 목표 문장과 비교
        if (currentTargetIndex < targetPhrases.Count)
        {
            string currentTarget = targetPhrases[currentTargetIndex];

            if (IsTextMatch(recognizedText, currentTarget))
            {
                completedCount++;

                // 식물에게 성장 포인트 추가
                if (arPlantManager != null)
                    arPlantManager.AddGrowthPoints(15f, $"음성: {recognizedText}");

                UpdateVoiceStatus($"성공! \"{recognizedText}\"");

                // 목표 완료 체크
                if (completedCount >= REQUIRED_COUNT)
                {
                    completedCount = 0;
                    currentTargetIndex++;

                    if (currentTargetIndex >= targetPhrases.Count)
                    {
                        UpdateVoiceStatus("모든 목표를 완료했습니다!");
                    }
                }
                ShowCurrentTarget();
            }
            else
            {
                UpdateVoiceStatus("다시 시도해보세요");
            }
        }
        StopListening();
    }
    
    private bool IsTextMatch(string input, string target)
    {
        // 간단한 매칭 로직
        string cleanInput = input.ToLower().Replace(" ", "");
        string cleanTarget = target.ToLower().Replace(" ", "");

        // 완전 일치
        if (cleanInput == cleanTarget) return true;

        // 부분 일치 (70% 이상)
        if (cleanInput.Contains(cleanTarget) || cleanTarget.Contains(cleanInput))
            return true;

        // 긍정적 단어가 포함되어 있으면 인정
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

        Debug.Log($"음성 상태: {status}");
    }

    // 공개 메서드들
    public void ResetVoiceTargets()
    {
        currentTargetIndex = 0;
        completedCount = 0;
        ShowCurrentTarget();
        UpdateVoiceStatus("음성 인식 초기화됨");
    }

}
