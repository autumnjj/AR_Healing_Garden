using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

public class CalendarPanelManager : MonoBehaviour
{
    [Header("Panel 컨트롤")]
    public GameObject calendarPanel;
    public Button backgroundButton;

    [Header("캘린더 UI 컴포넌트")]
    public TextMeshProUGUI monthYearText;
    public Button prevMonthButton;
    public Button nextMonthButton;
    public Transform calendarGrid;

    [Header("날짜 셀 프리팹")]
    public GameObject dayButtonPrefab;

    [Header("상세 정보 파업")]
    public GameObject detailPopup;
    public TextMeshProUGUI popupDateText;
    public TextMeshProUGUI popupContentText;
    public Button popupCloseButton;

    [Header("통계 UI")]
    public TextMeshProUGUI statsText;

    [Header("색상 설정")]
    public Color activeColor = Color.green;
    public Color inactiveColor = Color.gray;
    public Color todayColor = Color.yellow;
    public Color otherMonthColor = Color.clear;

    // 현재 표시 중인 월/년
    private DateTime currentDisplayMonth;
    //private List<CalendarDayButton> dayButtons = new List<CalendarDayButton>();

    // 패널 상태
    private bool isCalnedarOpen = false;

    // 한국어 월 이름
    private readonly string[] koreanMonths =
    {
        "", "1월", "2월", "3월", "4월", "5월", "6월",
        "7월", "8월", "9월", "10월", "11월", "12월"
    };


}
