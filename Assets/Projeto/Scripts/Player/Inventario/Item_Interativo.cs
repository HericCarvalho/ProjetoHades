using UnityEngine;
using UnityEngine.Events;

public enum TipoInteracao
{
    Observavel,
    Coletavel,
    Especial
}

[CreateAssetMenu(fileName = "NovoItem", menuName = "Inventario/ItemSO")]
public class ItemSO : ScriptableObject
{
    public string nomeItem;
    public Sprite iconeItem;
    public bool abreJanela;
    public GameObject prefabMundo;
}

public class Item_Interativo : MonoBehaviour
{
    [Header("Tipo de Interação")]
    public TipoInteracao tipo = TipoInteracao.Observavel;

    [Header("Observável")]
    [TextArea] public string descricao;

    [Header("Coletável")]
    public ItemSO itemColetavel;

    [Header("Especial")]
    public UnityEvent onInteragir;

    public void Interagir(MovimentaçãoPlayer jogador)
    {
        switch (tipo)
        {
            case TipoInteracao.Observavel:
                if (HUD_Interacao.instancia != null)
                    HUD_Interacao.instancia.MostrarMensagem(descricao);
                break;

            case TipoInteracao.Coletavel:
                if (jogador != null && itemColetavel != null)
                {
                    Sistema_Inventario.instancia.AdicionarItem(itemColetavel);
                    Destroy(gameObject);
                }
                break;

            case TipoInteracao.Especial:
                onInteragir?.Invoke();
                break;
        }
    }
}
