using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Collections;

public class PlantInteractionSystem : MonoBehaviour
{
    [Header("��ȣ�ۿ� ��ư��")]
    public Button talkButton;
    public Button waterButton;
    public Button touchButton;
    public Button sunlightButton;

    [Header("���� ������Ʈ")]
    public PlantGrowthData plantGrowthData;
    public PlantGrowthUI plantGrowthUI;

    [Header("��ȣ�ۿ� ����")]
    public bool enablekeyboardShortcuts = true;

    // ��ȣ�ۿ� �޽��� ��ųʸ�
    private Dictionary<InteractionType, List<string>> interactionMessages = new Dictionary<InteractionType, List<string>>();

    private void Start()
    {
        SetupInteractionButtons();
        SetupInteractionMessages();

        // �̺�Ʈ ����
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
            "������ ���� ���ּż� ������!",
            "�������� �������� �޾ƿ�!",
            "����� ������ ���� �ſ�!",
            "��������� ���� ����� ��������!"
        };
        interactionMessages[InteractionType.WaterGiving] = new List<string>
        {
            "�ÿ��� ���� ���ð� ����� ���ƿ�!",
            "������ �ؼҵǾ� �ÿ��ؿ�!",
            "���� �ּż� ������!",
            "���������� Ȱ���� ���Ŀ�!"
        };
        interactionMessages[InteractionType.TouchCare] = new List<string>
        {
            "�ε巯�� ��ġ�� �����ؿ�!",
            "�ձ��� �ʹ� �����ؿ�!",
            "��������� ��ġ�� ����� ��������!",
            "������ �ձ��� ��������!"    
        };
        interactionMessages[InteractionType.SunlightGiving] = new List<string>
        {
            "������ �޺��� �޾� Ȱ���� ���Ŀ�!",
            "�޺��� �����ϰ� ���ƿ�!",
            "���ռ��� Ȱ��������!",
            "���� �� ���п� �������� �����ſ�!"
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

        // ���� ����(����/�׽�Ʈ��)
        if(Input.GetKeyDown(KeyCode.F5))
        {
            ResetPlant();
        }
    }

    public void PerformInteraction(InteractionType interactionType)
    {
        if (plantGrowthData == null)
        {
            Debug.LogError("PlantGrowthData�� �������� �ʾҽ��ϴ�.");
            return;
        }

        // ��ȣ�ۿ� ���� ���� Ȯ��
        if (!plantGrowthData.CanInteract())
        {
            ShowMessage("���� ��ٷ��ּ���!", Color.yellow);
            return;
        }

        // �̹� �ִ� ���� �ܰ� Ȯ��
        if(plantGrowthData.IsMaxGrowthStage())
        {
            ShowMessage("�̹� ������ ������ �Ƹ��ٿ� ����̿���!", Color.green);
            return;
        }

        // ��ȣ�ۿ� ����
        float pointsToAdd = plantGrowthData.GetInteractionPoints(interactionType);
        plantGrowthData.AddGrowthPoints(pointsToAdd, interactionType);

        // ��ȣ�ۿ� �޽��� ǥ��
        ShowInteractionMessage(interactionType);

        // ��ư �ǵ�� ȿ��
        StartCoroutine(ButtonFeedbackEffect(GetButtonForInteraction(interactionType)));
    }

    private void ShowInteractionMessage(InteractionType interactionType)
    {
        string message = "";

        // �Ĺ��� �ɾ� �޽��� �켱 ���
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
            message = "������!";
        }

        ShowMessage(message, Color.white);

    }

    private void HandleInteractionFeedback(InteractionType interactionType, float points)
    {
        // ��ȣ�ۿ� ���� �� �߰� �ǵ��
        string pointsMessage = $"+{points:F0} ���� ����Ʈ";

        if(plantGrowthUI != null)
        {
            plantGrowthUI.ShowPointsGainedEffect(points);
        }

        // ��ȣ�ۿ� Ÿ�Ժ� Ư�� ȿ��
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
        // ���߿� ��ƼŬ �ý����̳� �ִϸ��̼� ȿ�� �߰�
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

        // ��ư ���� ȿ��
        float duration = 0.1f;
        float elaspedTime = 0f;

        while(elaspedTime < duration)
        {
            float t = elaspedTime / duration;
            button.transform.localScale = Vector3.Lerp(originalScale, pressedScale, t);
            elaspedTime += Time.deltaTime;
            yield return null;
        }

        // ���� ũ��� ����
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
            ShowMessage("�Ĺ��� �ʱ�ȭ�Ǿ����ϴ�.", Color.cyan);
        }
    }

    // ��ȣ�ۿ� ��� ����
    public string GetInteractionStats()
    {
        if (plantGrowthData == null)
        {
            return "������ ����";
        }

        var state = plantGrowthData.GetCurrentState();
        string stats = $"�� ��ȣ�ۿ� : {state.totalInteractions}\n";

        foreach(var kvp in state.interactionCounts)
        {
            stats += $"{kvp.Key}: {kvp.Value}ȸ\n";
        }
        return stats;
    }

    // ��ư Ȱ��ȭ/��Ȱ��ȭ
    public void SetInteractionEnabled(bool enabled)
    {
        if(talkButton != null) talkButton.interactable = enabled;
        if (waterButton != null) waterButton.interactable = enabled;
        if (touchButton != null) touchButton.interactable = enabled;
        if (sunlightButton != null) sunlightButton.interactable = enabled;
    }

    private void OnDestroy()
    {
        // �̺�Ʈ ����
        if (plantGrowthData != null)
        {
            plantGrowthData.OnInteractionPerformed -= HandleInteractionFeedback;
        }
    }
}
