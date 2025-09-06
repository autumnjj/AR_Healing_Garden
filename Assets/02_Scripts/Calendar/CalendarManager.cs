using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

[System.Serializable]
public class DailyRecord 
{
    public string date;
    public int speechCount;

    public DailyRecord(string date, int count)
    {
        this.date = date;
        this.speechCount = count;
    }
}

public class CalendarManager : MonoBehaviour
{
    [Header("Calendar UI")]
    public GameObject calendarPanel;
    public TextMeshProUGUI monthYearText;
    public Transform calendarGrid;
    public GameObject dayButtonPrefab;

    [Header("Navigation")]
    public Button prevMonthButton;
    public Button nextMonthButton;
    public Button closeButton;

    [Header("�Ĺ��� ���� �޽���")]
    public TextMeshProUGUI personalizedMessageText;
    public TextMeshProUGUI todayStatusText;

    [Header("Calendar Settings")]
    public int dailyGoal = 1;

    // ���� ǥ�� ���� ��¥
    private DateTime currentDisplayDate;

    // ��ȭ ��� ����
    private Dictionary<string, int> speechRecords = new Dictionary<string, int>();

    // ������ ��¥ ��ư��
    private List<GameObject> dayButtons = new List<GameObject>();

    // MBTI Ÿ��
    private string userPlantType = "";

    // MBTI ���� �޽���
    private readonly Dictionary<string, string[]> plantMessages = new Dictionary<string, string[]>
    {
        {
            "sunflower", new[]
            {
                "�عٶ��ó�� ��� �������� ���! ���õ� ȯ�ϰ� ��������!",
                "������ �޻� ���� ����� ������ ������ ��� ������!",
                "�عٶ�Ⱑ �¾��� �ٶ󺸵�, ��ŵ� �׻� ����� ���� ���ư�����!"
            }
        },
        {
            "lavender", new[]
            {
                "�󺥴�ó�� ����ϰ� ��ȭ�ο� ���! ������ ������ ã������!",
                "����� ������ �������� �ֺ��� �������� ���� ä����!",
                "�󺥴� ���ó�� ���������� ���� ����� ������ �����ؿ�!"
            }
        },
        {
            "cactus", new[]
            {
                "������ó�� �����ϰ� �������� ���! � �÷õ� �̰ܳ� �� �־��!",
                "�賭�� ȯ�濡���� ���� �ǿ�� ������ó��, ��ŵ� ������ �����ϰ� �־��",
                "�������� ������ó�� ������ ���! ��ǥ�� ���� ��鸲 ���� ���ư�����!"
            }
        },
        {
            "rose", new[]
            {
                "���ó�� ����ϰ� �Ƹ��ٿ� ���! �����θ� �� ������ּ���!",
                "���ð� �־ �Ƹ��ٿ� ���ó��, ����� ��� ���� �Ϻ��ؿ�!",
                "����� ���ó�� �����ϰ� �ŷ����� ���! �ڽŰ��� ��������"
            }
        }
    };

    private void Start()
    {
        currentDisplayDate = DateTime.Now;
        LoadUserPlantType();
        SetupButtons();
        LoadCalendarData();

        if (calendarPanel != null)
            calendarPanel.SetActive(false);
    }

   private void LoadUserPlantType()
    {
        // MBTI���� �Ĺ� Ÿ�� �߷�
        string mbti = PlayerPrefs.GetString("MBTI_Type", "ENFP");
        userPlantType = GetPlantTypeFromMBTI(mbti);
        Debug.Log("user MBTI: {mbti) -> Plant: {userPlantType");
    }

    private string GetPlantTypeFromMBTI(string mbti)
    {
        if (mbti == "ENFP" || mbti == "ENFJ" || mbti == "ESFP" || mbti == "ESFJ")
            return "sunflower";
        else if (mbti == "INFP" || mbti == "INFJ" || mbti == "ISFP" || mbti == "ISFJ")
            return "lavender";
        else if (mbti == "ENTJ" || mbti == "ENTP" || mbti == "INTJ" || mbti == "INTP")
            return "cactus";
        else
            return "rose";
    }
    private void SetupButtons()
    {
        if (prevMonthButton != null)
            prevMonthButton.onClick.AddListener(GoToPrevMonth);

        if (nextMonthButton != null)
            nextMonthButton.onClick.AddListener(GoToNextMonth);

        if (closeButton != null)
            closeButton.onClick.AddListener(HideCalendar);
    }

