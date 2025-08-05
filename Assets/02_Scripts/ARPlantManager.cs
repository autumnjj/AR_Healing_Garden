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

        
    }

    public void PlacePlantAtPosition(Pose pose)
    {
        if (plantPrefab != null && !isPlantPlaced)
        {
            currentPlantInstance = Instantiate(plantPrefab, pose.position, pose.rotation);

            // �Ĺ� ���̾� ����
            SetLayerRecursively(currentPlantInstance, 8);   // "Plant" ���̾�

            if (plantParent != null)
            {
                currentPlantInstance.transform.SetParent(plantParent);
            }

            ARPlantController plantController = currentPlantInstance.GetComponent<ARPlantController>();
            if(plantController == null)
            {
               plantController = currentPlantInstance.AddComponent<ARPlantController>();
            }

            if(plantGrowthData != null)
            {
               plantController.Initialize(plantGrowthData, plantGrowthUI);
            }

            isPlantPlaced = true;

            HideInstructionUI();
            if(placementIndicator != null) placementIndicator.SetActive(false);

            // ��� ���� ��Ȱ��ȭ(���� ���)
            if(planeManager != null)
            {
                planeManager.enabled = false;
                foreach(var plane in planeManager.trackables)
                {
                    plane.gameObject.SetActive(false);
                }
            }
            Debug.Log("Plant is placed AR Environment");
            ShowInstructionUI("�Ĺ��� ��ġ�ؼ� ��ȣ�ۿ��غ�����!");
        }
    }

    private void SetLayerRecursively(GameObject obj, int newLayer)
    {
        obj.layer = newLayer;
        foreach(Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, newLayer);
        }
    }

    public void ResetPlantPlacement()
    {
        if (currentPlantInstance != null)
        {
            Destroy(currentPlantInstance);
            currentPlantInstance = null;
        }

        isPlantPlaced = false;
        lastPlaneCount = 0;     // ��� ī��Ʈ ����

        if (planeManager != null)
        {
            planeManager.enabled = true;
            foreach(var plane in planeManager.trackables) 
            {
                plane.gameObject.SetActive(true);
            }
        }
        ShowInstructionUI("���ο� ��ġ�� �Ĺ��� ��ġ�غ�����!");
    }

    private void ShowInstructionUI(string message)
    {
        if(instructionUI != null)
        {
            instructionUI.SetActive(true);
            var textComponent = instructionUI.GetComponentInChildren<TextMeshProUGUI>();
            if(textComponent != null)
            {
                textComponent.text = message;
            }
        }
    }

    private void HideInstructionUI()
    {
        if(instructionUI != null)
        {
            instructionUI.SetActive(false);
        }
    }

    public GameObject GetCurrentPlant()
    {
        return currentPlantInstance;
    }

    public bool IsPlantPlaced()
    { 
        return isPlantPlaced; 
    }

    // ���� ������ ��� �� ��ȯ
    public int GetDectectedPlaneCount()
    {
        return planeManager != null ? planeManager.trackables.count : 0;
    }

    // Ư�� ��� Ÿ�Ը� Ȱ��ȭ
    public void SetPlaneDectectionMode(PlaneDetectionMode detectionMode)
    {
        if(planeManager != null)
        {
            planeManager.requestedDetectionMode = detectionMode;
        }
    }
}
