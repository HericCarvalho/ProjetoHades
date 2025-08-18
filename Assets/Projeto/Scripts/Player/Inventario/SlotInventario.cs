using UnityEngine;
using UnityEngine.UI;

public class SlotInventario : MonoBehaviour
{
    public Image iconeItem;
    public Text textoQuantidade;
    public Text textoNome;
    public Button botaoSlot;

    public ItemSO itemAtual;
    private int quantidadeAtual = 1;
    private Sistema_Inventario sistema;

    public void ConfigurarSlot(ItemSO item, int quantidade, Sistema_Inventario sistemaRef)
    {
        itemAtual = item;
        quantidadeAtual = Mathf.Max(1, quantidade);
        sistema = sistemaRef;

        if (iconeItem != null)
        {
            iconeItem.sprite = item?.iconeItem;
            iconeItem.enabled = iconeItem.sprite != null;
            iconeItem.preserveAspect = true;
        }

        if (textoNome != null)
            textoNome.text = item?.nomeItem ?? "";

        if (textoQuantidade != null)
            textoQuantidade.text = quantidadeAtual > 1 ? quantidadeAtual.ToString() : "";

        if (botaoSlot != null)
        {
            botaoSlot.onClick.RemoveAllListeners();
            botaoSlot.onClick.AddListener(() => sistema?.AbrirPopup(this));
        }
    }

    public void UsarItem()
    {
        if (itemAtual == null) return;
        Debug.Log($"Usou {itemAtual.nomeItem}");
        sistema?.RemoverQuantidade(itemAtual, 1);
    }
}
