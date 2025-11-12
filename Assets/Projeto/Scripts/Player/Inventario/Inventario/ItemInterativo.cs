using UnityEngine;
using UnityEngine.Events;

public class ItemInterativo : MonoBehaviour
{
    public TipoItem tipo = TipoItem.Interativo;

    [TextArea] public string descricao;

    [Header("Áudio")]
    public AudioClip somColeta;

    public ItemSistema itemColetavel;

    public UnityEvent onInteragir;

    public void Interagir(MovimentaçãoPlayer jogador)
    {
        switch (tipo)
        {
            case TipoItem.Interativo:
                HUD_Interacao.instancia?.MostrarMensagem(descricao);
                break;

            case TipoItem.Coletavel:
                if (itemColetavel != null)
                {
                    SistemaInventario.instancia?.AdicionarItem(itemColetavel);
                    AudioSource.PlayClipAtPoint(somColeta, transform.position);
                    Destroy(gameObject);
                }
                break;

            case TipoItem.Especial:
                onInteragir?.Invoke();
                break;
        }
    }
}