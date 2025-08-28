using UnityEngine;

public class SafeAreaController : MonoBehaviour
{
    private RectTransform rectTransform;
    private Rect lastSafeArea = new Rect(0, 0, 0, 0);
    private Vector2Int lastScreenSize = new Vector2Int(0, 0);

    [Header("Safe Area ¼³Á¤")]
    public bool conformX = true;
    public bool conformY = true;

    private void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        Refresh();
    }

    private void Update()
    {
        Refresh();
    }

    private void Refresh()
    {
        Rect safeArea = Screen.safeArea;
        Vector2Int screenSize = new Vector2Int(Screen.width, Screen.height);

        if (safeArea != lastSafeArea || screenSize != lastScreenSize)
        {
            lastSafeArea = safeArea;
            lastScreenSize = screenSize;
            ApplySafeArea(safeArea);
        }
    }

    private void ApplySafeArea(Rect area)
    {
        if (rectTransform == null) return;

        Vector2 anchorMin = area.position;
        Vector2 anchorMax = area.position + area.size;

        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;

        if (conformX)
        {
            rectTransform.anchorMin = new Vector2(anchorMin.x, rectTransform.anchorMin.y);
            rectTransform.anchorMax = new Vector2(anchorMax.x, rectTransform.anchorMax.y);
        }
        if (conformY)
        {
            rectTransform.anchorMin = new Vector2(rectTransform.anchorMin.x, anchorMin.y);
            rectTransform.anchorMax = new Vector2(rectTransform.anchorMax.x, anchorMax.y);
        }

        Debug.Log($"Safe Area Applied: {area}");
    }
}
