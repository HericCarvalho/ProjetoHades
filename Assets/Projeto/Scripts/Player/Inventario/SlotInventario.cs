using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SlotInventario : MonoBehaviour
{
    [Header("Referências UI")]
    public Image iconeItem;
    public TMP_Text textoNome;
    public TMP_Text textoQuantidade;
    public Button botao;

    [HideInInspector] public ItemSO itemAtual;
    [HideInInspector] public int quantidadeAtual = 1;

    // Configura o slot ao receber um item
    public void ConfigurarSlot(ItemSO item, int quantidade = 1)
    {
        itemAtual = item;
        quantidadeAtual = quantidade;

        if (iconeItem != null)
        {
            iconeItem.sprite = item.iconeItem;
            iconeItem.enabled = true;
        }

        if (textoNome != null)
            textoNome.text = item.nomeItem;

        AtualizarQuantidade();

        botao.onClick.RemoveAllListeners();
        botao.onClick.AddListener(() => Sistema_Inventario.instancia.AbrirPopup(this));
    }

    // Atualiza apenas a quantidade exibida
    public void AtualizarQuantidade()
    {
        if (textoQuantidade != null)
            textoQuantidade.text = (quantidadeAtual > 1) ? quantidadeAtual.ToString() : "";
    }

    // Usar o item
    public void UsarItem()
    {
        if (itemAtual == null) return;

        Debug.Log($"Usou {itemAtual.nomeItem}");
        quantidadeAtual--;

        if (quantidadeAtual <= 0)
        {
            Sistema_Inventario.instancia.RemoverItem(itemAtual);
            Destroy(gameObject);
        }
        else
        {
            AtualizarQuantidade();
        }
    }

    // Adiciona quantidade (para empilhamento)
    public int AdicionarQuantidade(int valor)
    {
        if (itemAtual == null) return valor;

        int max = itemAtual.quantidadeMax;
        int sobra = 0;

        quantidadeAtual += valor;

        if (quantidadeAtual > max)
        {
            sobra = quantidadeAtual - max;
            quantidadeAtual = max;
        }

        AtualizarQuantidade();
        return sobra;
    }
}
