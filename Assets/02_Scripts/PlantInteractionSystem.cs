using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Collections;

public class PlantInteractionSystem : MonoBehaviour
{
    [Header("상호작용 버튼들")]
    public Button talkButton;
    public Button waterButton;
    public Button touchButton;
    public Button sunlightButton;

    [Header("참조 컴포넌트")]
    public PlantGrowthData plantGrowthData;
    public PlantGrowthUI plantGrowthUI;

    [Header("상호작용 설정")]
    public bool enablekeyboardShortcuts = true;

    // 상호작용 메시지 딕셔너리
    private Dictionary<InteractionType, List<string>> interactionMessages = new Dictionary<InteractionType, List<string>>();

    private void Start()
    {
        SetupInteractionButtons();
        SetupInteractionMessages();

        // 이벤트 연결
        if(plantGrowthData != null)
        {
            plantGrowthData.OnInteractionPerformed += HandleInteractionFeedback;
        }
    }

    private void SetupInteractionButtons()
    {
        if (talkButton != null)
        {
            talkButton.onClick.AddListener(() => PerformInteraction(InteractionType.PositiveTalk));
        }
        if (waterButton != null)
        {
            waterButton.onClick.AddListener(() => PerformInteraction(InteractionType.WaterGiving));
        }
        if (touchButton != null)
        {
            touchButton.onClick.AddListener(() => PerformInteraction(InteractionType.TouchCare));
        }
        if (sunlightButton != null)
        {
            sunlightButton.onClick.AddListener(() => PerformInteraction(InteractionType.SunlightGiving));
        }
    }

    private void SetupInteractionMessages()
    {
        interactionMessages[InteractionType.PositiveTalk] = new List<string>
        {
            "따뜻한 말을 해주셔서 고마워요!",
            "긍정적인 에너지를 받아요!",
            "당신의 응원이 힘이 돼요!",
            "사랑스러운 말에 기분이 좋아져요!"
        };
        interactionMessages[InteractionType.WaterGiving] = new List<string>
        {
            "시원한 물을 마시고 기분이 좋아요!",
            "갈증이 해소되어 시원해요!",
            "물을 주셔서 고마워요!",
            "촉촉해져서 활력이 넘쳐요!"
        };
        interactionMessages[InteractionType.TouchCare] = new List<string>
        {
            "부드러운 터치가 따뜻해요!",
            "손길이 너무 포근해요!",
            "사랑스러운 터치에 기분이 좋아져요!",
            "따뜻한 손길이 느껴져요!"    
        };
        interactionMessages[InteractionType.SunlightGiving] = new List<string>
        {
            "따뜻한 햇빛을 받아 활력이 넘쳐요!",
            "햇빛이 따뜻하고 좋아요!",
            "광합성이 활발해져요!",
            "밝은 빛 덕분에 에너지가 충전돼요!"
        };
    }

    private void Update()
    {
        if (enablekeyboardShortcuts)
        {
            HandleKeyboardInput();
        }
    }

