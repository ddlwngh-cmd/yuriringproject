using UnityEngine;

[DisallowMultipleComponent]
public class ExplosionEffectController : MonoBehaviour
{
    [SerializeField, Min(0f)] private float effectSize = 3f;
    [SerializeField, Min(0.01f)] private float effectLifeTime = 0.35f;
    [SerializeField, Range(0f, 1f)] private float startScaleRatio = 0.15f;

    private SpriteRenderer[] spriteRenderers;
    private Color[] originalColors;
    private float elapsedTime;

    public float EffectSize
    {
        get => effectSize;
        set => effectSize = Mathf.Max(0f, value);
    }

    public float EffectLifeTime
    {
        get => effectLifeTime;
        set => effectLifeTime = Mathf.Max(0.01f, value);
    }

    private void Awake()
    {
        CacheSpriteRenderers();
    }

    private void OnEnable()
    {
        ResetVisuals();
    }

    private void OnValidate()
    {
        effectSize = Mathf.Max(0f, effectSize);
        effectLifeTime = Mathf.Max(0.01f, effectLifeTime);
        startScaleRatio = Mathf.Clamp01(startScaleRatio);
    }

    private void Update()
    {
        elapsedTime += Time.deltaTime;
        float normalizedTime = Mathf.Clamp01(elapsedTime / effectLifeTime);
        float easedTime = 1f - Mathf.Pow(1f - normalizedTime, 3f);

        float currentSize = Mathf.Lerp(effectSize * startScaleRatio, effectSize, easedTime);
        transform.localScale = Vector3.one * currentSize;

        float alpha = 1f - normalizedTime;
        ApplyAlpha(alpha);

        if (elapsedTime >= effectLifeTime)
        {
            gameObject.SetActive(false);
        }
    }

    public void Play(Vector3 position)
    {
        transform.position = position;
        elapsedTime = 0f;
        gameObject.SetActive(true);
        ResetVisuals();
    }

    private void ResetVisuals()
    {
        elapsedTime = 0f;
        transform.localScale = Vector3.one * (effectSize * startScaleRatio);
        ApplyAlpha(1f);
    }

    private void ApplyAlpha(float alpha)
    {
        CacheSpriteRenderers();
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            SpriteRenderer spriteRenderer = spriteRenderers[i];
            if (spriteRenderer == null)
            {
                continue;
            }

            Color color = i < originalColors.Length ? originalColors[i] : spriteRenderer.color;
            color.a *= Mathf.Clamp01(alpha);
            spriteRenderer.color = color;
        }
    }

    private void CacheSpriteRenderers()
    {
        if (spriteRenderers != null && originalColors != null)
        {
            return;
        }

        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        originalColors = new Color[spriteRenderers.Length];
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            originalColors[i] = spriteRenderers[i] != null ? spriteRenderers[i].color : Color.white;
        }
    }
}
