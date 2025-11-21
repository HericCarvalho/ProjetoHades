using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(CanvasGroup))]
public class NoiseOverlay : MonoBehaviour
{
    [Header("UI")]
    public RawImage noiseImage; 
    public float uvSpeed = 0.8f; 
    public Vector2 uvScale = new Vector2(2f, 2f);

    [Header("Alpha")]
    [Range(0f, 1f)] public float maxAlpha = 1f;
    public float flickerFrequency = 8f;
    private float targetAlpha = 0f;
    private Coroutine fadeRoutine;
    private CanvasGroup cg;

    private void Awake()
    {
        cg = GetComponent<CanvasGroup>();
        if (noiseImage != null)
        {
            noiseImage.uvRect = new Rect(0, 0, uvScale.x, uvScale.y);
            noiseImage.enabled = true;
        }
        cg.alpha = 0f;
        targetAlpha = 0f;
    }

    private void Update()
    {
        if (noiseImage != null && targetAlpha > 0f)
        {
            Rect r = noiseImage.uvRect;
            r.x += Time.unscaledDeltaTime * uvSpeed;
            r.y += Time.unscaledDeltaTime * (uvSpeed * 0.4f);
            noiseImage.uvRect = r;

            float flicker = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * flickerFrequency * (0.8f + targetAlpha));
            cg.alpha = Mathf.Lerp(cg.alpha, targetAlpha * flicker, Time.unscaledDeltaTime * 8f);
        }
        else
        {
            cg.alpha = Mathf.Lerp(cg.alpha, targetAlpha, Time.unscaledDeltaTime * 6f);
        }
    }

    public void FadeTo(float alpha, float duration = 0.3f)
    {
        alpha = Mathf.Clamp01(alpha) * maxAlpha;
        targetAlpha = alpha;
    }
    public void SetUVScale(Vector2 s)
    {
        uvScale = s;
        if (noiseImage != null) noiseImage.uvRect = new Rect(0, 0, uvScale.x, uvScale.y);
    }
}
