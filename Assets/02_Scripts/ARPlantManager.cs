using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using TMPro;

public class ARPlantManager : MonoBehaviour
{
    [Header("AR Foundation Components")]
    public ARRaycastManager raycastManager;
    public ARPlaneManager planeManager;
    public Camera arCamera;
    
    [Header("Plant Prefabs")]
    public GameObject tablePrefab;
    public GameObject seedPrefab;

    [Header("Plant Prefabs by Type")]
    public PlantPrefabSet sunflowerPrefabs;
    public PlantPrefabSet rosePrefabs;
    public PlantPrefabSet cactusPrefabs;
    public PlantPrefabSet lavenderPrefabs;

    [Header("UI")]
    public GameObject placementIndicator;
    public TextMeshProUGUI instructionText;
    public TextMeshProUGUI statusText;

    [Header("Input Actions")]
    public InputActionAsset inputActions;

    // Input Actions
    private InputAction touchAction;
    private InputAction touchPositionAction;
    private InputAction pinchAction;

    // AR ���� ����
    private List<ARRaycastHit> raycastHits = new List<ARRaycastHit>();
    private GameObject tableInstance;
    private GameObject currentPlantInstance;
    private bool isTablePlaced = false;
    private bool isPlantPlaced = false;

    // �Ĺ� ����
    private PlantGrowthStage currentGrowthStage = PlantGrowthStage.Seed;
    private string selectedPlantType = "sunflower";
    private float growthPoints = 0f;
    private float maxGrowthPoints = 100f;

    // ����
    private bool isPlacementMode = true;
    private Vector3 lastTouchPosition;
    private float lastTouchTime;

    [System.Serializable]
    public class PlantPrefabSet
    {
        public GameObject sprout;
        public GameObject growing;
        public GameObject blooming;
    }

    private void Start()
    {
        SetupInputActions();
        InitializeAR();
        LoadSelectedPlant();
        UpdateInstructionText("ȭ���� ��ġ�ؼ� ȭ���� ���ƺ�����!");
    }

    private void SetupInputActions()
    {
        if (inputActions == null) return;

        var actionMap = inputActions.FindActionMap("AR Plant");
        touchAction = actionMap.FindAction("Touch");
        touchPositionAction = actionMap.FindAction("TouchPosition");
        pinchAction = actionMap.FindAction("Pinch");
    }

    private void OnEnable()
    {
        if (touchAction != null)
        {
            touchAction.started += OnTouchStarted;
            touchAction.canceled += OnTouchEnded;
        }

        touchAction?.Enable();
        touchPositionAction?.Enable();
        pinchAction?.Enable();
    }

    private void OnDisable()
    {
        if (touchAction != null)
        {
            touchAction.started -= OnTouchStarted;
            touchAction.canceled -= OnTouchEnded;
        }

        touchAction?.Disable();
        touchPositionAction?.Disable();
        pinchAction?.Disable();
    }

    private void InitializeAR()
    { 
        if(placementIndicator != null)
            placementIndicator.SetActive(false);
    }

    private void LoadSelectedPlant()
    {
        // MBTI ������� �Ĺ� Ÿ�� ��������
        selectedPlantType = PlayerPrefs.GetString("Matched_Plant", "sunflower").ToLower();
        Debug.Log($"Selected plant type : {selectedPlantType}");
    }

    private void Update()
    {
        if (isPlacementMode)
            UpdatePlacementIndicator();

        HandlePinchForScale();
    }

    private void UpdatePlacementIndicator()
    {
        var screenCenter = arCamera.ViewportToScreenPoint(new Vector3(0.5f, 0.5f));
        raycastHits.Clear();

        if (raycastManager.Raycast(screenCenter, raycastHits, TrackableType.PlaneWithinPolygon))
        {
            var hit = raycastHits[0];

            if (placementIndicator != null)
            {
                placementIndicator.SetActive(true);
                placementIndicator.transform.SetPositionAndRotation(hit.pose.position, hit.pose.rotation);
            }
        }
        else
        {
            if (placementIndicator != null)
                placementIndicator.SetActive(false);
        }
    }

