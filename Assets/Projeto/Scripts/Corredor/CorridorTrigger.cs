using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CorridorTrigger : MonoBehaviour
{
    [Header("Refs")]
    public Transform playerTransform;           
    public Rigidbody playerRigidbody;           
    public NoiseOverlay noiseOverlay;           
    public PlayerDisruptor playerDisruptor;     
    public float stepBackForce = 3f;            
    public float entryDelay = 0.1f;             
    public string messageOnEnter = "Não quero ir por ali..."; 
    public float messageDuration = 3f;

    [Header("Behavior")]
    public bool unlocked = false;  
    public bool oneShot = false; 
    private bool triggered = false;

    private void Reset()
    {
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggered && oneShot) return;
        if (unlocked) return;

        if (playerTransform == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerTransform = p.transform;
        }

        if (playerRigidbody == null && playerTransform != null)
        {
            playerRigidbody = playerTransform.GetComponent<Rigidbody>();
        }

        if (other.transform == playerTransform || other.transform.IsChildOf(playerTransform) || other.CompareTag("Player"))
        {
            triggered = true;
            HUD_Interacao.instancia?.MostrarMensagem(messageOnEnter);

            if (playerRigidbody != null)
            {
                Vector3 backDir = -playerTransform.forward;
                playerRigidbody.AddForce(backDir.normalized * stepBackForce, ForceMode.VelocityChange);
            }
            else if (playerTransform != null)
            {
                playerTransform.position += -playerTransform.forward * 0.4f;
            }
            Invoke(nameof(ActivateEffects), entryDelay);
        }
    }

    private void ActivateEffects()
    {
        if (unlocked) return;
        if (noiseOverlay != null) noiseOverlay.FadeTo(1f, 0.25f);
        if (playerDisruptor != null) playerDisruptor.EnableDisruption();
    }

    private void OnTriggerExit(Collider other)
    {
        if (unlocked) return;
        if (other.transform == playerTransform || other.transform.IsChildOf(playerTransform) || other.CompareTag("Player"))
        {
            if (playerDisruptor != null) playerDisruptor.ReduceDisruption();
            if (noiseOverlay != null) noiseOverlay.FadeTo(0.5f, 0.6f);
        }
    }
    public void UnlockCorridor()
    {
        unlocked = true;
        if (playerDisruptor != null) playerDisruptor.DisableDisruption();
        if (noiseOverlay != null) noiseOverlay.FadeTo(0f, 0.5f);
    }
}
