using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class ARManager : MonoBehaviour
{
    [Header("기본 UI")]
    public Button topHomeButton;

    [Header("완료 UI")]
    public GameObject completionPanel;
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
        CheckCompletion();
    }

    private void SetupUI()
    {
        if (topHomeButton != null)
            topHomeButton.onClick.AddListener(GoHome);
        

        if (homeButton != null)
            homeButton.onClick.AddListener(GoHome);
        

        if (quitButton != null)
            quitButton.onClick.AddListener(QuitApp);

        if (calendarButton != null)
            calendarButton.onClick.AddListener(OpenCalendar);

        if (completionPanel != null)
            completionPanel.SetActive(false);
    }

    private void CheckCompletion()
    {
        StartCoroutine(CheckCompletionLoop());
    }

    private IEnumerator CheckCompletionLoop()
    {
        while (!isCompleted)
        {
            yield return new WaitForSeconds(1f);

            if (plantVoiceController != null && plantVoiceController.IsAllComplete())
            {
                yield return new WaitForSeconds(3f);
                ShowCompletion();
                break;
            }
        }
    }

    private void ShowCompletion()
    {
        if (isCompleted) return;

        isCompleted = true;

        if (topHomeButton != null)
            topHomeButton.gameObject.SetActive(false);

        if (completionPanel != null)
            completionPanel.SetActive(true);
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
