using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class PlantGrowthUI : MonoBehaviour
{
    [Header("�Ĺ� ���÷���")]
    public Image plantImage; 
    public TextMeshProUGUI plantNameText;
    public TextMeshProUGUI stageDescriptionText;

    [Header("���� ���൵ UI")]
    public Slider growthProgressBar;
    public TextMeshProUGUI growthPointsText;
    public TextMeshProUGUI currentStageText;

    [Header("�޽��� UI")]
    public TextMeshProUGUI messageText;
    public float messageDisplayTime = 3f;

    [Header("����Ʈ ȹ�� ȿ��")]
    public TextMeshProUGUI pointsGainedText;
    public AnimationCurve pointsAnimationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("���� ������Ʈ")]
    public PlantGrowthData plantGrowthData;

    [Header("�ִϸ��̼� ����")]
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
        // ����Ʈ ȹ�� �ؽ�Ʈ �ʱ� ����
        if(pointsGainedText != null)
        {
            pointsGainedText.gameObject.SetActive(false);
        }

        // ���α׷��� �� ����
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

        // �Ĺ� �̸� ǥ��
        if(plantNameText != null && plantGrowthData.plantData != null)
        {
            plantNameText.text = plantGrowthData.plantData.koreanName;
        }

        // ���� ���� ǥ��
        var currentState = plantGrowthData.GetCurrentState();
        var stageData = plantGrowthData.GetCurrentStageData();

        UpdatePlantDisplay(currentState.currentStage, stageData);
        UpdateGrowthProgress(currentState.currentGrowthPoints);
    }

    private void HandleStageChanged(PlantGrowthStage newStage)
    {
        var stageData = plantGrowthData.GetStageData(newStage);
        UpdatePlantDisplay(newStage, stageData);

        // ���� �ܰ� ��ȭ �ִϸ��̼�
        StartCoroutine(StageTransitionAnimation(stageData));

        // ���� ���� �޽���
        string celebrationMessage = $"{GetStageKoreanName(newStage)} �ܰ�� �����߾��!";
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

        // �Ĺ� �̹��� ������Ʈ
        if (plantImage != null)
        {
            if(stageData.stageSprite != null)
            {
                plantImage.sprite = stageData.stageSprite;
            }

            // ���� ����
            plantImage.color = stageData.stageColor;

            // ũ�� ����
            plantImage.transform.localScale = stageData.stageScale;
        }

        // �ܰ� ���� ������Ʈ
        if(stageDescriptionText != null)
        {
            stageDescriptionText.text = stageData.stageDescription;
        }

        // ���� �ܰ� �ؽ�Ʈ ������Ʈ
        if(currentStageText != null)
        {
            currentStageText.text = $"���� �ܰ� : {GetStageKoreanName(stage)}";
        }
    }

    private void UpdateGrowthProgress(float currentPoints)
    {
        if (plantGrowthData == null) return;

        var settings = plantGrowthData.growthSettings;

        // ���α׷��� �� ������Ʈ
        if(growthProgressBar != null)
        {
            float progress = currentPoints / settings.maxGrowthPoints;
            growthProgressBar.value = progress;
        }

        // ����Ʈ �ؽ�Ʈ ������Ʈ
        if(growthPointsText != null)
        {
            growthPointsText.text = $"���� ����Ʈ : {currentPoints:F0} / {settings.maxGrowthPoints:F0}";
        }

    }

    public void ShowMessage(string message, Color color)
    {
        if (messageText == null) return;

        // ���� �޽��� �ڷ�ƾ ����
        if(currentMessageCoroutine != null)
        {
            StopCoroutine(currentMessageCoroutine);
        }

        // �� �޽��� ǥ��
        messageText.text = message;
        messageText.color = color;

        currentMessageCoroutine = StartCoroutine(FadeOutMessage());
    }

    public void ShowPointsGainedEffect(float points)
    {
        if (pointsGainedText == null) return;

        // ���� ȿ�� ����
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
            case PlantGrowthStage.Seed: return "����";
            case PlantGrowthStage.Sprout: return "����";
            case PlantGrowthStage.Growing: return "����";
            case PlantGrowthStage.Blooming: return "��ȭ";
            default: return "�� �� ����";
        }
    }

    IEnumerator StageTransitionAnimation(PlantGrowthStageData stageData)
    {
        if(plantImage == null) yield break;

        Vector3 originalScale = plantImage.transform.localScale;
        Vector3 targetScale = stageData.stageScale;

        // �ܰ躰 ��¦�� ȿ��
        for (int i = 0; i < 3; i++) 
        {
            plantImage.color = Color.white;
            yield return new WaitForSeconds(0.1f);
            plantImage.color = stageData.stageColor;
            yield return new WaitForSeconds(0.1f);
        }

        // ũ�� ��ȭ �ִϸ��̼�
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
        Vector3 endPosition = startPosition + Vector3.up * 100f; // ���� 100�ȼ� �̵�

        Color startColor = Color.green;
        Color endColor = new Color(startColor.r, startColor.g, startColor.b, 0f);

        float duration = 1f;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            float t = elapsedTime / duration;
            float curveValue = pointsAnimationCurve.Evaluate(t);

            // ��ġ �̵�
            pointsGainedText.transform.position = Vector3.Lerp(startPosition, endPosition, curveValue);

            // ���� ���̵� �ƿ�
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

    // ���� �޼����
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
