using System.Collections;
using UnityEngine;

/// <summary>
/// Controla um conjunto de luzes / emissive materials para "ligar" limpamente:
/// - interrompe flicker (se houver via coroutine interna)
/// - faz ramp (lerp) da intensity para um valor alvo
/// - opcional: controla emissive color/intensity em materiais (para placas/lâmpadas)
/// 
/// Use UnlockLights(finalIntensity, duration) quando o puzzle for resolvido.
/// </summary>
public class LightController : MonoBehaviour
{
    [Header("Lights (Unity Light components)")]
    public Light[] lights;

    [Header("Emissive materials (opcional)")]
    public Renderer[] emissiveRenderers; // renderers whose material has emission
    public Color emissiveColor = Color.white;
    public float emissiveTargetMultiplier = 1f; // multiplies emissiveColor

    [Header("Flicker safety")]
    public bool ensureRealtimeMode = true; // garante que a light esteja em realtime para efeitos dinâmicos
    public float defaultIntensityFallback = 1f;

    private Coroutine rampCoroutine;
    private Coroutine flickerCoroutine;

    private void OnValidate()
    {
        // suavização no editor
        if (lights != null)
        {
            foreach (var l in lights)
            {
                if (l == null) continue;
                if (ensureRealtimeMode)
                {
                    // não força por script em runtime editor, só log
                    if (Application.isPlaying == false && l.lightmapBakeType != LightmapBakeType.Realtime)
                    {
                        Debug.LogWarning($"[LightController] Light '{l.name}' não está Realtime — considere mudar para Realtime para controlar intensity via runtime.");
                    }
                }
            }
        }
    }

    /// <summary>
    /// Chame isso para ligar luzes de modo suave e parar flicker.
    /// </summary>
    public void UnlockLights(float targetIntensity = 1f, float duration = 0.8f)
    {
        // interrompe coroutines que poderiam estar fazendo flicker
        if (flickerCoroutine != null) { StopCoroutine(flickerCoroutine); flickerCoroutine = null; }

        // garante valores iniciais mínimos sensatos
        if (lights != null)
        {
            foreach (var l in lights)
            {
                if (l == null) continue;
                if (l.enabled == false) l.enabled = true;
            }
        }

        // start ramp
        if (rampCoroutine != null) StopCoroutine(rampCoroutine);
        rampCoroutine = StartCoroutine(RampLightsRoutine(targetIntensity, duration));
    }

    private IEnumerator RampLightsRoutine(float targetIntensity, float duration)
    {
        // read current intensities
        float[] start = new float[lights != null ? lights.Length : 0];
        for (int i = 0; i < start.Length; i++)
        {
            if (lights[i] != null)
            {
                // se light intensity for 0 por default, usa fallback razoavel
                start[i] = (lights[i].intensity <= 0f) ? defaultIntensityFallback * 0.1f : lights[i].intensity;
            }
        }

        // emissive setup: enable keyword
        if (emissiveRenderers != null && emissiveRenderers.Length > 0)
        {
            foreach (var r in emissiveRenderers)
            {
                if (r == null) continue;
                foreach (var m in r.materials)
                {
                    if (m == null) continue;
                    m.EnableKeyword("_EMISSION");
                }
            }
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duration));

            // lights
            for (int i = 0; i < start.Length; i++)
            {
                if (lights[i] == null) continue;
                lights[i].intensity = Mathf.Lerp(start[i], targetIntensity, k);
            }

            // emissive materials
            if (emissiveRenderers != null)
            {
                foreach (var r in emissiveRenderers)
                {
                    if (r == null) continue;
                    foreach (var m in r.materials)
                    {
                        if (m == null) continue;
                        // define cor emissiva multiplicada
                        Color c = emissiveColor * Mathf.Lerp(0f, emissiveTargetMultiplier, k);
                        m.SetColor("_EmissionColor", c);
                        // opcional: se estiver usando URP/HDRP ou shader custom, ajuste conforme necessário
                    }
                }
            }

            yield return null;
        }

        // final set
        if (lights != null)
            foreach (var l in lights) if (l != null) l.intensity = targetIntensity;

        if (emissiveRenderers != null)
        {
            foreach (var r in emissiveRenderers)
            {
                if (r == null) continue;
                foreach (var m in r.materials)
                {
                    if (m == null) continue;
                    m.SetColor("_EmissionColor", emissiveColor * emissiveTargetMultiplier);
                }
            }
        }

        rampCoroutine = null;
    }

    /// <summary>
    /// Caso queira forçar um flicker leve (por exemplo antes de resolver), use isso.
    /// Não usado automaticamente no UnlockLights.
    /// </summary>
    public void StartFlicker(float intensity = 0.4f, float frequency = 12f)
    {
        if (flickerCoroutine != null) StopCoroutine(flickerCoroutine);
        flickerCoroutine = StartCoroutine(FlickerRoutine(intensity, frequency));
    }

    private IEnumerator FlickerRoutine(float intensity, float freq)
    {
        float time = 0f;
        while (true)
        {
            time += Time.unscaledDeltaTime;
            float v = 1f + (Mathf.PerlinNoise(time * freq, 0f) - 0.5f) * intensity;
            if (lights != null)
            {
                foreach (var l in lights) if (l != null) l.intensity = Mathf.Max(0f, l.intensity * v);
            }
            yield return null;
        }
    }
}