    private void HandleKeyboardInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Q))
        {
            PerformInteraction(InteractionType.PositiveTalk);
        }

        if(Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.W))
        {
            PerformInteraction(InteractionType.WaterGiving);
        }
        if(Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.E))
        {
            PerformInteraction(InteractionType.TouchCare);
        }
        if(Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.R))
        {
            PerformInteraction(InteractionType.SunlightGiving);
        }

        // 리셋 가능(개발/테스트용)
        if(Input.GetKeyDown(KeyCode.F5))
        {
            ResetPlant();
        }
    }

    public void PerformInteraction(InteractionType interactionType)
    {
        if (plantGrowthData == null)
        {
            Debug.LogError("PlantGrowthData가 설정되지 않았습니다.");
            return;
        }

        // 상호작용 가능 여부 확인
        if (!plantGrowthData.CanInteract())
        {
            ShowMessage("조금 기다려주세요!", Color.yellow);
            return;
        }

        // 이미 최대 성장 단계 확인
        if(plantGrowthData.IsMaxGrowthStage())
        {
            ShowMessage("이미 완전히 성장한 아름다운 모습이에요!", Color.green);
            return;
        }

        // 상호작용 수행
        float pointsToAdd = plantGrowthData.GetInteractionPoints(interactionType);
        plantGrowthData.AddGrowthPoints(pointsToAdd, interactionType);

        // 상호작용 메시지 표시
        ShowInteractionMessage(interactionType);

        // 버튼 피드백 효과
        StartCoroutine(ButtonFeedbackEffect(GetButtonForInteraction(interactionType)));
    }

    private void ShowInteractionMessage(InteractionType interactionType)
    {
        string message = "";

        // 식물별 케어 메시지 우선 사용
        if(interactionType == InteractionType.PositiveTalk && plantGrowthData.plantData.careMessages.Count > 0)
        {
            message = plantGrowthData.plantData.careMessages[Random.Range(0, plantGrowthData.plantData.careMessages.Count)];
        }
        else if(interactionMessages.ContainsKey(interactionType))
        {
            var messages = interactionMessages[interactionType];
            message = messages[Random.Range(0, messages.Count)];
        }
        else
        {
            message = "고마워요!";
        }

        ShowMessage(message, Color.white);

    }

    private void HandleInteractionFeedback(InteractionType interactionType, float points)
    {
        // 상호작용 성공 시 추가 피드백
        string pointsMessage = $"+{points:F0} 성장 포인트";

        if(plantGrowthUI != null)
        {
            plantGrowthUI.ShowPointsGainedEffect(points);
        }

        // 상호작용 타입별 특별 효과
        switch (interactionType)
        {
            case InteractionType.PositiveTalk:
                PlayInteractionEffect("talk");
                break;
            case InteractionType.WaterGiving:
                PlayInteractionEffect("water");
                break;
            case InteractionType.TouchCare:
                PlayInteractionEffect("touch");
                break;
            case InteractionType.SunlightGiving:
                PlayInteractionEffect("sunlight");
                break;
        }
    }

    private void PlayInteractionEffect(string effectType)
    {
        // 나중에 파티클 시스템이나 애니메이션 효과 추가
        Debug.Log($"Playing {effectType} effect");
    }

    Button GetButtonForInteraction(InteractionType interactionType)
    {
        switch (interactionType)
        {
            case InteractionType.PositiveTalk:
                return talkButton;
            case InteractionType.WaterGiving:
                return waterButton;
            case InteractionType.TouchCare:
                return touchButton;
            case InteractionType.SunlightGiving:
                return sunlightButton;
            default:
                return null;
        }
    }

    IEnumerator ButtonFeedbackEffect(Button button)
    {
        if (button == null) yield break;

        Vector3 originalScale = button.transform.localScale;
        Vector3 pressedScale = originalScale * 0.9f;

        // 버튼 누름 효과
        float duration = 0.1f;
        float elaspedTime = 0f;

        while(elaspedTime < duration)
        {
            float t = elaspedTime / duration;
            button.transform.localScale = Vector3.Lerp(originalScale, pressedScale, t);
            elaspedTime += Time.deltaTime;
            yield return null;
        }

        // 원래 크기로 복원
        elaspedTime = 0f;
        while(elaspedTime < duration)
        {
            float t = elaspedTime / duration;
            button.transform.localScale = Vector3.Lerp(pressedScale, originalScale, t);
            elaspedTime += Time.deltaTime;
            yield return null;
        }

        button.transform.localScale = originalScale; 
    }

    private void ShowMessage(string message, Color color)
    {
        if (plantGrowthUI != null)
        {
            plantGrowthUI.ShowMessage(message, color);
        }
        else
        {
            Debug.Log($"Message : {message}");
        }
    }

    public void ResetPlant()
    {
        if(plantGrowthData != null)
        {
            plantGrowthData.ResetToDefault();
            ShowMessage("식물이 초기화되었습니다.", Color.cyan);
        }
    }

    // 상호작용 통계 정보
    public string GetInteractionStats()
    {
        if (plantGrowthData == null)
        {
            return "데이터 없음";
        }

        var state = plantGrowthData.GetCurrentState();
        string stats = $"총 상호작용 : {state.totalInteractions}\n";

        foreach(var kvp in state.interactionCounts)
        {
            stats += $"{kvp.Key}: {kvp.Value}회\n";
        }
        return stats;
    }

    // 버튼 활성화/비활성화
    public void SetInteractionEnabled(bool enabled)
    {
        if(talkButton != null) talkButton.interactable = enabled;
        if (waterButton != null) waterButton.interactable = enabled;
        if (touchButton != null) touchButton.interactable = enabled;
        if (sunlightButton != null) sunlightButton.interactable = enabled;
    }

    private void OnDestroy()
    {
        // 이벤트 해제
        if (plantGrowthData != null)
        {
            plantGrowthData.OnInteractionPerformed -= HandleInteractionFeedback;
        }
    }
}