    public void RecordPlantCompletion()
    {
        string today = DateTime.Now.ToString("yyyy-MM-dd");

        // �Ĺ� �ϼ� = ��ǥ �޼�
        speechRecords[today] = dailyGoal;

        Debug.Log($"Plant Blooming! Today({today}) Goal reched record!");

        SaveCalendarData();

        // ���� ǥ�� ���� ���̸� ������Ʈ
        if (currentDisplayDate.Year == DateTime.Now.Year &&
                currentDisplayDate.Month == DateTime.Now.Month)
                UpdateCalendarDisplay();
    }

    // ���� ��ȭ���� �� ȣ���� �޼���
    public void RecordTodaySpeech()
    {
        RecordPlantCompletion();
    }

    private void UpdateCalendarDisplay()
    {
        // ���� ��ư�� ����
        ClearCalendarButtons();

        // ��/�� ǥ�� ������Ʈ
        if (monthYearText != null)
            monthYearText.text = currentDisplayDate.ToString("MMMM", System.Globalization.CultureInfo.InvariantCulture);

        // �ش� ���� ù���� �������� ���ϱ�
        DateTime firstDay = new DateTime(currentDisplayDate.Year, currentDisplayDate.Month, 1);
        DateTime lastDay = firstDay.AddMonths(1).AddDays(-1);

        // ù ���� ���� ���� (�Ͽ���=0)
        int startDayOfWeek = (int)firstDay.DayOfWeek;

        // �� ���� ���� (���� �� ������ ����)
        for (int i = 0; i < startDayOfWeek; i++)
        {
            CreateEmptyDayButton();
        }

        // ���� ��¥ ��ư�� ����
        for(int day = 1; day <= lastDay.Day; day++)
        {
            CreateDayButton(day);
        }

        UpdatePersonalizedMessage();
    }

    private void CreateDayButton(int day)
    {
        if (dayButtonPrefab == null || calendarGrid == null) return;

        GameObject dayBtn = Instantiate(dayButtonPrefab, calendarGrid);
        dayButtons.Add(dayBtn);

        // ��ư �ؽ�Ʈ ����
        TextMeshProUGUI dayText = dayBtn.GetComponentInChildren<TextMeshProUGUI>();
        Image flowerIcon = dayBtn.GetComponentInChildren<Image>();
        if(dayText != null)
        {
            dayText.text = day.ToString();
        }

        // �ش� ��¥�� ��ȭ ��� Ȯ��
        string dateKey = new DateTime(currentDisplayDate.Year, currentDisplayDate.Month, day).ToString("yyyy-MM-dd");
        int speechCount = speechRecords.ContainsKey(dateKey) ? speechRecords[dateKey] : 0;

        if(speechCount >= dailyGoal)
        {
            if (dayText != null)
                dayText.gameObject.SetActive(false);

            if (flowerIcon != null)
                flowerIcon.gameObject.SetActive(true);
        }
        else
        {
            if (dayText != null)
                dayText.gameObject.SetActive(true);
            if (flowerIcon != null)
                flowerIcon.gameObject.SetActive(false);
        }

        if (IsToday(day))
        {
            Outline outline = dayBtn.GetComponent<Outline>();
            if(outline == null)
            {
                outline = dayBtn.AddComponent<Outline>();
            }
            outline.effectColor = Color.deepPink;
            outline.effectDistance = new Vector2(3, 3);
        }
    }

