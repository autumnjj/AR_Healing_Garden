using UnityEngine;
using System.Collections;

public class SparrowController : MonoBehaviour
{
    [Header("캐릭터")]
    public GameObject sparrowPrefab;
    private GameObject sparrowInstance;
    private Animator sparrowAnimator;

    [Header("비행 설정")]
    public float spawnDistance = 4f;
    public float flyHeight = 1.5f;
    public float hoverRadius = 0.8f;
    public float flySpeed = 2f;
    public float arrivalTime = 2.5f;

    [Header("AR Camera")]
    public Camera arCamera;

    [Header("Animation 설정")]
    public float spinDuration = 1.5f;
    public float idleTransitionTime = 0.5f;

    // 상태
    private bool isActive = false;
    private bool isFlying = false;
    private bool isHovering = false;
    private bool isTouchReacting = false;

    private Vector3 hoverCenter;
    private float hoverAngle = 0f;

    // 이벤트
    public System.Action OnSparrowSpawned;
    public System.Action OnSparrowTouched;
    public System.Action OnEntranceComplete;

    public void SpawnSparrow()
    {
        if (sparrowPrefab == null || arCamera == null) return;

        // 스폰 위치 계산
        Vector3 spawnPos = CalculateSpawnPosition();
        sparrowInstance = Instantiate(sparrowPrefab, spawnPos, Quaternion.identity);

        // 애니메이터 설정
        sparrowAnimator = sparrowInstance.GetComponent<Animator>();

        // 호버링 중심정 설정 (카메라 앞쪽)
        hoverCenter = CalculateHoverCenter();

        isActive = true;

        // 스폰 이벤트 발생
        OnSparrowSpawned?.Invoke();

        // 입장 애니메이션 시작
        StartCoroutine(EntranceAnimation());
    }

    private Vector3 CalculateSpawnPosition()
    {
        Vector3 cameraPos = arCamera.transform.position;
        Vector3 cameraForward = arCamera.transform.forward;
        Vector3 cameraRight = arCamera.transform.right;

        return cameraPos +
            cameraForward * spawnDistance +
            cameraRight * 3f + Vector3.up * (flyHeight + 1f);
    }

    private Vector3 CalculateHoverCenter()
    {
        Vector3 cameraPos = arCamera.transform.position;
        Vector3 cameraForward = arCamera.transform.forward;

        return cameraPos +
            cameraForward * (spawnDistance * 0.7f) +
            Vector3.up * flyHeight;
    }

