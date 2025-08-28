using UnityEngine;
using System.Collections.Generic;
using System;

[System.Serializable]
public class DailyRecord
{
    [Header("기본 정보")]
    public string date;
    public bool didSpeak;
    public int voiceCount;

    [Header("식물 상태")]
    public string plantType;
    public PlantGrowthStage plantStage;

    // 생성자
    public DailyRecord()
    {
        date = DateTime.Now.ToString("yyyy-MM-dd");
        didSpeak = false;
        voiceCount = 0;
        plantType = "sunflower";
        plantStage = PlantGrowthStage.Seed;
    }

    public DailyRecord(string recordData)
    {
        date = recordData;
        didSpeak = false;
        voiceCount = 0;
        plantType = "sunflower";
        plantStage = PlantGrowthStage.Seed;
    }

    // 발화 기록 추가
    public void AddVoice()
    {
        didSpeak = true;
        voiceCount++;

        Debug.Log($"{date}: 발화 기록 추가(총 {voiceCount}회");
    }

    // 식물 상태 업데이트
    public void UpdatePlantInfo(string type, PlantGrowthStage stage)
    {
        plantType = type;
        plantStage = stage;
    }

    // 간단한 요약 정보
    public string GetSimpleSummary()
    {
        if (!didSpeak)
            return $"{date}: 아직 식물과 대화하지 않았어요";

        return $"{date}: 식물에게 {voiceCount}번 말을 걸었어요";
    }

    // 한국어 날짜 표시
    public string GetKoreanDate()
    {
        if (DateTime.TryParse(date, out DateTime dateTime))
        {
            return dateTime.ToString("M월 d일");
        }
        return date;
    }

    // 식물 상태 한국어 이름
    public string GetPlantStageKorean()
    {
        switch (plantStage)
        {
            case PlantGrowthStage.Seed: return "씨앗";
            case PlantGrowthStage.Sprout: return "새싹";
            case PlantGrowthStage.Growing: return "성장";
            case PlantGrowthStage.Blooming: return "개화";
            default: return "알 수 없음";
        }
    }

    // 오늘인지 확인
    public bool IsToday()
    {
        return date == DateTime.Now.ToString("yyyy-MM-dd");
    }

    // 이번 달인지 확인
    public bool IsThisMonth()
    {
        if (DateTime.TryParse(date, out DateTime recordDate))
        {
            DateTime now = DateTime.Now;
            return recordDate.Year == now.Year && recordDate.Month == now.Month;
        }
        return false;
    }

    public bool IsValid()
    {
        return !string.IsNullOrEmpty(date) && DateTime.TryParse(date, out _);
    }

}