    private void CreateEmptyDayButton()
    {
        if (dayButtonPrefab == null || calendarGrid == null) return;

        GameObject emptyBtn = Instantiate(dayButtonPrefab, calendarGrid);
        dayButtons.Add(emptyBtn);

        // �ؽ�Ʈ ����
        TextMeshProUGUI dayText = emptyBtn.GetComponentInChildren<TextMeshProUGUI>();
        Image flowerIcon = emptyBtn.GetComponentInChildren<Image>();
        if(dayText != null)
        {
            dayText.text = "";
            dayText.gameObject.SetActive(false);
        }
        if(flowerIcon != null)
            flowerIcon.gameObject .SetActive(false);

        // ��ư ��Ȱ��ȭ
        Button btn = emptyBtn.GetComponent<Button>();
        if (btn != null)
            btn.interactable = false;
    }

    private void UpdatePersonalizedMessage()
    {
        if (personalizedMessageText == null) return;

        if (plantMessages.ContainsKey(userPlantType))
        {
            var messages = plantMessages[userPlantType];
            string randomMessage = messages[UnityEngine.Random.Range(0, messages.Length)];
            personalizedMessageText.text = randomMessage;
        }
        else
        {
            personalizedMessageText.text = "����� �Ĺ��� ����� �����ϰ� �־��!";
        }

        if(todayStatusText != null)
        {
            int todayCount = GetTodaySpeechCount();
            if (todayCount >= dailyGoal)
                todayStatusText.text = $"���� �Ĺ� �ϼ�! ��ǥ �޼�!";
            else
                todayStatusText.text = $"���� ���ο� �Ĺ��� ����������!";
        }
    }

    private bool IsToday(int day)
    {
        var today = DateTime.Now;
        return currentDisplayDate.Year == today.Year &&
            currentDisplayDate.Month == today.Month &&
            day == today.Day;
    }
    

    private void ClearCalendarButtons()
    {
        foreach (GameObject btn in dayButtons)
        {
            if (btn != null)
                DestroyImmediate(btn);
        }
        dayButtons.Clear();
    }

    private void GoToPrevMonth()
    {
        currentDisplayDate = currentDisplayDate.AddMonths(-1);
        UpdateCalendarDisplay();
    }

    private void GoToNextMonth()
    {
        currentDisplayDate = currentDisplayDate.AddMonths(1);
        UpdateCalendarDisplay();
    }

    // ������ ����/�ε�
    private void SaveCalendarData()
    {
        List<string> dateKeys = new List<string>();
        List<int> counts = new List<int>();

        foreach(var record in speechRecords)
        {
            dateKeys.Add(record.Key);
            counts.Add(record.Value);
        }

        // JSON���� ����
        string datesJson = string.Join(",", dateKeys);
        string countsJson = string.Join(",", counts);

        PlayerPrefs.SetString("Calendar_Dates", datesJson);
        PlayerPrefs.SetString("Calendar_Counts", countsJson);
        PlayerPrefs.Save();
    }

    private void LoadCalendarData()
    {
        speechRecords.Clear();

        string datesJson = PlayerPrefs.GetString("Calendar_Dates", "");
        string countsJson = PlayerPrefs.GetString("Calendar_Counts", "");

        if (!string.IsNullOrEmpty(datesJson) && !string.IsNullOrEmpty(countsJson))
        {
            string[] dates = datesJson.Split(',');
            string[] counts = countsJson.Split(',');

            for(int i = 0; i < dates.Length && i < counts.Length; i++)
            {
                if (int.TryParse(counts[i], out int count))
                    speechRecords[dates[i]] = count;
            }
        }
    }

    // �ܺο��� ȣ���� ���� �޼����
    public void ShowCalendar()
    {
        if (calendarPanel != null)
        {
            calendarPanel.SetActive(true);
            UpdateCalendarDisplay();
        }
    }

    public void HideCalendar()
    {
        if (calendarPanel != null)
            calendarPanel.SetActive(false);
    }

    public int GetTodaySpeechCount()
    {
        string today = DateTime.Now.ToString("yyyy-MM-dd");
        return speechRecords.ContainsKey(today) ? speechRecords[today] : 0;
    }

    public int GetTotalSpeechCount()
    {
        int total = 0;
        foreach(var count in speechRecords.Values)
        {
            total += count;
        }
        return total;
    }
}
