using UnityEngine;

public class CalendarIntegration : MonoBehaviour
{
    [Header("Calendar Reference")]
    public CalendarManager calendarManager;

    private ARPlantGrowthController growthController;
    private ARPlantVoiceController voiceController;

    private void Start()
    {
        SetupReferences();
        ConnectEvents();
    }

    private void SetupReferences()
    {
        if (calendarManager == null)
            calendarManager = FindAnyObjectByType<CalendarManager>();

        growthController = FindAnyObjectByType<ARPlantGrowthController>();

        voiceController = FindAnyObjectByType<ARPlantVoiceController>();
    }

    private void ConnectEvents()
    {
        if(voiceController != null)
            voiceController.OnRecognitionSuccess += OnVoiceSuccess;
        
    }

    private void OnVoiceSuccess(string keyword, float points, string method)
    {
        if (calendarManager != null)
            calendarManager.RecordTodaySpeech();
    }

    private void OnDestroy()
    {
        if(voiceController != null)
            voiceController.OnRecognitionSuccess -= OnVoiceSuccess;
    }
}
