using UnityEngine;
using System.Collections;

public class VirtualEnvironment : MonoBehaviour
{
    [Header("���� ȯ�� ����")]
    public GameObject tablePrefab;
    public Transform tableParent;

    [Header("ī�޶� ���� ��ġ")]
    public Vector3 tableOffset = new Vector3(0, -0.5f, 1.5f);
    public Vector3 tableRotation = new Vector3(0, 0, 0);
    public float tableScale = 1.0f;

    [Header("�Ĺ� ��ġ")]
    public Transform plantSpawnPoint;
    public Vector3 plantOffset = new Vector3(0, 0.1f, 0);



}
