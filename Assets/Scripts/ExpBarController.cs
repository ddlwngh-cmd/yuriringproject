using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class ExpBarController : MonoBehaviour
{
    [SerializeField] private PlayerExperiences playerExperiences;
    [SerializeField, Min(0.01f)] private float fillAnimationSeconds = 0.18f;

    private Image expBarFill;
    private Coroutine fillRoutine;

    private void Awake()
    {
        expBarFill = GetComponent<Image>();
    }

    private void OnEnable()
    {
        Subscribe();
    }

    private void Start()
    {
        if (playerExperiences == null)
        {
            playerExperiences = PlayerExperiences.Instance;
        }

        SetFillAmount(playerExperiences != null ? playerExperiences.ExpRatio : 0f);
        Subscribe();
    }

    private void OnDisable()
    {
        if (playerExperiences != null)
        {
            playerExperiences.ExpRatioChanged -= OnExpRatioChanged;
        }
    }

    private void Subscribe()
    {
        if (playerExperiences == null)
        {
            playerExperiences = PlayerExperiences.Instance;
        }

        if (playerExperiences == null)
        {
            return;
        }

        playerExperiences.ExpRatioChanged -= OnExpRatioChanged;
        playerExperiences.ExpRatioChanged += OnExpRatioChanged;
    }

    private void OnExpRatioChanged(float ratio, bool animate)
    {
        ratio = Mathf.Clamp01(ratio);

        if (fillRoutine != null)
        {
            StopCoroutine(fillRoutine);
            fillRoutine = null;
        }

        if (!animate)
        {
            SetFillAmount(ratio);
            return;
        }

        fillRoutine = StartCoroutine(AnimateFill(ratio));
    }

    private IEnumerator AnimateFill(float targetRatio)
    {
        float startRatio = expBarFill.fillAmount;
        float elapsed = 0f;

        while (elapsed < fillAnimationSeconds)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fillAnimationSeconds);
            float easedT = Mathf.SmoothStep(0f, 1f, t);
            SetFillAmount(Mathf.Lerp(startRatio, targetRatio, easedT));
            yield return null;
        }

        SetFillAmount(targetRatio);
        fillRoutine = null;
    }

    private void SetFillAmount(float amount)
    {
        if (expBarFill == null)
        {
            return;
        }

        expBarFill.fillAmount = Mathf.Clamp01(amount);
    }
}