    private IEnumerator EntranceAnimation()
    {
        if (sparrowInstance == null) yield break;

        isFlying = true;

        // fly 애니메이션 시작
        PlayAnimation("Fly");

        Vector3 startPos = sparrowInstance.transform.position;
        Vector3 endPos = hoverCenter;

        float elapsed = 0f;

        while (elapsed < arrivalTime)
        {
            float t = elapsed / arrivalTime;

            // 곡선 경로
            Vector3 currentPos = CalculateFlightPath(startPos, endPos, t);
            sparrowInstance.transform.position = currentPos;

            // 비행 방향 바라보기
            if (t < 0.9f)
            {
                Vector3 nextPos = CalculateFlightPath(startPos, endPos, t + 0.1f);
                Vector3 direction = (nextPos - currentPos).normalized;
                if (direction.magnitude > 0.1f)
                {
                    sparrowInstance.transform.rotation = Quaternion.LookRotation(direction);
                }
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        // 최종 위치
        sparrowInstance.transform.position = endPos;

        // 카메라 방향 바라보기
        LookAtCamera();

        // 입장 완료
        isFlying = false;
        isHovering = true;

        // 기본 애니메이션으로 전환
        yield return new WaitForSeconds(idleTransitionTime);
        PlayAnimation("Idle");

        // 입장 완료 이벤트
        OnEntranceComplete?.Invoke();

        // 호버링 시작
        StartCoroutine(HoveringLoop());
    }

    private Vector3 CalculateFlightPath(Vector3 start, Vector3 end, float t)
    {
        // 부드러운 곡선 경로
        Vector3 midPoint = Vector3.Lerp(start, end, 0.5f) + Vector3.up * 0.5f;

        Vector3 point1 = Vector3.Lerp(start, midPoint, t);
        Vector3 point2 = Vector3.Lerp(midPoint, end, t);

        return Vector3.Lerp(point1, point2, t);
    }

    private IEnumerator HoveringLoop()
    {
        while(isHovering && !isTouchReacting)
        {
            // 원형 궤도로 자연스럽게 맴돌기
            hoverAngle += flySpeed * 0.5f * Time.deltaTime;

            Vector3 offset = new Vector3(Mathf.Cos(hoverAngle) * hoverRadius,
                Mathf.Sin(hoverAngle * 1.5f) * 0.15f,
                Mathf.Sin(hoverAngle) * hoverRadius * 0.5f);

            Vector3 targetPos = hoverCenter + offset;

            // 부드럽게 이동
            sparrowInstance.transform.position = Vector3.Lerp(
                sparrowInstance.transform.position, targetPos,
                Time.deltaTime * 2f);

            // 주기적으로 카메라 바라보기
            if (Time.time % 3f < Time.deltaTime)
                LookAtCamera();

            yield return null;
        }
    }

    private void LookAtCamera()
    {
        if (arCamera == null || sparrowInstance == null) return;

        Vector3 lookDirection = arCamera.transform.position - sparrowInstance.transform.position;
        lookDirection.y *= 0.5f;

        if (lookDirection.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
            sparrowInstance.transform.rotation = Quaternion.Slerp(
                sparrowInstance.transform.rotation,
                targetRotation, Time.deltaTime * 3f);
        }
    }


    public void OnTouched(Vector2 touchScreenPosition)
    {
        if (!IsValidTouch(touchScreenPosition)) return;

        // 터치 반응 시작
        StartCoroutine(TouchReactionCoroutine());

        // 터치 이벤트 발생
        OnSparrowTouched?.Invoke();

    }

    private IEnumerator TouchReactionCoroutine()
    {
        isTouchReacting = true;

        // 터치 반응 애니메이션_Spin
        PlayAnimation("Spin");

        yield return new WaitForSeconds(spinDuration);

        // 기본 애니메이션으로 복귀
        PlayAnimation("Idle");

        isTouchReacting = false;
    }

    private bool IsValidTouch(Vector2 screenPosition)
    {
        if (sparrowInstance == null || arCamera == null) return false;

        Vector3 sparrowScreenPos = arCamera.WorldToScreenPoint(sparrowInstance.transform.position);

        if (sparrowScreenPos.z < 0) return false;

        Vector2 sparrowScreen2D = new Vector2(sparrowScreenPos.x, sparrowScreenPos.y);
        float distance = Vector2.Distance(screenPosition, sparrowScreen2D);

        // 터치 허용 반경
        return distance <= 100f;
    }

    public void PlayAnimation(string animationName)
    {
        if (sparrowAnimator != null)
        {
            // 애니메이션이 있는지 확인 후 재생
            try
            {
                sparrowAnimator.CrossFade(animationName, 0.2f);
                Debug.Log($"Playing Animation: {animationName}");
            }
            catch
            {
                // 애니메이션이 없으면 무시
                Debug.Log($"Animation '{animationName}' not found, using default state");
            }
        }
    }

    // 대화 시작할 때 호출
    public void StartSpeaking()
    {
        if (!isTouchReacting && isHovering)
        {
            flySpeed *= 0.3f;
        }
    }

    public void StopSpeaking()
    {
        if (!isTouchReacting && isHovering)
        {
            flySpeed = 3f;
        }
    }

    // 공개 메서드들
    public bool IsActive() => isActive;
    public GameObject GetSparrowInstance() => sparrowInstance;
    public Vector3 GetSparrowPosition() => sparrowInstance?.transform.position ?? Vector3.zero;

    private void OnDestroy()
    {
        if(sparrowInstance != null)
            Destroy(sparrowInstance);
    }
}
