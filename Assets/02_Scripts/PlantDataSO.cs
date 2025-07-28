using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "PlantData", menuName = "BloomSpeak/PlantData")]
public class PlantDataSO : ScriptableObject
{
    [Header("�⺻ ����")]
    public string plantId;
    public string plantName;
    public string koreanName;
    public Sprite plantImage;
    public GameObject plantPrefab;

    [Header("MBIT ��Ī")]
    public List<string> primaryMBTI = new List<string>();

    [Header("�Ĺ� Ư��")]
    public string symolism;
    public List<string> careMessages = new List<string>();

    [Header("���� �ܰ�")]
    public List<Sprite> growthStages = new List<Sprite>();  // ����, ����, ����, ��ȭ
}
