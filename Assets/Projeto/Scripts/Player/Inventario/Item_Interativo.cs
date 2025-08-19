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
    [Header("Tipo de Intera��o")]
    public TipoInteracao tipo = TipoInteracao.Observavel;

    [Header("Observ�vel")]
    [TextArea] public string descricao;

    [Header("Colet�vel")]
    public ItemSO itemColetavel;

    [Header("Especial")]
    public UnityEvent onInteragir;

    public void Interagir(Movimenta��oPlayer jogador)
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