    private void OnTouchStarted(InputAction.CallbackContext context)
    {
        lastTouchPosition = touchPositionAction.ReadValue<Vector2>();
        lastTouchTime = Time.time;
    }

    private void OnTouchEnded(InputAction.CallbackContext context)
    {
        Vector2 currentTouchPosition = touchPositionAction.ReadValue<Vector2>();
        float touchDuration = Time.time - lastTouchTime;
        float touchDistance = Vector2.Distance(lastTouchPosition, currentTouchPosition);

        // �� ����
        if (touchDuration < 0.3f && touchDistance < 50f)
            ProcessTap(currentTouchPosition);
    }

    private void ProcessTap(Vector2 screenPosition)
    {
        if (isPlacementMode)
        {
            PlaceTableAndPlant(screenPosition);
        }
        else
        {
            // �Ĺ��� ��ȣ�ۿ�
            InteractWithPlant(screenPosition);
        }
    }

    private void PlaceTableAndPlant(Vector2 screenPosition)
    {
        raycastHits.Clear();

        if (raycastManager.Raycast(screenPosition, raycastHits, TrackableType.PlaneWithinPolygon))
        {
            var hit = raycastHits[0];

            // Ź�� ��ġ
            if (tablePrefab != null && !isTablePlaced)
            {
                tableInstance = Instantiate(tablePrefab, hit.pose.position, hit.pose.rotation);
                isTablePlaced = true;

                // Ź�� ���� ���� ��ġ
                Vector3 plantPosition = hit.pose.position + Vector3.up * 0.1f;
                currentPlantInstance = Instantiate(seedPrefab, plantPosition, hit.pose.rotation);
                isPlantPlaced = true;

                // ��ġ ��� ����
                isPlacementMode = false;
                if (placementIndicator != null)
                    placementIndicator.SetActive(false);

                // ��� ���� ��Ȱ��ȭ(���� ���)
                if (planeManager != null)
                {
                    planeManager.enabled = false;
                    foreach(var plane in planeManager.trackables)
                    {
                        plane.gameObject.SetActive(false);
                    }
                }

                UpdateInstructionText("�Ĺ��� ��ġ�ϰų� �������� ���� �غ�����!");
                UpdateStatusText($"���� �ܰ�: ���� ({growthPoints:F0}/{maxGrowthPoints}");
            }
        }
    }

    private void InteractWithPlant(Vector2 screenPosition)
    {
        if (!isPlantPlaced) return;

        Ray ray = arCamera.ScreenPointToRay(screenPosition);
        RaycastHit hit;

        // �Ĺ� ��ġ ����
        if (Physics.Raycast(ray, out hit))
        {
            if (hit.collider.gameObject == currentPlantInstance ||
                hit.collider.transform.IsChildOf(currentPlantInstance.transform))
            {
                // ��ġ ��ȣ�ۿ� ����
                AddGrowthPoints(10f, "��ġ");
                PlayTouchEffect(hit.point);
            }
        }
    }

    private void HandlePinchForScale()
    {
        if(Input.touchCount == 2)
        {
            Touch touch1 = Input.GetTouch(0);
            Touch touch2 = Input.GetTouch(1);

            float currentDistance = Vector2.Distance(touch1.position, touch2.position);

            if (touch1.phase == TouchPhase.Moved || touch2.phase == TouchPhase.Moved)
            {
                float prevDistance = Vector2.Distance(
                    touch1.position - touch1.deltaPosition,
                    touch2.position - touch2.deltaPosition);

                float deltaDistance = currentDistance - prevDistance;

                if (currentPlantInstance != null && Mathf.Abs(deltaDistance) > 10f)
                {
                    float scaleChange = deltaDistance * 0.001f;
                    Vector3 newScale = currentPlantInstance.transform.localScale + Vector3.one * scaleChange;

                    // ������ ����
                    newScale = Vector3.Clamp(newScale, Vector3.one * 0.5f, Vector3.one * 2f);
                    currentPlantInstance.transform.localScale = newScale;
                }
            }

        }
        
    }

