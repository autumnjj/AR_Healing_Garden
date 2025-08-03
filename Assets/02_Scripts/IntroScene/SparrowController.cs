using UnityEngine;
using System.Collections;
using Unity.Collections;

public class SparrowController : MonoBehaviour
{
    [Header("캐릭터")]
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

    // 상태
    private bool isActive = false;
    private Vector3 originalPosition;

    // 이벤트
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

        // 기존 참새 제거
        if (sparrowInstance != null)
        {
            Destroy(sparrowInstance);
        }

        // 카메라 기준 고정 위치에 소환
        Vector3 worldPosition = arCamera.transform.TransformPoint(fixedPosition);
        sparrowInstance = Instantiate(sparrowPrefab, worldPosition, Quaternion.identity);

        // 카메라 바라보기
        if (faceCamera)
        {
            Vector3 lookDirection = arCamera.transform.position - worldPosition;
            lookDirection.y *= 0.3f;
            if (lookDirection.magnitude > 0.1f)
            {
                // 180도 추가 회전으로 정면을 카메라쪽으로
                Quaternion lookRotation = Quaternion.LookRotation(lookDirection);
                Quaternion finalRotation = lookRotation * Quaternion.Euler(0, 180, 0) * Quaternion.Euler(additionalRotation);
                sparrowInstance.transform.rotation = finalRotation;
            }
        }
        else
        {
            // 고정 회전
            sparrowInstance.transform.rotation = Quaternion.Euler(additionalRotation);
        }

        // 컴포넌트 설정
        sparrowAnimator = sparrowInstance.GetComponent<Animator>();
        originalPosition = sparrowInstance.transform.position;

        isActive = true;

        // 이벤트 발생
        OnSparrowSpawned?.Invoke();

        // 잠깐 후 입장 완료 이벤트(바로 대화 시작용)
        StartCoroutine(QuickEntranceComplete());

        // idle 애니메이션 시작
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
                // 애니메이션이 없어도 에러 안남
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

    // 공개 메서드들
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