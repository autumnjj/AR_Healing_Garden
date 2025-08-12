using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SimpleImageEffects : MonoBehaviour
{
    [Header("���� UI ĵ����")]
    public GameObject imagePanel;

    [Header("����Ʈ �̹���")]
    public Image voiceSuccessImage;
    public Image sproutImage;
    public Image growingImage;
    public Image bloomingImage;

    [Header("ǥ�� ����")]
    public float displayDuration = 1.5f;

    private void Start()
    {
        HideAllImages();
    }

    private void HideAllImages()
    {
        if (voiceSuccessImage != null) voiceSuccessImage.gameObject.SetActive(false);
        if (sproutImage != null) sproutImage.gameObject.SetActive(false);
        if (growingImage != null) growingImage.gameObject.SetActive(false);
        if (bloomingImage != null) bloomingImage.gameObject.SetActive(false);
    }

    // ���� ������ ����Ʈ
    public void PlayVoiceSuccessEffect()
    {
        ShowImage(voiceSuccessImage);
        Debug.Log("VoiceSuccessImage");
    }

    // ���� �ܰ� ����Ʈ
    public void PlaySproutEffect()
    {
        ShowImage(sproutImage);
    }

    // ���� �ܰ� ����Ʈ
    public void PlayGrowingEffect()
    {
        ShowImage(growingImage);
    }

    // ��ȭ �ܰ� ����Ʈ
    public void PlayBloomingEffect()
    {
        ShowImage(bloomingImage);
    }

    private void ShowImage(Image targetImage)
    {
        if (targetImage == null) return;

        StopAllCoroutines();

        HideAllImages();
        targetImage.gameObject.SetActive(true);

        if (imagePanel != null)
            imagePanel.SetActive(true);

        StartCoroutine(HideImageAfterDelay(targetImage));
    }

    private IEnumerator HideImageAfterDelay(Image targetImage)
    {
        yield return new WaitForSeconds(displayDuration);

        if (targetImage != null)
            targetImage.gameObject.SetActive(false);

        if (imagePanel != null && !IsAnyImageShowing())
        {
            imagePanel.SetActive(false);
        }
    }

    public void HideAllImagesImmediately()
    {
        StopAllCoroutines();
        HideAllImages();

        if (imagePanel != null)
            imagePanel.SetActive(false);
    }

    // ǥ�� �ð� ����
    public void SetDisplayDuration(float duration)
    {
        displayDuration = duration;
    }

    // ���� � �̹����� ǥ�� ������ Ȯ��
    public bool IsAnyImageShowing()
    {
        return (voiceSuccessImage != null && voiceSuccessImage.gameObject.activeInHierarchy) ||
               (sproutImage != null && sproutImage.gameObject.activeInHierarchy) ||
               (growingImage != null && growingImage.gameObject.activeInHierarchy) ||
               (bloomingImage != null && bloomingImage.gameObject.activeInHierarchy);
    }

    // Ư�� �̹����� ǥ�� ������ Ȯ��
    public bool IsImageShowing(Image targetImage)
    {
        return targetImage != null && targetImage.gameObject.activeInHierarchy;
    }
}
