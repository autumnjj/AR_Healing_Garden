using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChoiceUIController : MonoBehaviour
{
    [Header("Choice UI Elements")]
    public TextMeshProUGUI choiceTitle;
    public TextMeshProUGUI choiceDescription;
    public Button quickTestButton;
    public Button quickStartButton;
    public Button directInputButton;
    public Button choiceCloseButton;

    // ÀÌº¥Æ®
    public System.Action OnDirectInputSelected;
    public System.Action OnQuickTestSelected;
    public System.Action OnQuickStartSelected;

    private void Start()
    {
        ConnectEvents();
    }

    private void ConnectEvents()
    {
        if (directInputButton != null) 
            directInputButton.onClick.AddListener(() => OnDirectInputSelected?.Invoke());
        if (quickStartButton != null)
            quickStartButton.onClick.AddListener(()=> OnQuickStartSelected?.Invoke());
        if (quickTestButton != null)
            quickTestButton.onClick.AddListener(()=> OnQuickTestSelected?.Invoke());
    }

    public void ClosePanel()
    {

    }
}
