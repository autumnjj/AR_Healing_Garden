using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class PlantGrowthUI : MonoBehaviour
{
    [Header("식물 디스플레이")]
    public Image plantImage; 
    public TextMeshProUGUI plantNameText;
    public TextMeshProUGUI stageDescriptionText;

    [Header("성장 진행도 UI")]
    public Slider growthProgressBar;
    public TextMeshProUGUI growthPointsText;
    public TextMeshProUGUI currentStageText;

    [Header("메시지 UI")]
    public TextMeshProUGUI messageText;
    public float messageDisplayTime = 3f;

    [Header("포인트 획득 효과")]
    public TextMeshProUGUI pointsGainedText;
    public AnimationCurve pointsAnimationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("참조 컴포넌트")]
    public PlantGrowthData plantGrowthData;

    [Header("애니메이션 설정")]
    public float stageTransitionDuration = 1f;
    public AnimationCurve scaleAnimationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Coroutine currentMessageCoroutine;
    private Coroutine currentPointsEffectCoroutine;

    private void Start()
    {
        SetupUI();
        ConnectEvents();
        InitializeDisplay();
    }

    private void SetupUI()
    {
        // 포인트 획득 텍스트 초기 설정
        if(pointsGainedText != null)
        {
            pointsGainedText.gameObject.SetActive(false);
        }

        // 프로그레스 바 설정
        if(growthProgressBar != null)
        {
            growthProgressBar.minValue = 0f;
            growthProgressBar.maxValue = 1f;
            growthProgressBar.value = 0f;
        }
    }

    private void ConnectEvents()
    {
        if (plantGrowthData != null)
        {
            plantGrowthData.OnStageChanged += HandleStageChanged;
            plantGrowthData.OnGrowthPointsChanged += HandleGrowthPointsChanged;
            plantGrowthData.OnMessageUpdate += HandleMessageUpdate;
        }
    }

    public void InitializeDisplay()
    {
        if (plantGrowthData != null) return;

        // 식물 이름 표시
        if(plantNameText != null && plantGrowthData.plantData != null)
        {
            plantNameText.text = plantGrowthData.plantData.koreanName;
        }

        // 현재 상태 표시
        var currentState = plantGrowthData.GetCurrentState();
        var stageData = plantGrowthData.GetCurrentStageData();

        UpdatePlantDisplay(currentState.currentStage, stageData);
        UpdateGrowthProgress(currentState.currentGrowthPoints);
    }

    private void HandleStageChanged(PlantGrowthStage newStage)
    {
        var stageData = plantGrowthData.GetStageData(newStage);
        UpdatePlantDisplay(newStage, stageData);

        // 성장 단계 변화 애니메이션
        StartCoroutine(StageTransitionAnimation(stageData));

        // 성장 축하 메시지
        string celebrationMessage = $"{GetStageKoreanName(newStage)} 단계로 성장했어요!";
        ShowMessage(celebrationMessage, Color.yellow);
    }

    private void HandleGrowthPointsChanged(float newPoints)
    {
        UpdateGrowthProgress(newPoints);
    }

    private void HandleMessageUpdate(string message)
    {
        ShowMessage(message, Color.white);
    }

    private void UpdatePlantDisplay(PlantGrowthStage stage, PlantGrowthStageData stageData)
    {
        if (stageData == null) return;

        // 식물 이미지 업데이트
        if (plantImage != null)
        {
            if(stageData.stageSprite != null)
            {
                plantImage.sprite = stageData.stageSprite;
            }

            // 색상 적용
            plantImage.color = stageData.stageColor;

            // 크기 적용
            plantImage.transform.localScale = stageData.stageScale;
        }

        // 단계 설명 업데이트
        if(stageDescriptionText != null)
        {
            stageDescriptionText.text = stageData.stageDescription;
        }

        // 현재 단계 텍스트 업데이트
        if(currentStageText != null)
        {
            currentStageText.text = $"성장 단계 : {GetStageKoreanName(stage)}";
        }
    }

    private void UpdateGrowthProgress(float currentPoints)
    {
        if (plantGrowthData == null) return;

        var settings = plantGrowthData.growthSettings;

        // 프로그레스 바 업데이트
        if(growthProgressBar != null)
        {
            float progress = currentPoints / settings.maxGrowthPoints;
            growthProgressBar.value = progress;
        }

        // 포인트 텍스트 업데이트
        if(growthPointsText != null)
        {
            growthPointsText.text = $"성장 포인트 : {currentPoints:F0} / {settings.maxGrowthPoints:F0}";
        }

    }

    public void ShowMessage(string message, Color color)
    {
        if (messageText == null) return;

        // 기존 메시지 코루틴 중지
        if(currentMessageCoroutine != null)
        {
            StopCoroutine(currentMessageCoroutine);
        }

        // 새 메시지 표시
        messageText.text = message;
        messageText.color = color;

        currentMessageCoroutine = StartCoroutine(FadeOutMessage());
    }

    public void ShowPointsGainedEffect(float points)
    {
        if (pointsGainedText == null) return;

        // 기존 효과 중지
        if(currentPointsEffectCoroutine != null)
        {
            StopCoroutine(currentPointsEffectCoroutine);
        }

        currentPointsEffectCoroutine = StartCoroutine(PointsGainedAnimation(points));
    }

    private string GetStageKoreanName(PlantGrowthStage stage)
    {
        switch (stage)
        {
            case PlantGrowthStage.Seed: return "씨앗";
            case PlantGrowthStage.Sprout: return "새싹";
            case PlantGrowthStage.Growing: return "성장";
            case PlantGrowthStage.Blooming: return "개화";
            default: return "알 수 없음";
        }
    }

    IEnumerator StageTransitionAnimation(PlantGrowthStageData stageData)
    {
        if(plantImage == null) yield break;

        Vector3 originalScale = plantImage.transform.localScale;
        Vector3 targetScale = stageData.stageScale;

        // 단계별 반짝임 효과
        for (int i = 0; i < 3; i++) 
        {
            plantImage.color = Color.white;
            yield return new WaitForSeconds(0.1f);
            plantImage.color = stageData.stageColor;
            yield return new WaitForSeconds(0.1f);
        }

        // 크기 변화 애니메이션
        float elapsedTime = 0f;
        while (elapsedTime < stageTransitionDuration)
        {
            float t = elapsedTime / stageTransitionDuration;
            float curveValue = scaleAnimationCurve.Evaluate(t);

            plantImage.transform.localScale = Vector3.Lerp(originalScale, targetScale, curveValue);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        plantImage.transform.localScale = targetScale;
    }

    IEnumerator PointsGainedAnimation(float points)
    {
        pointsGainedText.gameObject.SetActive(true);
        pointsGainedText.text = $"+{points:F0}";

        Vector3 startPosition = pointsGainedText.transform.position;
        Vector3 endPosition = startPosition + Vector3.up * 100f; // 위로 100픽셀 이동

        Color startColor = Color.green;
        Color endColor = new Color(startColor.r, startColor.g, startColor.b, 0f);

        float duration = 1f;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            float t = elapsedTime / duration;
            float curveValue = pointsAnimationCurve.Evaluate(t);

            // 위치 이동
            pointsGainedText.transform.position = Vector3.Lerp(startPosition, endPosition, curveValue);

            // 색상 페이드 아웃
            pointsGainedText.color = Color.Lerp(startColor, endColor, t);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        pointsGainedText.gameObject.SetActive(false);
        pointsGainedText.transform.position = startPosition; 
    }

    IEnumerator FadeOutMessage()
    {
        yield return new WaitForSeconds(messageDisplayTime);

        if (messageText != null)
        {
            Color originalColor = messageText.color;
            float fadeTime = 0.5f;
            float elapsedTime = 0f;

            while (elapsedTime < fadeTime)
            {
                float alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeTime);
                messageText.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            messageText.text = "";
            messageText.color = originalColor;
        }
    }

    // 공개 메서드들
    public void UpdatePlantImage(Sprite newSprite)
    {
        if (plantImage != null && newSprite != null)
        {
            plantImage.sprite = newSprite;
        }
    }

    public void SetPlantColor(Color color)
    {
        if (plantImage != null)
        {
            plantImage.color = color;
        }
    }

    public void SetPlantScale(Vector3 scale)
    {
        if (plantImage != null)
        {
            plantImage.transform.localScale = scale;
        }
    }
}
