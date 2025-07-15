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
    [Header("긍정적 언어 목록")]
    public List<PositivePhrase> phrases = new List<PositivePhrase>();

    [Header("설정")]
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
            // 자존감 향상
            new PositivePhrase("나는 소중해", 15f, "자존감"),
            new PositivePhrase("나는 가치있어", 15f, "자존감"),
            new PositivePhrase("나는 충분해", 15f, "자존감"),

            // 격려
            new PositivePhrase("잘하고 있어", 12f, "격려"),
            new PositivePhrase("힘내", 10f, "격려"),
            new PositivePhrase("파이팅", 10f, "격려"),

            // 사랑과 감사
            new PositivePhrase("사랑해", 14f, "사랑"),
            new PositivePhrase("고마워", 11f, "감사"),
            new PositivePhrase("감사해", 11f, "감사"),

            // 성장
            new PositivePhrase("무럭무럭 자라", 13f, "성장"),
            new PositivePhrase("예쁘게 자라", 13f, "성장"),
            new PositivePhrase("건강하게 자라", 13f, "성장"),

            // 기본 긍정 표현
            new PositivePhrase("예뻐", 10f, "칭찬"),
            new PositivePhrase("귀여워", 10f, "칭찬"),
            new PositivePhrase("좋아", 8f, "칭찬")
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

        // 완전 일치
        if (input == target) return true;

        // 포함 관계
        if (input.Contains(target) || target.Contains(input)) return true;

        // 키워드 매칭(간단한 방식)
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


