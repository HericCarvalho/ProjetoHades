using UnityEngine;
using System.Collections;

public class PlayerDisruptor : MonoBehaviour
{
    [Header("Refs")]
    public Transform cameraTransform;   
    public Transform playerRoot;         
    public Rigidbody playerRb;          

    [Header("Jitter settings")]
    public float jitterAngle = 2.5f;
    public float jitterPos = 0.03f;
    public float jitterFrequency = 9f;

    [Header("Drift settings")]
    public float driftMagnitude = 0.3f;
    public float driftFrequency = 0.6f;

    [Header("Timing")]
    public float rampIn = 0.6f;
    public float rampOut = 0.8f;

    private float intensity = 0f;    
    private Coroutine currentRoutine;
    private Quaternion camOriginalRot;
    private Vector3 camOriginalPos;

    private void Awake()
    {
        if (cameraTransform == null)
        {
            if (Camera.main != null) cameraTransform = Camera.main.transform;
        }

        camOriginalRot = cameraTransform != null ? cameraTransform.localRotation : Quaternion.identity;
        camOriginalPos = cameraTransform != null ? cameraTransform.localPosition : Vector3.zero;
    }

    private void Update()
    {
        if (intensity <= 0f) return;
        if (cameraTransform != null)
        {
            float jitter = Mathf.PerlinNoise(Time.time * jitterFrequency, 0f) * 2f - 1f;
            float angle = jitter * jitterAngle * intensity;
            Quaternion q = Quaternion.Euler(0f, 0f, angle);
            float px = (Mathf.PerlinNoise(Time.time * jitterFrequency * 1.3f, 1f) - 0.5f) * jitterPos * intensity;
            float py = (Mathf.PerlinNoise(Time.time * jitterFrequency * 1.7f, 2f) - 0.5f) * jitterPos * intensity;
            cameraTransform.localRotation = camOriginalRot * q;
            cameraTransform.localPosition = camOriginalPos + new Vector3(px, py, 0f);
        }

        if (playerRoot != null)
        {
            float dx = (Mathf.PerlinNoise(Time.time * driftFrequency, 10f) - 0.5f) * driftMagnitude * intensity;
            float dz = (Mathf.PerlinNoise(Time.time * driftFrequency, 12f) - 0.5f) * driftMagnitude * intensity;
            Vector3 local = new Vector3(dx, 0f, dz);
            playerRoot.Translate(local * Time.deltaTime, Space.Self);
        }
    }

    public void EnableDisruption(float target = 1f)
    {
        target = Mathf.Clamp01(target);
        if (currentRoutine != null) StopCoroutine(currentRoutine);
        currentRoutine = StartCoroutine(RampIntensity(target, rampIn));
    }

    public void ReduceDisruption()
    {
        if (currentRoutine != null) StopCoroutine(currentRoutine);
        currentRoutine = StartCoroutine(RampIntensity(0.5f, rampOut));
    }

    public void DisableDisruption()
    {
        if (currentRoutine != null) StopCoroutine(currentRoutine);
        currentRoutine = StartCoroutine(RampIntensity(0f, rampOut));
    }

    private IEnumerator RampIntensity(float to, float duration)
    {
        float from = intensity;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            intensity = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        intensity = to;
        if (intensity == 0f)
        {
            if (cameraTransform != null)
            {
                cameraTransform.localRotation = camOriginalRot;
                cameraTransform.localPosition = camOriginalPos;
            }
        }
        currentRoutine = null;
    }
    public void ApplyImpulseBackward(float force = 3f)
    {
        if (playerRb != null)
        {
            playerRb.AddForce(-playerRoot.forward * force, ForceMode.VelocityChange);
        }
        else if (playerRoot != null)
        {
            playerRoot.position += -playerRoot.forward * 0.4f;
        }
    }
}
