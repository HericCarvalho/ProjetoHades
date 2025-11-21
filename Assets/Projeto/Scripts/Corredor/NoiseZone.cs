using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider))]
public class NoiseZone : MonoBehaviour
{
    [Header("Audio")]
    public AudioSource audioSource;              
    public AudioLowPassFilter lowPassFilter;     
    [Range(0f, 1f)] public float maxVolume = 1f;
    [Range(500f, 22000f)] public float lowPassMinCutoff = 800f;
    [Range(500f, 22000f)] public float lowPassMaxCutoff = 22000f;

    [Header("Player disruption")]
    public PlayerDisruptor playerDisruptor;
    [Range(0f, 1f)] public float disruptorMaxIntensity = 1f;

    [Header("Behaviour")]
    public bool keepUntilUnlocked = false;      
    public float rampSpeed = 2f;                
    public bool debugLogs = false;

    private bool playerInside = false;
    private bool unlocked = false;
    private Transform playerTransform;
    private Collider zoneCollider;
    private float current = 0f;

    private void Reset()
    {
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    private void Awake()
    {
        zoneCollider = GetComponent<Collider>();
        if (zoneCollider == null) Debug.LogWarning("[NoiseZone] Collider required.");

        if (audioSource != null)
        {
            audioSource.loop = true;
            audioSource.volume = 0f;
            if (!audioSource.isPlaying) audioSource.Play();
        }

        if (lowPassFilter != null)
            lowPassFilter.cutoffFrequency = lowPassMaxCutoff;
    }

    private void Update()
    {
        float target = 0f;
        if (playerInside && !unlocked)
        {
            if (playerTransform != null && zoneCollider != null)
            {
                Vector3 closest = zoneCollider.ClosestPoint(playerTransform.position);
                float dist = Vector3.Distance(closest, playerTransform.position);
                Bounds b = zoneCollider.bounds;
                float maxDist = Mathf.Max(b.extents.x, b.extents.y, b.extents.z) * 1.2f;
                float normalized = Mathf.Clamp01(1f - (dist / maxDist));
                target = normalized;
            }
            else
            {
                target = 1f;
            }
        }

        if (unlocked) target = 0f;

        current = Mathf.MoveTowards(current, target, Time.unscaledDeltaTime * rampSpeed);

        if (audioSource != null)
            audioSource.volume = Mathf.Clamp01(current * maxVolume);

        if (lowPassFilter != null)
        {
            float cutoff = Mathf.Lerp(lowPassMaxCutoff, lowPassMinCutoff, current);
            lowPassFilter.cutoffFrequency = Mathf.Lerp(lowPassFilter.cutoffFrequency, cutoff, Time.unscaledDeltaTime * rampSpeed * 2f);
        }
        if (playerDisruptor != null)
        {
            float targetIntensity = current * disruptorMaxIntensity;
            if (current > 0.01f)
                playerDisruptor.EnableDisruption(targetIntensity);
            else
                playerDisruptor.DisableDisruption();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (playerTransform == null)
        {
            var pgo = GameObject.FindGameObjectWithTag("Player");
            if (pgo != null) playerTransform = pgo.transform;
        }

        if (other.transform == playerTransform || other.CompareTag("Player") || (playerTransform != null && other.transform.IsChildOf(playerTransform)))
        {
            playerInside = true;
            if (debugLogs) Debug.Log("[NoiseZone] Player entrou no zone.");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.transform == playerTransform || other.CompareTag("Player") || (playerTransform != null && other.transform.IsChildOf(playerTransform)))
        {
            playerInside = false;
            if (debugLogs) Debug.Log("[NoiseZone] Player saiu do zone.");
            if (!keepUntilUnlocked && playerDisruptor == null)
            {
            }
        }
    }

    public void Unlock()
    {
        unlocked = true;
        playerInside = false;
        if (debugLogs) Debug.Log("[NoiseZone] Unlock chamado -> fading out.");
    }

    public void ForceEnable()
    {
        unlocked = false;
        playerInside = true;
    }
}
