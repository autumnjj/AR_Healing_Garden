using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "MBTIQuestions", menuName = "BloomSpeak/MBTI Questions")]
public class MBTIQuestionsSO : ScriptableObject
{
    [System.Serializable]
    public class  Question
    {
        [TextArea(2, 4)]
        public string questionText;
        public string optionA;
        public string optionB;
        public string dimension; // EI, SN, TF, JP
        public int optionAValue; 
        public int optionBValue;
    }

    public List<Question> questions = new List<Question>();
}
