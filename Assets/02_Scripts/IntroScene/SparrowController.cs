using UnityEngine;
using System.Collections;

public class SparrowController : MonoBehaviour
{
    [Header("ĳ����")]
    public GameObject sparrowPrefab;
    private GameObject sparrowInstance;
    private Animator sparrowAnimator;

    [Header("���� ����")]
    public float spawnDistance = 4f;
    public float flyHeight = 1.5f;
    public float hoverRadius = 0.8f;
    public float flySpeed = 2f;
    public float arrivalTime = 2.5f;

    [Header("AR Camera")]
    public Camera arCamera;

    [Header("Animation ����")]
    public float spinDuration = 1.5f;
    public float idleTransitionTime = 0.5f;

    // ����
    private bool isActive = false;
    private bool isFlying = false;
    private bool isHovering = false;
    private bool isTouchReacting = false;

    private Vector3 hoverCenter;
    private float hoverAngle = 0f;

    // �̺�Ʈ
    public System.Action OnSparrowSpawned;
    public System.Action OnSparrowTouched;
    public System.Action OnEntranceComplete;

    public void SpawnSparrow()
    {
        if (sparrowPrefab == null || arCamera == null) return;

        // ���� ��ġ ���
        Vector3 spawnPos = CalculateSpawnPosition();
        sparrowInstance = Instantiate(sparrowPrefab, spawnPos, Quaternion.identity);

        // �ִϸ����� ����
        sparrowAnimator = sparrowInstance.GetComponent<Animator>();

        // ȣ���� �߽��� ���� (ī�޶� ����)
        hoverCenter = CalculateHoverCenter();

        isActive = true;

        // ���� �̺�Ʈ �߻�
        OnSparrowSpawned?.Invoke();

        // ���� �ִϸ��̼� ����
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

        // fly �ִϸ��̼� ����
        PlayAnimation("Fly");

        Vector3 startPos = sparrowInstance.transform.position;
        Vector3 endPos = hoverCenter;

        float elapsed = 0f;

        while (elapsed < arrivalTime)
        {
            float t = elapsed / arrivalTime;

            // � ���
            Vector3 currentPos = CalculateFlightPath(startPos, endPos, t);
            sparrowInstance.transform.position = currentPos;

            // ���� ���� �ٶ󺸱�
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

        // ���� ��ġ
        sparrowInstance.transform.position = endPos;

        // ī�޶� ���� �ٶ󺸱�
        LookAtCamera();

        // ���� �Ϸ�
        isFlying = false;
        isHovering = true;

        // �⺻ �ִϸ��̼����� ��ȯ
        yield return new WaitForSeconds(idleTransitionTime);
        PlayAnimation("Idle");

        // ���� �Ϸ� �̺�Ʈ
        OnEntranceComplete?.Invoke();

        // ȣ���� ����
        StartCoroutine(HoveringLoop());
    }

    private Vector3 CalculateFlightPath(Vector3 start, Vector3 end, float t)
    {
        // �ε巯�� � ���
        Vector3 midPoint = Vector3.Lerp(start, end, 0.5f) + Vector3.up * 0.5f;

        Vector3 point1 = Vector3.Lerp(start, midPoint, t);
        Vector3 point2 = Vector3.Lerp(midPoint, end, t);

        return Vector3.Lerp(point1, point2, t);
    }

    private IEnumerator HoveringLoop()
    {
        while(isHovering && !isTouchReacting)
        {
            // ���� �˵��� �ڿ������� �ɵ���
            hoverAngle += flySpeed * 0.5f * Time.deltaTime;

            Vector3 offset = new Vector3(Mathf.Cos(hoverAngle) * hoverRadius,
                Mathf.Sin(hoverAngle * 1.5f) * 0.15f,
                Mathf.Sin(hoverAngle) * hoverRadius * 0.5f);

            Vector3 targetPos = hoverCenter + offset;

            // �ε巴�� �̵�
            sparrowInstance.transform.position = Vector3.Lerp(
                sparrowInstance.transform.position, targetPos,
                Time.deltaTime * 2f);

            // �ֱ������� ī�޶� �ٶ󺸱�
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

        // ��ġ ���� ����
        StartCoroutine(TouchReactionCoroutine());

        // ��ġ �̺�Ʈ �߻�
        OnSparrowTouched?.Invoke();

    }

    private IEnumerator TouchReactionCoroutine()
    {
        isTouchReacting = true;

        // ��ġ ���� �ִϸ��̼�_Spin
        PlayAnimation("Spin");

        yield return new WaitForSeconds(spinDuration);

        // �⺻ �ִϸ��̼����� ����
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

        // ��ġ ��� �ݰ�
        return distance <= 100f;
    }

    public void PlayAnimation(string animationName)
    {
        if (sparrowAnimator != null)
        {
            // �ִϸ��̼��� �ִ��� Ȯ�� �� ���
            try
            {
                sparrowAnimator.CrossFade(animationName, 0.2f);
                Debug.Log($"Playing Animation: {animationName}");
            }
            catch
            {
                // �ִϸ��̼��� ������ ����
                Debug.Log($"Animation '{animationName}' not found, using default state");
            }
        }
    }

    // ��ȭ ������ �� ȣ��
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

    // ���� �޼����
    public bool IsActive() => isActive;
    public GameObject GetSparrowInstance() => sparrowInstance;
    public Vector3 GetSparrowPosition() => sparrowInstance?.transform.position ?? Vector3.zero;

    private void OnDestroy()
    {
        if(sparrowInstance != null)
            Destroy(sparrowInstance);
    }
}
