using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

[DisallowMultipleComponent]
public class SecurityCameraSystem : MonoBehaviour
{
    [Header("Cameras (fixas)")]
    public Camera[] cameras;
    public bool loop = true;

    [Header("Tempos")]
    public float timePerCamera = 10f;
    public float fadeDuration = 0.8f;
    public float staticDurationMin = 0.6f;
    public float staticDurationMax = 1.8f;

    [Header("Sway (movimento suave)")]
    public float swayRotationAmount = 0.8f;
    public float swayPositionAmount = 0.02f;
    public float swaySpeed = 0.4f;

    [Header("Estática visual (RawImage)")]
    public RawImage staticRawImage;
    [Range(0f, 1f)] public float staticMaxAlpha = 0.85f;
    public float staticFlickerSpeed = 30f;
    public bool useStatic = true;
    [Tooltip("Velocidade de scroll UV da textura de estática (x,y)")]
    public Vector2 staticUVScroll = new Vector2(0.2f, 0f);

    [Header("Fade (Image)")]
    public Image fadeImage;
    public bool fadeUnscaled = true;

    [Header("Audio (opcional)")]
    public AudioSource staticSfxSource;
    public float staticSfxVolume = 0.6f;
    public bool playStaticSfx = true;
    [Tooltip("Se true, o SFX é tocado apenas durante o período de estática (mais imersivo)")]
    public bool playStaticSfxOnlyDuringStatic = true;

    [Header("Misc")]
    public bool startOnAwake = true;
    public int startIndex = 0;

    // internals
    private Quaternion[] baseRotations;
    private Vector3[] basePositions;
    private int currentIndex = 0;
    private Coroutine cycleCoroutine;
    private bool isRunning = false;

    // UV control
    private Rect staticUVRect = new Rect(0, 0, 1, 1);

    void Awake()
    {
        if (cameras == null || cameras.Length == 0)
        {
            Debug.LogWarning("SecurityCameraSystem: nenhuma câmera atribuída.");
            enabled = false;
            return;
        }

        // garantir startIndex válido antes de ativar câmeras
        startIndex = Mathf.Clamp(startIndex, 0, cameras.Length - 1);
        currentIndex = startIndex;

        baseRotations = new Quaternion[cameras.Length];
        basePositions = new Vector3[cameras.Length];
        for (int i = 0; i < cameras.Length; i++)
        {
            baseRotations[i] = cameras[i].transform.localRotation;
            basePositions[i] = cameras[i].transform.localPosition;
        }

        // assegura que apenas a câmera inicial esteja ativa
        for (int i = 0; i < cameras.Length; i++)
            SetCameraActive(i, i == startIndex);

        if (fadeImage != null)
        {
            Color c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;
            fadeImage.raycastTarget = false;
        }

        if (staticRawImage != null)
        {
            var c = staticRawImage.color;
            c.a = 0f;
            staticRawImage.color = c;
            staticRawImage.raycastTarget = false;

            // iniciar uvRect com tiling igual a 1 (pode ajustar)
            staticUVRect = staticRawImage.uvRect;
        }

        if (staticSfxSource != null)
        {
            staticSfxSource.loop = true;
            staticSfxSource.playOnAwake = false;
            staticSfxSource.volume = staticSfxVolume;
        }

        if (startOnAwake)
            StartSystem();
    }

    void OnEnable()
    {
        if (startOnAwake && !isRunning)
            StartSystem();
    }

    void OnDisable()
    {
        StopSystem();
    }

    public void StartSystem()
    {
        if (isRunning) return;
        isRunning = true;
        cycleCoroutine = StartCoroutine(CycleRoutine());
        // se preferir tocar SFX contínuo, mantenha; caso contrário o SFX será tocado apenas durante a estática
        if (playStaticSfx && staticSfxSource != null && !playStaticSfxOnlyDuringStatic)
            staticSfxSource.Play();
    }

    public void StopSystem()
    {
        if (!isRunning) return;
        isRunning = false;
        if (cycleCoroutine != null)
        {
            StopCoroutine(cycleCoroutine);
            cycleCoroutine = null;
        }
        if (staticSfxSource != null)
            staticSfxSource.Stop();

        for (int i = 0; i < cameras.Length; i++)
        {
            cameras[i].transform.localRotation = baseRotations[i];
            cameras[i].transform.localPosition = basePositions[i];
        }
        if (fadeImage != null) SetImageAlpha(fadeImage, 0f);
        if (staticRawImage != null) SetImageAlpha(staticRawImage, 0f);
    }

    void Update()
    {
        if (!isRunning) return;
        Camera cam = cameras[currentIndex];
        ApplySwayToCamera(cam, currentIndex);
    }

