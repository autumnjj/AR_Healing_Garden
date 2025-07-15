using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransition : MonoBehaviour
{
    public void GoBackToMBTIScene()
    {
        SceneManager.LoadScene(0);
    }

    public void GoToPlantGrowthScene()
    {
        SceneManager.LoadScene(1);
    }

    public void RestartMBTITest()
    {
        // MBTI ������ �ʱ�ȭ
        PlayerPrefs.DeleteKey("MBTI_Type");
        PlayerPrefs.DeleteKey("Matched_Plant");
        PlayerPrefs.Save();

        SceneManager.LoadScene(0);
    }
}
