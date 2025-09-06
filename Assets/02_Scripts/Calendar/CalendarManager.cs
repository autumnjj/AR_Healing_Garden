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

    [Header("식물별 응원 메시지")]
    public TextMeshProUGUI personalizedMessageText;
    public TextMeshProUGUI todayStatusText;

    [Header("Calendar Settings")]
    public int dailyGoal = 1;

    // 현재 표시 중인 날짜
    private DateTime currentDisplayDate;

    // 대화 기록 저장
    private Dictionary<string, int> speechRecords = new Dictionary<string, int>();

    // 생성된 날짜 버튼들
    private List<GameObject> dayButtons = new List<GameObject>();

    // MBTI 타입
    private string userPlantType = "";

    // MBTI 맞춤 메시지
    private readonly Dictionary<string, string[]> plantMessages = new Dictionary<string, string[]>
    {
        {
            "sunflower", new[]
            {
                "해바라기처럼 밝고 긍정적인 당신! 오늘도 환하게 빛나세요!",
                "따뜻한 햇살 같은 당신의 마음이 세상을 밝게 만들어요!",
                "해바라기가 태양을 바라보듯, 당신도 항상 희망을 향해 나아가세요!"
            }
        },
        {
            "lavender", new[]
            {
                "라벤더처럼 고요하고 평화로운 당신! 마음의 안정을 찾으세요!",
                "당신의 차분한 에너지가 주변을 힐링으로 가득 채워요!",
                "라벤더 향기처럼 은은하지만 깊은 당신의 마음이 소중해요!"
            }
        },
        {
            "cactus", new[]
            {
                "선인장처럼 강인하고 독립적인 당신! 어떤 시련도 이겨낼 수 있어요!",
                "험난한 환경에서도 꽃을 피우는 선인장처럼, 당신도 멋지게 성장하고 있어요",
                "선인장의 의지력처럼 굳건한 당신! 목표를 향해 흔들림 없이 나아가세요!"
            }
        },
        {
            "rose", new[]
            {
                "장미처럼 우아하고 아름다운 당신! 스스로를 더 사랑해주세요!",
                "가시가 있어도 아름다운 장미처럼, 당신의 모든 면이 완벽해요!",
                "장미의 향기처럼 달콤하고 매력적인 당신! 자신감을 가지세요"
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
        // MBTI에서 식물 타입 추론
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

        // 식물 완성 = 목표 달성
        speechRecords[today] = dailyGoal;

        Debug.Log($"Plant Blooming! Today({today}) Goal reched record!");

        SaveCalendarData();

        // 현재 표시 중인 달이면 업데이트
        if (currentDisplayDate.Year == DateTime.Now.Year &&
                currentDisplayDate.Month == DateTime.Now.Month)
                UpdateCalendarDisplay();
    }

    // 오늘 대화했을 때 호출할 메서드
    public void RecordTodaySpeech()
    {
        RecordPlantCompletion();
    }

    private void UpdateCalendarDisplay()
    {
        // 기존 버튼들 제거
        ClearCalendarButtons();

        // 월/년 표시 업데이트
        if (monthYearText != null)
            monthYearText.text = currentDisplayDate.ToString("MMMM", System.Globalization.CultureInfo.InvariantCulture);

        // 해당 월의 첫날과 마지막날 구하기
        DateTime firstDay = new DateTime(currentDisplayDate.Year, currentDisplayDate.Month, 1);
        DateTime lastDay = firstDay.AddMonths(1).AddDays(-1);

        // 첫 주의 시작 요일 (일요일=0)
        int startDayOfWeek = (int)firstDay.DayOfWeek;

        // 빈 공간 생성 (이전 달 마지막 날들)
        for (int i = 0; i < startDayOfWeek; i++)
        {
            CreateEmptyDayButton();
        }

        // 실제 날짜 버튼들 생성
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

        // 버튼 텍스트 설정
        TextMeshProUGUI dayText = dayBtn.GetComponentInChildren<TextMeshProUGUI>();
        Image flowerIcon = dayBtn.GetComponentInChildren<Image>();
        if(dayText != null)
        {
            dayText.text = day.ToString();
        }

        // 해당 날짜의 대화 기록 확인
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

        // 텍스트 제거
        TextMeshProUGUI dayText = emptyBtn.GetComponentInChildren<TextMeshProUGUI>();
        Image flowerIcon = emptyBtn.GetComponentInChildren<Image>();
        if(dayText != null)
        {
            dayText.text = "";
            dayText.gameObject.SetActive(false);
        }
        if(flowerIcon != null)
            flowerIcon.gameObject .SetActive(false);

        // 버튼 비활성화
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
            personalizedMessageText.text = "당신의 식물이 당신을 응원하고 있어요!";
        }

        if(todayStatusText != null)
        {
            int todayCount = GetTodaySpeechCount();
            if (todayCount >= dailyGoal)
                todayStatusText.text = $"오늘 식물 완성! 목표 달성!";
            else
                todayStatusText.text = $"오늘 새로운 식물과 만나보세요!";
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

    // 데이터 저장/로드
    private void SaveCalendarData()
    {
        List<string> dateKeys = new List<string>();
        List<int> counts = new List<int>();

        foreach(var record in speechRecords)
        {
            dateKeys.Add(record.Key);
            counts.Add(record.Value);
        }

        // JSON으로 저장
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

    // 외부에서 호출할 공용 메서드들
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
