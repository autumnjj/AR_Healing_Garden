using UnityEngine;
using System.Collections;

public class VirtualEnvironment : MonoBehaviour
{
    [Header("가상 환경 설정")]
    public GameObject tablePrefab;
    public Transform tableParent;

    [Header("카메라 기준 위치")]
    public Vector3 tableOffset = new Vector3(0, -0.5f, 1.5f);
    public Vector3 tableRotation = new Vector3(0, 0, 0);
    public float tableScale = 1.0f;

    [Header("식물 배치")]
    public Transform plantSpawnPoint;
    public Vector3 plantOffset = new Vector3(0, 0.1f, 0);



}