    public void AddGrowthPoints(float points, string source)
    {
        growthPoints += points;
        growthPoints = Mathf.Clamp(growthPoints, 0f, maxGrowthPoints);

        UpdateStatusText($"���� �ܰ�: {GetStageKoreanName(currentGrowthStage)} (+{points} from {source}");

        // ���� �ܰ� üũ
        CheckGrowthStage();
    }

    private void CheckGrowthStage()
    {
        PlantGrowthStage newStage = CalculateGrowthStage();

        if (newStage != currentGrowthStage)
        {
            currentGrowthStage = newStage;
            UpdatePlantVisual();
            UpdateInstructionText($"�����մϴ�! {GetStageKoreanName(currentGrowthStage)} �ܰ�� �����߾��!");
        }
    }

    private PlantGrowthStage CalculateGrowthStage()
    {
        if (growthPoints >= 75f) return PlantGrowthStage.Blooming;
        if (growthPoints >= 50f) return PlantGrowthStage.Growing;
        if (growthPoints >= 25f) return PlantGrowthStage.Sprout;
        return PlantGrowthStage.Seed;
    }

    private void UpdatePlantVisual()
    {
        if (currentPlantInstance == null) return;

        Vector3 position = currentPlantInstance.transform.position;
        Quaternion rotation = currentPlantInstance.transform.rotation;
        Vector3 scale = currentPlantInstance.transform.localScale;

        // ���� �Ĺ� ����
        Destroy(currentPlantInstance );

        // ���ο� �ܰ��� �Ĺ� ����
        GameObject newPlantPrefab = GetPlantPrefabForStage(currentGrowthStage);
        if (newPlantPrefab != null)
        {
            currentPlantInstance = Instantiate(newPlantPrefab, position, rotation);
            currentPlantInstance.transform.localScale = scale;
        }
    }

    private GameObject GetPlantPrefabForStage(PlantGrowthStage stage)
    {
        PlantPrefabSet prefabSet = GetPrefabSetForPlantType(selectedPlantType);
        if (prefabSet == null) return seedPrefab;

        switch (stage)
        {
            case PlantGrowthStage.Sprout: return prefabSet.sprout;
            case PlantGrowthStage.Growing: return prefabSet.growing;
            case PlantGrowthStage.Blooming: return prefabSet.blooming;
            default: return seedPrefab;
        }
    }

    private PlantPrefabSet GetPrefabSetForPlantType(string plantType)
    {
        switch (plantType.ToLower())
        {
            case "sunflower": return sunflowerPrefabs;
            case "rose": return rosePrefabs;
            case "cactus": return cactusPrefabs;
            case "lavender": return lavenderPrefabs;
            default: return sunflowerPrefabs;
        }
    }

    private void PlayTouchEffect(Vector3 position)
    {
        // ������ ��ƼŬ ȿ���� �ִϸ��̼�
        Debug.Log($"Touch effect at {position}");
    }

    private string GetStageKoreanName(PlantGrowthStage stage)
    {
        switch (stage)
        {
            case PlantGrowthStage.Seed: return "����";
            case PlantGrowthStage.Sprout: return "����";
            case PlantGrowthStage.Growing: return "����";
            case PlantGrowthStage.Blooming: return "��ȭ";
            default: return "�� �� ����";
        }
    }

    // UI ������Ʈ �޼����
    private void UpdateInstructionText(string message)
    {
        if (instructionText != null)
            instructionText.text = message;
    }

    private void UpdateStatusText(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }

    // ���� �νĿ��� ȣ���� �޼���
    public void OnPositiveSpeechDetected(float points)
    {
        AddGrowthPoints(points, "����");
    }

    // ���� ���
    public void ResetPlant()
    {
        if (tableInstance != null) Destroy(tableInstance);
        if (currentPlantInstance != null) Destroy(currentPlantInstance);

        isTablePlaced = false;
        isPlantPlaced = false;
        isPlacementMode = true;
        currentGrowthStage = PlantGrowthStage.Seed;
        growthPoints = 0f;

        if (planeManager != null)
        {
            planeManager.enabled = true;
            foreach(var plane in planeManager.trackables)
            {
                plane.gameObject.SetActive(true);
            }
        }

        UpdateInstructionText("Ź�ڸ� ��ġ�ؼ� ȭ���� ���ƺ�����!");
    }
}
