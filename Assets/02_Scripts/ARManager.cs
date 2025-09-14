using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class ARManager : MonoBehaviour
{
    [Header("UI")]
    public Button homeButton;
    public Button quitButton;
    public Button calendarButton;

    [Header("연결된 컴포넌트")]
    public ARPlantVoiceController plantVoiceController;
    public CalendarManager calendarManager;
    public ARPlacementManager placementManager;
    public ARPlantGrowthController growthController;

    private bool isCompleted = false;
    private void Start()
    {
        SetupUI();
    }

    private void SetupUI()
    {
        if (homeButton != null)
            homeButton.onClick.AddListener(GoHome);
        
        if (quitButton != null)
            quitButton.onClick.AddListener(QuitApp);

        if (calendarButton != null)
            calendarButton.onClick.AddListener(OpenCalendar);
    }


    private void GoHome()
    {
        SceneManager.LoadScene(0);
    }

    private void OpenCalendar()
    {
        if (calendarManager != null)
            calendarManager.ShowCalendar();
    }

    private void QuitApp()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
