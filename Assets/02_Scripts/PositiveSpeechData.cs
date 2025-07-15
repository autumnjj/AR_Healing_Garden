using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class PositivePhrase
{
    public string text;
    public float growthPoints = 10f;
    public string category;

    public PositivePhrase(string text, float points, string category) 
    {
        this.text = text;
        this.growthPoints = points;
        this.category = category;
    }
    
}

[CreateAssetMenu(fileName = "PositiveSpeechData", menuName = "BloomSpeak/Positive Speech Data")]
public class PositiveSpeechData : ScriptableObject
{
    [Header("������ ��� ���")]
    public List<PositivePhrase> phrases = new List<PositivePhrase>();

    [Header("����")]
    public float matchThreshold = 0.6f;

    private void OnEnable()
    {
        if (phrases.Count == 0)
        {
            CreateDefaultPhrases();
        }
    }

    private void CreateDefaultPhrases()
    {
        phrases = new List<PositivePhrase>
        {
            // ������ ���
            new PositivePhrase("���� ������", 15f, "������"),
            new PositivePhrase("���� ��ġ�־�", 15f, "������"),
            new PositivePhrase("���� �����", 15f, "������"),

            // �ݷ�
            new PositivePhrase("���ϰ� �־�", 12f, "�ݷ�"),
            new PositivePhrase("����", 10f, "�ݷ�"),
            new PositivePhrase("������", 10f, "�ݷ�"),

            // ����� ����
            new PositivePhrase("�����", 14f, "���"),
            new PositivePhrase("����", 11f, "����"),
            new PositivePhrase("������", 11f, "����"),

            // ����
            new PositivePhrase("�������� �ڶ�", 13f, "����"),
            new PositivePhrase("���ڰ� �ڶ�", 13f, "����"),
            new PositivePhrase("�ǰ��ϰ� �ڶ�", 13f, "����"),

            // �⺻ ���� ǥ��
            new PositivePhrase("����", 10f, "Ī��"),
            new PositivePhrase("�Ϳ���", 10f, "Ī��"),
            new PositivePhrase("����", 8f, "Ī��")
        };
    }

    public PositivePhrase FindBestMatch(string inputText)
    {
        if (string.IsNullOrEmpty(inputText)) return null;

        inputText = inputText.ToLower().Trim();

        foreach (var pharse in phrases)
        {
            if (IsMatch(inputText, pharse.text)) return pharse;
        }
        return null;
    }

    private bool IsMatch(string input, string target)
    {
        target = target.ToLower();

        // ���� ��ġ
        if (input == target) return true;

        // ���� ����
        if (input.Contains(target) || target.Contains(input)) return true;

        // Ű���� ��Ī(������ ���)
        string[] inputWords = input.Split(' ');
        string[] targetWords = target.Split(' ');

        foreach (string inputWord in inputWords)
        {
            foreach (string targetWord in targetWords)
            {
                if (inputWord.Contains(targetWord) || targetWord.Contains(inputWord)) return true;
            }
        }

        return false;
    }

    public PositivePhrase GetRandomPhrase()
    {
        if(phrases.Count == 0) return null;
        return phrases[Random.Range(0, phrases.Count)];
    }

    public List<PositivePhrase> GetPhrasesByCategory(string category)
    {
        List<PositivePhrase> result = new List<PositivePhrase>();
        foreach(var phrase in phrases)
        {
            if (phrase.category == category) result.Add(phrase);
        }
        return result;
    }
}


