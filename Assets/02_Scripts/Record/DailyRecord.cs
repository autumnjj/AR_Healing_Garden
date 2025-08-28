using UnityEngine;
using System.Collections.Generic;
using System;

[System.Serializable]
public class DailyRecord
{
    [Header("�⺻ ����")]
    public string date;
    public bool didSpeak;
    public int voiceCount;

    [Header("�Ĺ� ����")]
    public string plantType;
    public PlantGrowthStage plantStage;

    // ������
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

    // ��ȭ ��� �߰�
    public void AddVoice()
    {
        didSpeak = true;
        voiceCount++;

        Debug.Log($"{date}: ��ȭ ��� �߰�(�� {voiceCount}ȸ");
    }

    // �Ĺ� ���� ������Ʈ
    public void UpdatePlantInfo(string type, PlantGrowthStage stage)
    {
        plantType = type;
        plantStage = stage;
    }

    // ������ ��� ����
    public string GetSimpleSummary()
    {
        if (!didSpeak)
            return $"{date}: ���� �Ĺ��� ��ȭ���� �ʾҾ��";

        return $"{date}: �Ĺ����� {voiceCount}�� ���� �ɾ����";
    }

    // �ѱ��� ��¥ ǥ��
    public string GetKoreanDate()
    {
        if (DateTime.TryParse(date, out DateTime dateTime))
        {
            return dateTime.ToString("M�� d��");
        }
        return date;
    }

    // �Ĺ� ���� �ѱ��� �̸�
    public string GetPlantStageKorean()
    {
        switch (plantStage)
        {
            case PlantGrowthStage.Seed: return "����";
            case PlantGrowthStage.Sprout: return "����";
            case PlantGrowthStage.Growing: return "����";
            case PlantGrowthStage.Blooming: return "��ȭ";
            default: return "�� �� ����";
        }
    }

    // �������� Ȯ��
    public bool IsToday()
    {
        return date == DateTime.Now.ToString("yyyy-MM-dd");
    }

    // �̹� ������ Ȯ��
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
