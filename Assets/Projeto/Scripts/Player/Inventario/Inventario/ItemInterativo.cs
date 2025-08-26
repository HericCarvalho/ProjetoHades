using UnityEngine;
using UnityEngine.Events;

public class ItemInterativo : MonoBehaviour
{
    // use o enum certo:
    public TipoItem tipo = TipoItem.Interativo;

    [TextArea] public string descricao;

    // use o ScriptableObject novo
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
                    Destroy(gameObject);
                }
                break;

            case TipoItem.Especial:
                onInteragir?.Invoke();
                break;
        }
    }
}