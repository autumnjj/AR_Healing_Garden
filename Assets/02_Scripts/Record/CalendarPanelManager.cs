using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

public class CalendarPanelManager : MonoBehaviour
{
    [Header("Panel ��Ʈ��")]
    public GameObject calendarPanel;
    public Button backgroundButton;

    [Header("Ķ���� UI ������Ʈ")]
    public TextMeshProUGUI monthYearText;
    public Button prevMonthButton;
    public Button nextMonthButton;
    public Transform calendarGrid;

    [Header("��¥ �� ������")]
    public GameObject dayButtonPrefab;

    [Header("�� ���� �ľ�")]
    public GameObject detailPopup;
    public TextMeshProUGUI popupDateText;
    public TextMeshProUGUI popupContentText;
    public Button popupCloseButton;

    [Header("��� UI")]
    public TextMeshProUGUI statsText;

    [Header("���� ����")]
    public Color activeColor = Color.green;
    public Color inactiveColor = Color.gray;
    public Color todayColor = Color.yellow;
    public Color otherMonthColor = Color.clear;

    // ���� ǥ�� ���� ��/��
    private DateTime currentDisplayMonth;
    //private List<CalendarDayButton> dayButtons = new List<CalendarDayButton>();

    // �г� ����
    private bool isCalnedarOpen = false;

    // �ѱ��� �� �̸�
    private readonly string[] koreanMonths =
    {
        "", "1��", "2��", "3��", "4��", "5��", "6��",
        "7��", "8��", "9��", "10��", "11��", "12��"
    };


}
