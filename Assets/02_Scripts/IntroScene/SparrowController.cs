using UnityEngine;
using System.Collections;
using Unity.Collections;

public class SparrowController : MonoBehaviour
{
    [Header("ĳ����")]
    public GameObject sparrowPrefab;
    private GameObject sparrowInstance;
    private Animator sparrowAnimator;

    [Header("Basic Settings")]
    public bool autoSpawnOnStart = true;
    public Vector3 fixedPosition = new Vector3(0, -0.5f, 2f);
    public bool faceCamera = false;
    public Vector3 additionalRotation = new Vector3(0, -5, 0);

    [Header("AR Camera")]
    public Camera arCamera;

    [Header("Animation")]
    public float idleFloatAmount = 0.1f;
    public float idleFloatSpeed = 2f;

    // ����
    private bool isActive = false;
    private Vector3 originalPosition;

    // �̺�Ʈ
    public System.Action OnSparrowSpawned;
    public System.Action OnEntranceComplete;

    private void Start()
    {
        if (arCamera == null)
        {
            arCamera = Camera.main ?? FindAnyObjectByType<Camera>();
        }

        if (autoSpawnOnStart)
        {
            SpawnSparrow();
        }
    }

    public void SpawnSparrow()
    {
        if (sparrowPrefab == null || arCamera == null) return;

        // ���� ���� ����
        if (sparrowInstance != null)
        {
            Destroy(sparrowInstance);
        }

        // ī�޶� ���� ���� ��ġ�� ��ȯ
        Vector3 worldPosition = arCamera.transform.TransformPoint(fixedPosition);
        sparrowInstance = Instantiate(sparrowPrefab, worldPosition, Quaternion.identity);

        // ī�޶� �ٶ󺸱�
        if (faceCamera)
        {
            Vector3 lookDirection = arCamera.transform.position - worldPosition;
            lookDirection.y *= 0.3f;
            if (lookDirection.magnitude > 0.1f)
            {
                // 180�� �߰� ȸ������ ������ ī�޶�������
                Quaternion lookRotation = Quaternion.LookRotation(lookDirection);
                Quaternion finalRotation = lookRotation * Quaternion.Euler(0, 180, 0) * Quaternion.Euler(additionalRotation);
                sparrowInstance.transform.rotation = finalRotation;
            }
        }
        else
        {
            // ���� ȸ��
            sparrowInstance.transform.rotation = Quaternion.Euler(additionalRotation);
        }

        // ������Ʈ ����
        sparrowAnimator = sparrowInstance.GetComponent<Animator>();
        originalPosition = sparrowInstance.transform.position;

        isActive = true;

        // �̺�Ʈ �߻�
        OnSparrowSpawned?.Invoke();

        // ��� �� ���� �Ϸ� �̺�Ʈ(�ٷ� ��ȭ ���ۿ�)
        StartCoroutine(QuickEntranceComplete());

        // idle �ִϸ��̼� ����
        StartCoroutine(SimpleIdleAnimation());

        Debug.Log($"Sparrow spawned at {worldPosition}");
    }

    private IEnumerator QuickEntranceComplete()
    {
        yield return new WaitForSeconds(0.5f);
        OnEntranceComplete?.Invoke();
    }

    private IEnumerator SimpleIdleAnimation()
    {
        while (sparrowInstance != null && isActive)
        {
            float offset = Mathf.Sin(Time.time * idleFloatSpeed) * idleFloatAmount;
            sparrowInstance.transform.position = originalPosition + Vector3.up * offset;

            yield return null;
        }
    }

    public void StartSpeaking()
    {
        PlayAnimation("Speaking");
    }

    public void StopSpeaking()
    {
        PlayAnimation("Idle");
    }

    public void PlayAnimation(string animationName)
    {
        if (sparrowAnimator != null && sparrowInstance != null)
        {
            try
            {
                sparrowAnimator.CrossFade(animationName, 0.2f);
            }
            catch
            {
                // �ִϸ��̼��� ��� ���� �ȳ�
            }
        }

    }

    public void HideSparrow()
    {
        if (sparrowInstance != null)
        {
            sparrowInstance.SetActive(false);
            Debug.Log("Sparrow hidden.");   
        }
    }

    public void ShowSparrow()
    {
        if (sparrowInstance != null)
        {
            sparrowInstance.SetActive(true);
            Debug.Log("Sparrow shown.");
        }
    }

    public void DestroySparrow()
    {
        if (sparrowInstance != null)
        {
            Destroy(sparrowInstance);
            sparrowInstance = null;
            isActive = false;
            Debug.Log("Sparrow destroyed.");
        }
    }

    // ���� �޼����
    public bool IsActive() => isActive;
    public GameObject GetSparrowInstance() => sparrowInstance;
    public Vector3 GetSparrowPosition() => sparrowInstance?.transform.position ?? Vector3.zero;

    private void OnDestroy()
    {
        if (sparrowInstance != null)
        {
            Destroy(sparrowInstance);
        }
    }
}