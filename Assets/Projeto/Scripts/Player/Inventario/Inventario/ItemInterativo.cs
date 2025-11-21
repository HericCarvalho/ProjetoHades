using UnityEngine;
using UnityEngine.Events;

public class ItemInterativo : MonoBehaviour
{
    public TipoItem tipo = TipoItem.Interativo;

    [TextArea] public string descricao;

    [Header("Áudio")]
    public AudioClip somColeta;

    public ItemSistema itemColetavel;

    [Header("Ajuste do popup (opcional)")]
    public Transform interactionPoint;
    public Vector3 popupOffset = Vector3.zero;

    public UnityEvent onInteragir;

    public void Interagir(MovimentaçãoPlayer jogador)
    {
        switch (tipo)
        {
            case TipoItem.Interativo:
                HUD_Interacao.instancia?.MostrarMensagem(descricao);
                break;

            case TipoItem.Coletavel:
                var collectibleComponent = GetComponent<CollectibleItem>();
                if (collectibleComponent == null)
                    collectibleComponent = GetComponentInChildren<CollectibleItem>();

                if (collectibleComponent != null)
                {
                    Debug.Log($"[ItemInterativo] Found CollectibleItem on '{gameObject.name}', calling Collect()");
                    collectibleComponent.Collect();
                }
                else
                {
                    Debug.Log($"[ItemInterativo] CollectibleItem NOT found on '{gameObject.name}', falling back to inventory add");
                    if (itemColetavel != null)
                    {
                        SistemaInventario.instancia?.AdicionarItem(itemColetavel, 1);
                        if (somColeta != null)
                            AudioSource.PlayClipAtPoint(somColeta, transform.position);
                        Destroy(gameObject); // DEVE ficar aqui, dentro do if itemColetavel != null
                    }
                    else
                    {
                        Debug.LogWarning("[ItemInterativo] itemColetavel null e CollectibleItem ausente — nada a fazer.");
                    }
                }
                break;

            case TipoItem.Especial:
                onInteragir?.Invoke();
                break;
        }
    }
}