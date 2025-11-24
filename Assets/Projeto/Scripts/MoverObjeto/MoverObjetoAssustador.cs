using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class MoverObjetoAssustador : MonoBehaviour
{
    [Header("Alvo a mover")]
    public Transform objetoParaMover;     
    public Transform destino;               
    public Vector3 destinoOffset = Vector3.zero; 
    public bool useDestinoTransform = true;

    [Header("Movimento")]
    public float delayAntes = 0.15f;        
    public float duracaoMovimento = 0.6f;    
    public AnimationCurve curvaMovimento = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public bool unparentDuringMove = false; 

    [Header("Efeito sonoro")]
    public AudioClip sfxAssustador;         
    public float sfxVolume = 1f;
    public AudioSource sfxSource;           

    [Header("Impacto no jogador / câmera")]
    public bool aplicarImpulsoPlayer = true;
    public float impulsoForce = 2.5f;
    public bool usarPlayerDisruptor = true; 
    public PlayerDisruptor playerDisruptor; 
    public float disruptorIntensity = 1f;
    public float disruptorDuration = 0.7f;

    [Header("Comportamento")]
    public bool oneShot = true;             
    public bool disableTriggerAfter = true; 

    [Header("Extras (opcional)")]
    public bool aplicarRotacaoLeve = true;
    public Vector3 rotacaoAlvoEuler = new Vector3(0, 15f, 0);
    public float rotacaoDuracao = 0.4f;

    private bool triggered = false;
    private Collider myCollider;

    private void Reset()
    {
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    private void Awake()
    {
        myCollider = GetComponent<Collider>();
        if (myCollider == null) Debug.LogWarning("[MoverObjetoAssustador] Collider não encontrado.");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggered && oneShot) return;

        if (other.CompareTag("Player") || other.transform.IsChildOf(GameObject.FindGameObjectWithTag("Player")?.transform))
        {
            StartCoroutine(TriggerRoutine(other.gameObject));
        }
    }

    private IEnumerator TriggerRoutine(GameObject player)
    {
        triggered = true;

        if (disableTriggerAfter && myCollider != null) myCollider.enabled = false;

        yield return new WaitForSeconds(delayAntes);

        if (sfxSource != null && sfxAssustador != null)
        {
            sfxSource.PlayOneShot(sfxAssustador, sfxVolume);
        }
        else if (sfxAssustador != null)
        {
            AudioSource.PlayClipAtPoint(sfxAssustador, transform.position, sfxVolume);
        }

        if (aplicarImpulsoPlayer && player != null)
        {
            Rigidbody rb = player.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 backDir = -player.transform.forward;
                rb.AddForce(backDir.normalized * impulsoForce, ForceMode.VelocityChange);
            }
            else
            {
                player.transform.position += -player.transform.forward * 0.25f;
            }
        }

        if (usarPlayerDisruptor && playerDisruptor != null)
        {
            playerDisruptor.EnableDisruption(disruptorIntensity);
            StartCoroutine(DisableDisruptorAfter(disruptorDuration));
        }

        if (objetoParaMover != null)
        {
            Transform originalParent = objetoParaMover.parent;
            if (unparentDuringMove) objetoParaMover.SetParent(null);

            Vector3 startPos = objetoParaMover.position;
            Quaternion startRot = objetoParaMover.rotation;

            Vector3 targetPos = useDestinoTransform && destino != null ? destino.position : (startPos + destinoOffset);
            Quaternion targetRot = useDestinoTransform && destino != null ? destino.rotation : Quaternion.Euler(rotacaoAlvoEuler) * startRot;

            float elapsed = 0f;
            while (elapsed < duracaoMovimento)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duracaoMovimento);
                float k = (curvaMovimento != null) ? curvaMovimento.Evaluate(t) : t;
                objetoParaMover.position = Vector3.Lerp(startPos, targetPos, k);

                if (aplicarRotacaoLeve)
                    objetoParaMover.rotation = Quaternion.Slerp(startRot, targetRot, Mathf.Clamp01(k * (duracaoMovimento / Mathf.Max(0.01f, rotacaoDuracao))));

                yield return null;
            }

            objetoParaMover.position = targetPos;
            if (aplicarRotacaoLeve) objetoParaMover.rotation = targetRot;

            if (unparentDuringMove) objetoParaMover.SetParent(originalParent);
        }

        if (oneShot)
        {

        }
    }

    private IEnumerator DisableDisruptorAfter(float t)
    {
        yield return new WaitForSeconds(t);
        if (playerDisruptor != null) playerDisruptor.DisableDisruption();
    }
}
