using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class ARPlantController : MonoBehaviour
{
    [Header("Plant Visual Components")]
    public Renderer plantRenderer;
    public Animator plantAnimator;

    [Header("Touch Feedback")]
    public GameObject touchFeedbackPrefab;
    public AudioClip touchSound;

    [Header("Billboard Settings")]
    public bool enableBillboard = true;
    public Transform billboardTarget;

    [Header("Growth Effects")]
    public ParticleSystem growthParticles;

    private PlantGrowthData growthData;
    private PlantGrowthUI growthUI;
    private Camera arCamera;
    private AudioSource audioSource;

    // Billboard 관련
    private Vector3 originalRotation;

    private void Start()
    {
        SetupComponents();
        SetupBillboard();
    }

    private void SetupComponents()
    {
        arCamera = Camera.main;
        if(arCamera == null)
        {
            arCamera = FindAnyObjectByType<Camera>();
        }

        if(billboardTarget == null && arCamera != null)
        {
            billboardTarget = arCamera.transform;
        }

        if(plantRenderer == null)
        {
            plantRenderer = GetComponentInChildren<Renderer>();
        }

        if (plantAnimator == null)
        {
            plantAnimator = GetComponentInChildren<Animator>();
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if(audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        originalRotation = transform.eulerAngles;
    }

    private void SetupBillboard()
    {
        if(enableBillboard && billboardTarget != null)
        {
            // 카메라를 향하도록 초기 회전 설정
            LookAtCamera();
        }
    }

    private void Update()
    {
        if(enableBillboard && billboardTarget != null)
        {
            LookAtCamera();
        }
    }

    private void LookAtCamera()
    {
        Vector3 targetPosition = billboardTarget.position;
        targetPosition.y = transform.position.y;

        transform.LookAt(targetPosition);
        transform.Rotate(0, 180, 0);
    }

    public void Initialize(PlantGrowthData growthData, PlantGrowthUI growthUI)
    {
        this.growthData = growthData;
        this.growthUI = growthUI;

        if(growthData != null)
        {
            growthData.OnStageChanged += OnStageChanged;
            growthData.OnInteractionPerformed += OnInteractionPerformed;
        }

        UpdateVisualState();
    }

    public void OnTouchFeedback(Vector3 touchWorldPosition)
    {
        // 터치 위치에 피드백 효과 생성
        if(touchFeedbackPrefab != null)
        {
            GameObject feedback = Instantiate(touchFeedbackPrefab, touchWorldPosition, Quaternion.identity);
            Destroy(feedback, 2f);
        }

        // 터치 사운드 재생
        if (audioSource != null && touchSound != null) 
        {
            audioSource.PlayOneShot(touchSound);
        }

        // 시각적 피드백
        StartCoroutine(TouchVisualFeedback());

        Debug.Log($"Touch feedback at position : {touchWorldPosition}");
    }

    private IEnumerator TouchVisualFeedback()
    {
        if (plantRenderer != null) 
        {
            Color originalColor = plantRenderer.material.color;
            Color brightColor = Color.white;

            // 밝게 깜빡임
            for (int i = 0; i < 3; i++)
            {
                plantRenderer.material.color = brightColor;
                yield return new WaitForSeconds(0.1f);
                plantRenderer.material.color = originalColor;
                yield return new WaitForSeconds(0.1f);
            }
        }
    }

    private void OnStageChanged(PlantGrowthStage newStage)
    {
        UpdateVisualState();
        PlayGrowthEffect();

        // 애니메이션 트리거
        if(plantAnimator != null)
        {
            //plantAnimator.SetTrigger("GrowthStage");
            //plantAnimator.SetTrigger("Stagee", (int)newStage);
        }
    }

    private void OnInteractionPerformed(InteractionType interactionType, float points)
    {
        // 상호작용별 시각적 피드백
        switch (interactionType)
        {
            case InteractionType.PositiveTalk:
                PlayTalkEffect();
                break;
            case InteractionType.WaterGiving:
                PlayWaterEffect();
                break;
            case InteractionType.TouchCare:
                OnTouchFeedback(transform.position);
                break;
            case InteractionType.SunlightGiving:
                PlaySunlightEffect();
                break;
        }
    }

    private void UpdateVisualState()
    {
        if (growthData == null) return;

        var currentState = growthData.GetCurrentState().currentStage;
        var stageData = growthData.GetCurrentStageData();

        if (stageData != null) 
        {
            if(plantRenderer != null)
            {
                plantRenderer.material.color = stageData.stageColor;
            }

            // 크기 변경
            transform.localScale = stageData.stageScale;

            // 스프라이트 변경(2D 식물의 경우)
            var spriteRenderer = GetComponent<SpriteRenderer>();
            if(spriteRenderer != null && stageData.stageSprite != null)
            {
                spriteRenderer.sprite = stageData.stageSprite;
            } 
        }
    }

    private void PlayGrowthEffect()
    {
        if(growthParticles != null)
        {
            growthParticles.Play();
        }

        // 성장 시 펄스 효과
        StartCoroutine(PulseEffect());
    }

    private void PlayTalkEffect()
    {
        Debug.Log("Playing talk effect");
    }

    private void PlayWaterEffect()
    {
        Debug.Log("Playing water effect");
    }

    private void PlaySunlightEffect()
    {
        Debug.Log("Playing sunlight effect");
    }

    private IEnumerator PulseEffect()
    {
        Vector3 originalScale = transform.localScale;
        Vector3 targetScale = originalScale * 1.2f;

        float duration = 0.5f;
        float elapsedTime = 0f;

        // 확대
        while(elapsedTime < duration)
        {
            float t = elapsedTime / duration;
            transform.localScale = Vector3.Lerp(originalScale, targetScale, t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        elapsedTime = 0f;

        // 축소
        while(elapsedTime < duration)
        {
            float t = elapsedTime / duration;
            transform.localScale = Vector3.Lerp(targetScale, originalScale, t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        transform.localScale = originalScale;
    }

    // Bilboard 설정 변경
    public void SetBillboardEnabled(bool enabled)
    {
        enableBillboard = enabled;

        if (!enabled)
        {
            transform.eulerAngles = originalRotation;
        }
    }

    // 식물 하이라이트 효과
    public void HighlightPlant(bool highlight)
    {
        if(plantRenderer != null)
        {
            if (highlight)
            {
                // 외각선 효과나 밝기 증가
                plantRenderer.material.SetFloat("_Brightness", 1.5f);
            }
            else
            {
                plantRenderer.material.SetFloat("_Brightness", 1.0f);
            }
        }
    }

    private void OnDestroy()
    {
        if(growthData != null)
        {
            growthData.OnStageChanged -= OnStageChanged;
            growthData.OnInteractionPerformed -= OnInteractionPerformed;
        }
    }
}
