using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class MBTIDropdownUI : MonoBehaviour
{
    [Header("MBTI �Է� UI")]
    public GameObject mbtiInputPanel;
    public TMP_Dropdown mbtiDropdown;
    public Button confirmButton;
    public Button cancelButton;
    public TextMeshProUGUI titleText;

    // �̺�Ʈ
    public System.Action<string> OnMBTIConfirmed;
    public System.Action OnCancelled;

    private List<string> mbtiOptions = new List<string>
    {
        "ENFP - Ȱ���� ������", "ENFJ - ���Ƿο� ��ȸ���", "ENTP - �߰ſ� ������ ���� ���а�",
        "ENTJ - ����� �����", "ESFP - �����ο� ��ȥ�� ������", "ESFJ - �米���� �ܱ���",
        "ESTP - ������ ���� �����", "ESTJ - ������ ������", "INFP - �������� ������",
        "INFJ - ������ ��ȣ��", "INTP - ������ �����", "INTJ - �����ֵ��� ������",
        "ISFP - ȣ��� ���� ������", "ISFJ - �밨�� ��ȣ��", "ISTP - ���� ���ֲ�", "ISTJ - ����������"
    };

    private void Start()
    {
        SetupUI();
        ConnectEvents();
    }

    private void SetupUI()
    {
        // �ʱ⿡�� �г� ����
        if (mbtiInputPanel != null)
            mbtiInputPanel.SetActive(false);

        // ���� ����
        if (titleText != null)
            titleText.text = "����� MBTI�� �������ּ���";
        SetupDropdown();
    }

    private void SetupDropdown()
    {
        if (mbtiDropdown == null) return;

        // ��Ӵٿ� �ʱ�ȭ
        mbtiDropdown.ClearOptions();

        // ù ��° �ɼ��� �ȳ� �޽���
        List<string> dropdownOptions = new List<string> { "MBTI�� �������ּ���" };
        dropdownOptions.AddRange(mbtiOptions);

        mbtiDropdown.AddOptions(dropdownOptions);

        // �ʱⰪ ���� (ù ��° �ȳ� �޽���)
        mbtiDropdown.value = 0;
        mbtiDropdown.RefreshShownValue();

        // ��Ӵٿ� ��Ÿ�� ����
        SetupDropdownStyle();
    }

    private void SetupDropdownStyle()
    {
        if (mbtiDropdown == null) return;

        // ��Ӵٿ� ũ�� �� ��Ÿ�� ����
        var rect = mbtiDropdown.GetComponent<RectTransform>();
        if (rect != null)
            rect.sizeDelta = new Vector2(400, 60);

        // �ؽ�Ʈ ũ�� ����
        if (mbtiDropdown.captionText != null)
            mbtiDropdown.captionText.fontSize = 18;

        if (mbtiDropdown.itemText != null)
            mbtiDropdown.itemText.fontSize = 16;
    }

    private void ConnectEvents()
    {
        // ��ư �̺�Ʈ ����
        if (confirmButton != null)
            confirmButton.onClick.AddListener(OnConfirm);

        if (cancelButton != null)
            cancelButton.onClick.AddListener(OnCancel);

        // ��Ӵٿ� �� ���� �̺�Ʈ
        if (mbtiDropdown != null)
            mbtiDropdown.onValueChanged.AddListener(OnDropdownChanged);
    }

    private void OnDropdownChanged(int index)
    {
        // Ȯ�� ��ư Ȱ��ȭ/��Ȱ��ȭ
        if (confirmButton != null)
        {
            // ù ��° �ɼ�(�ȳ� �޽���)�� �ƴ� ��쿡�� Ȱ��ȭ
            confirmButton.interactable = index > 0;
        }

        Debug.Log($"Selected: {(index > 0 ? mbtiOptions[index - 1] : "None")}");
    }

    private void OnConfirm()
    {
        int selectedIndex = mbtiDropdown.value;

        // ù ��° �ɼ�(�ȳ� �޽���)�� ���õ� ���
        if (selectedIndex <= 0)
        {
            ShowWarning("MBTI�� �������ּ���!");
            return;
        }

        string mbtiCode = ExtractMBTICode(mbtiOptions[selectedIndex - 1]);
        OnMBTIConfirmed?.Invoke(mbtiCode);

        Debug.Log($"Confirmed MBTI : {mbtiCode}");

        HidePanel();
    }

    private void OnCancel()
    {
        HidePanel();
        OnCancelled?.Invoke();
    }

    private string ExtractMBTICode(string optionText)
    {
        return optionText.Split(' ')[0];
    }

    private void ShowWarning(string message)
    {
        if (titleText != null)
            StartCoroutine(ShowWarningCoroutine(message));
    }

    private IEnumerator ShowWarningCoroutine(string message)
    {
        string original = titleText.text;
        Color originalColor = titleText.color;

        titleText.text = message;
        titleText.color = Color.red;

        yield return new WaitForSeconds(2f);

        titleText.text = original;
        titleText.color = originalColor;
    }


    // ���� �޼����
    public void ShowPanel()
    {
        if (mbtiInputPanel != null) mbtiInputPanel.SetActive(true);

        ResetDropdown();
    }

    public void HidePanel()
    {
        if (mbtiInputPanel != null) mbtiInputPanel.SetActive(false);
    }

    private void ResetDropdown()
    {
        if(mbtiDropdown != null)
        {
            mbtiDropdown.value = 0;
            mbtiDropdown.RefreshShownValue();
        }

        // Ȯ�� ��ư ��Ȱ��ȭ
        if (confirmButton != null) confirmButton.interactable = false;
    }
}