    private void ApplySwayToCamera(Camera cam, int index)
    {
        if (cam == null) return;

        float t = Time.time * swaySpeed;
        float nx = Mathf.PerlinNoise(t, index * 10f) - 0.5f;
        float ny = Mathf.PerlinNoise(index * 10f, t) - 0.5f;
        float rotX = nx * swayRotationAmount;
        float rotY = ny * swayRotationAmount;

        float px = (Mathf.PerlinNoise(t * 0.9f + 5f, index * 15f) - 0.5f) * swayPositionAmount;
        float py = (Mathf.PerlinNoise(index * 15f, t * 0.9f + 5f) - 0.5f) * swayPositionAmount;

        cam.transform.localRotation = baseRotations[index] * Quaternion.Euler(rotX, rotY, 0f);
        cam.transform.localPosition = basePositions[index] + new Vector3(px, py, 0f);
    }

    private IEnumerator CycleRoutine()
    {
        while (isRunning)
        {
            float visibleTime = Mathf.Max(0.1f, timePerCamera - (staticDurationMax + fadeDuration * 2f));
            yield return new WaitForSecondsRealtime(visibleTime);

            // mostra estática aleatória antes do fade
            if (useStatic && staticRawImage != null)
            {
                float staticTime = Random.Range(staticDurationMin, staticDurationMax);

                // tocar sfx somente durante a estática (opcional)
                if (playStaticSfx && staticSfxSource != null && playStaticSfxOnlyDuringStatic)
                    staticSfxSource.Play();

                float elapsed = 0f;
                while (elapsed < staticTime)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float flick = Mathf.PerlinNoise(Time.time * staticFlickerSpeed, currentIndex * 7f);
                    float a = Mathf.Lerp(0f, staticMaxAlpha, flick);
                    SetImageAlpha(staticRawImage, a);

                    // UV scroll durante estática
                    staticUVRect.x += staticUVScroll.x * Time.unscaledDeltaTime;
                    staticUVRect.y += staticUVScroll.y * Time.unscaledDeltaTime;
                    staticRawImage.uvRect = staticUVRect;

                    yield return null;
                }

                // parar sfx de estática ao fim do período (se estava tocando apenas durante static)
                if (playStaticSfx && staticSfxSource != null && playStaticSfxOnlyDuringStatic)
                    staticSfxSource.Stop();
            }

            // FADE OUT
            if (fadeImage != null)
            {
                yield return StartCoroutine(FadeImageRoutine(fadeImage, 0f, 1f, fadeDuration, fadeUnscaled));
            }

            yield return new WaitForSecondsRealtime(0.08f);

            int next = (currentIndex + 1) % cameras.Length;
            SetCameraActive(currentIndex, false);
            SetCameraActive(next, true);
            currentIndex = next;

            // FADE IN
            if (fadeImage != null)
            {
                yield return StartCoroutine(FadeImageRoutine(fadeImage, 1f, 0f, fadeDuration, fadeUnscaled));
            }

            // limpa estática após troca
            if (staticRawImage != null)
            {
                SetImageAlpha(staticRawImage, 0f);
                // optional: reset uvRect.x/y if you prefer
                // staticUVRect.x = staticUVRect.y = 0f;
                // staticRawImage.uvRect = staticUVRect;
            }

            if (!loop && currentIndex == cameras.Length - 1)
            {
                isRunning = false;
                break;
            }
        }
        cycleCoroutine = null;
    }

    private IEnumerator FadeImageRoutine(Image img, float from, float to, float dur, bool useUnscaled)
    {
        if (img == null) yield break;
        float t = 0f;
        Color c = img.color;
        while (t < dur)
        {
            t += useUnscaled ? Time.unscaledDeltaTime : Time.deltaTime;
            float a = Mathf.Lerp(from, to, Mathf.Clamp01(t / dur));
            c.a = a; img.color = c;
            yield return null;
        }
        c.a = to; img.color = c;
        img.raycastTarget = to > 0.95f;
    }

    private void SetImageAlpha(Image img, float a)
    {
        if (img == null) return;
        Color c = img.color; c.a = a; img.color = c;
        img.raycastTarget = a > 0.95f;
    }

    private void SetImageAlpha(RawImage img, float a)
    {
        if (img == null) return;
        Color c = img.color; c.a = a; img.color = c;
        img.raycastTarget = a > 0.95f;
    }

    private void SetCameraActive(int index, bool active)
    {
        if (index < 0 || index >= cameras.Length) return;
        cameras[index].gameObject.SetActive(active);
    }

    public void SwitchToCameraIndex(int idx)
    {
        if (idx < 0 || idx >= cameras.Length) return;
        SetCameraActive(currentIndex, false);
        SetCameraActive(idx, true);
        currentIndex = idx;
    }
}
