using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SlotInventario : MonoBehaviour
{
    [Header("UI")]
    public Image iconeItem;
    public TMP_Text textoQuantidade;
    public TMP_Text textoNome;
    public Button botaoSlot;

    private ItemSistema item;
    private int quantidade;

    public void ConfigurarSlot(ItemSistema novoItem, int qtd)
    {
        item = novoItem;
        quantidade = Mathf.Max(0, qtd);

        if (iconeItem != null)
        {
            iconeItem.sprite = item != null ? item.iconeItem : null;
            iconeItem.enabled = (iconeItem.sprite != null);
            iconeItem.preserveAspect = true;
        }

        AtualizarTexto();
        gameObject.SetActive(true);
    }

    private void AtualizarTexto()
    {
        if (textoNome != null) textoNome.text = item != null ? item.nomeItem : "";
        if (textoQuantidade != null) textoQuantidade.text = quantidade > 1 ? quantidade.ToString() : "";
    }

    public ItemSistema GetItem() => item;
    public int GetQuantidade() => quantidade;

    // Chamado pelo SistemaInventario ao reconstruir a UI
    public void SetQuantidade(int novaQtd)
    {
        quantidade = Mathf.Max(0, novaQtd);
        AtualizarTexto();
    }

    // Segurança: tenta achar referências automaticamente no editor ao soltar o script no prefab
    private void Reset()
    {
        if (botaoSlot == null) botaoSlot = GetComponent<Button>();
        if (iconeItem == null) iconeItem = GetComponentInChildren<Image>(true);

        if (textoNome == null || textoQuantidade == null)
        {
            var tmps = GetComponentsInChildren<TMP_Text>(true);
            foreach (var t in tmps)
            {
                var n = t.name.ToLower();
                if (n.Contains("nome")) textoNome = t;
                else if (n.Contains("quant")) textoQuantidade = t;
            }
        }
    }
}
