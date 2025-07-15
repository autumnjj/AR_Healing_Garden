using UnityEngine;

public class UserPersonalityData 
{
    public string mbtiType;
    public float EI_score;
    public float SN_score;
    public float TF_score;
    public float JP_score;
    public string matchedPlantId;

    // PlayerPrefs에 저장
    public void SaveToPlayerPrefs()
    {
        PlayerPrefs.SetString("MBTIType", mbtiType);
        PlayerPrefs.SetFloat("MBTI_EI", EI_score);
        PlayerPrefs.SetFloat("MBTI_SN", SN_score);
        PlayerPrefs.SetFloat("MBTI_TF", TF_score);
        PlayerPrefs.SetFloat("MBTI_JP", JP_score);
        PlayerPrefs.SetString("Matched_Plant", matchedPlantId);
        PlayerPrefs.Save();
    }

    // PlayerPrefs에서 로드
    public void LoadFromPlayerPrefs()
    {
        mbtiType = PlayerPrefs.GetString("MBTI_Type", "");
        EI_score = PlayerPrefs.GetFloat("MBTI_EI", 0f);
        SN_score = PlayerPrefs.GetFloat("MBTI_SN", 0f);
        TF_score = PlayerPrefs.GetFloat("MBTI_TF", 0f);
        JP_score = PlayerPrefs.GetFloat("MBTI_JP", 0f);
        matchedPlantId = PlayerPrefs.GetString("Matched_Plant", "");
    }

    // 데이터가 있는지 확인
    public bool HasData()
    {
        return !string.IsNullOrEmpty(mbtiType);
    }

    // 데이터 삭제
    public void ClearData()
    {
        PlayerPrefs.DeleteKey("MBTI_Type");
        PlayerPrefs.DeleteKey("MBTI_EI");
        PlayerPrefs.DeleteKey("MBTI_SN");
        PlayerPrefs.DeleteKey("MBTI_TF");
        PlayerPrefs.DeleteKey("MBTI_JP");
        PlayerPrefs.DeleteKey("Matched_Plant");
        PlayerPrefs.Save();
    }
}
