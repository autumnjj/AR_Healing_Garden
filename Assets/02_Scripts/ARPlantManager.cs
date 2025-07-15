using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;
using TMPro;

public class ARPlantManager : MonoBehaviour
{
    [Header("AR Foundation Components")]
    public ARRaycastManager raycastManager;
    public ARPlaneManager planeManager;
    public Camera arCamera;

    [Header("Plant Prefabs")]
    public GameObject plantPrefab;
    public Transform plantParent;

    [Header("Plant Growth System")]
    public PlantGrowthData plantGrowthData;
    public PlantGrowthUI plantGrowthUI;

    [Header("UI Elements")]
    public GameObject placementIndicator;
    public GameObject instructionUI;

    [Header("Input System")]
    public ARInputManager inputManager;

    // AR ���� ����
    private List<ARRaycastHit> raycastHits = new List<ARRaycastHit>();
    private GameObject currentPlantInstance;
    private bool isPlantPlaced = false;
    private Pose placementPose;
    private bool placementPoseIsValid = false;

    // ��� ���� üũ��
    private int lastPlaneCount = 0;

    private void Start()
    {
        InitializeAR();
        SetupInputManager();
    }

    private void InitializeAR()
    {
        if (planeManager != null)
        {
            planeManager.enabled = true;
        }

        if(placementIndicator != null)
        {
            placementIndicator.SetActive(false);
        }

        ShowInstructionUI("�ٴ��� ��ĵ�Ͽ� �Ĺ��� ��ġ�� ������ ã�ƺ�����");
    }

    private void SetupInputManager()
    {
        if (inputManager != null) 
        {
            inputManager.plantManager = this;

            // �Ĺ� ���̾� ����
            inputManager.SetPlantLayerMask(1 << 8);
        }
    }

    private void Update()
    {
        if (!isPlantPlaced)
        {
            UpdatePlacementPose();
            UpdatePlacementIndicator();
            CheckForNewPlanes();
        }
    }

    private void CheckForNewPlanes()
    {
        if(planeManager != null)
        {
            int currentPlaneCount = planeManager.trackables.count;

            // ���ο� ����� �����Ǿ��� ��
            if(currentPlaneCount > lastPlaneCount && !isPlantPlaced)
            {
                ShowInstructionUI("����� �����߽��ϴ�. ȭ���� ��ġ�Ͽ� �Ĺ��� ��ġ�ϼ���!");
                Debug.Log($"New Plane indicator. Total Plane Count : {currentPlaneCount}");
            }

            lastPlaneCount = currentPlaneCount;
        }
    }

    private void UpdatePlacementPose()
    {
        var screenCenter = arCamera.ViewportToScreenPoint(new Vector3(0.5f, 0.5f));
        raycastHits.Clear();

        if (raycastManager.Raycast(screenCenter, raycastHits, TrackableType.PlaneWithinPolygon))
        {
            placementPoseIsValid = true;
            placementPose = raycastHits[0].pose;

            var hitPlane = raycastHits[0].trackable as ARPlane;
            if (hitPlane != null && hitPlane.alignment == PlaneAlignment.HorizontalUp)
            {
                placementPose = raycastHits[0].pose;
            }
        }
        else
        {
            placementPoseIsValid = false;
        }
    }

    private void UpdatePlacementIndicator()
    {
        if (placementIndicator != null)
        {
            if (placementPoseIsValid && !isPlantPlaced) 
            {
                placementIndicator.SetActive(true);
                placementIndicator.transform.SetPositionAndRotation(placementPose.position, placementPose.rotation);
            }
            else
            {
                placementIndicator.SetActive(false);
            }
        }
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
