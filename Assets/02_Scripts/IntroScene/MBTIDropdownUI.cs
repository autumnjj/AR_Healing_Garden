using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class MBTIDropdownUI : MonoBehaviour
{
    [Header("MBTI 입력 UI")]
    public GameObject mbtiInputPanel;
    public TMP_Dropdown mbtiDropdown;
    public Button confirmButton;
    public Button cancelButton;
    public TextMeshProUGUI titleText;

    // 이벤트
    public System.Action<string> OnMBTIConfirmed;
    public System.Action OnCancelled;

    private List<string> mbtiOptions = new List<string>
    {
        "ENFP - 활발한 연예인", "ENFJ - 정의로운 사회운동가", "ENTP - 뜨거운 논쟁을 즐기는 변론가",
        "ENTJ - 대담한 통솔자", "ESFP - 자유로운 영혼의 연예인", "ESFJ - 사교적인 외교관",
        "ESTP - 모험을 즐기는 사업가", "ESTJ - 엄격한 관리자", "INFP - 열정적인 중재자",
        "INFJ - 선의의 옹호자", "INTP - 논리적인 사색가", "INTJ - 용의주도한 전략가",
        "ISFP - 호기심 많은 예술가", "ISFJ - 용감한 수호자", "ISTP - 만능 재주꾼", "ISTJ - 현실주의자"
    };

    private void Start()
    {
        SetupUI();
        ConnectEvents();
    }

    private void SetupUI()
    {
        // 초기에는 패널 숨김
        if (mbtiInputPanel != null)
            mbtiInputPanel.SetActive(false);

        // 제목 설정
        if (titleText != null)
            titleText.text = "당신의 MBTI를 선택해주세요";
        SetupDropdown();
    }

    private void SetupDropdown()
    {
        if (mbtiDropdown == null) return;

        // 드롭다운 초기화
        mbtiDropdown.ClearOptions();

        // 첫 번째 옵션은 안내 메시지
        List<string> dropdownOptions = new List<string> { "MBTI를 선택해주세요" };
        dropdownOptions.AddRange(mbtiOptions);

        mbtiDropdown.AddOptions(dropdownOptions);

        // 초기값 설정 (첫 번째 안내 메시지)
        mbtiDropdown.value = 0;
        mbtiDropdown.RefreshShownValue();

        // 드롭다운 스타일 설정
        SetupDropdownStyle();
    }

    private void SetupDropdownStyle()
    {
        if (mbtiDropdown == null) return;

        // 드롭다운 크기 및 스타일 설정
        var rect = mbtiDropdown.GetComponent<RectTransform>();
        if (rect != null)
            rect.sizeDelta = new Vector2(400, 60);

        // 텍스트 크기 설정
        if (mbtiDropdown.captionText != null)
            mbtiDropdown.captionText.fontSize = 18;

        if (mbtiDropdown.itemText != null)
            mbtiDropdown.itemText.fontSize = 16;
    }

    private void ConnectEvents()
    {
        // 버튼 이벤트 연결
        if (confirmButton != null)
            confirmButton.onClick.AddListener(OnConfirm);

        if (cancelButton != null)
            cancelButton.onClick.AddListener(OnCancel);

        // 드롭다운 값 변경 이벤트
        if (mbtiDropdown != null)
            mbtiDropdown.onValueChanged.AddListener(OnDropdownChanged);
    }

    private void OnDropdownChanged(int index)
    {
        // 확인 버튼 활성화/비활성화
        if (confirmButton != null)
        {
            // 첫 번째 옵션(안내 메시지)이 아닌 경우에만 활성화
            confirmButton.interactable = index > 0;
        }

        Debug.Log($"Selected: {(index > 0 ? mbtiOptions[index - 1] : "None")}");
    }

    private void OnConfirm()
    {
        int selectedIndex = mbtiDropdown.value;

        // 첫 번째 옵션(안내 메시지)이 선택된 경우
        if (selectedIndex <= 0)
        {
            ShowWarning("MBTI를 선택해주세요!");
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


    // 공개 메서드들
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

        // 확인 버튼 비활성화
        if (confirmButton != null) confirmButton.interactable = false;
    }
}
