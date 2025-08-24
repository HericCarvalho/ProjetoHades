using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class EntradaInventario
{
    public ItemSistema item;
    public int quantidade;

    public EntradaInventario(ItemSistema item, int quantidade)
    {
        this.item = item;
        this.quantidade = quantidade;
    }
}

public class SistemaInventario : MonoBehaviour
{
    public static SistemaInventario instancia;

    [Header("UI do Inventário")]
    public GameObject painelInventario;
    public Transform conteudoInventario;
    public GameObject prefabSlot;

    [Header("Popup de Itens")]
    public GameObject popupMenu;
    public Button botaoUsarPopup;
    public Button botaoCancelarPopup;

    private bool inventarioAberto = false;
    private List<EntradaInventario> itensNoInventario = new List<EntradaInventario>();
    private SlotInventario slotSelecionado;

    void Awake()
    {
        if (instancia == null) instancia = this;
        else { Destroy(gameObject); return; }

        if (painelInventario != null) painelInventario.SetActive(false);
        if (popupMenu != null) popupMenu.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.I)) AlternarInventario();
    }

    public void AlternarInventario()
    {
        inventarioAberto = !inventarioAberto;

        if (painelInventario != null)
            painelInventario.SetActive(inventarioAberto);

        Cursor.lockState = inventarioAberto ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = inventarioAberto;

        if (inventarioAberto) AtualizarUI();
        else if (popupMenu != null) popupMenu.SetActive(false);
    }

    public void AdicionarItem(ItemSistema item, int quantidade = 1)
    {
        if (item == null || quantidade <= 0) return;

        // Procura entrada existente
        var entrada = itensNoInventario.Find(e => e.item == item);
        if (entrada != null) entrada.quantidade += quantidade;
        else itensNoInventario.Add(new EntradaInventario(item, quantidade));

        HUD_Interacao.instancia?.MostrarNotificacao($"Pegou {item.nomeItem}", item.iconeItem);
        AtualizarUI();
    }

    public void RemoverItem(ItemSistema item, int quantidade = 1)
    {
        if (item == null || quantidade <= 0) return;

        var entrada = itensNoInventario.Find(e => e.item == item);
        if (entrada != null)
        {
            entrada.quantidade -= quantidade;
            if (entrada.quantidade <= 0) itensNoInventario.Remove(entrada);
            AtualizarUI();
        }
    }

    private void AtualizarUI()
    {
        if (conteudoInventario == null || prefabSlot == null)
        {
            Debug.LogWarning("[SistemaInventario] conteudoInventario ou prefabSlot não atribuídos.");
            return;
        }

        // Limpa tudo
        for (int i = conteudoInventario.childCount - 1; i >= 0; i--)
            Destroy(conteudoInventario.GetChild(i).gameObject);

        // Recria slots
        foreach (var entrada in itensNoInventario)
        {
            var slotGO = Instantiate(prefabSlot, conteudoInventario);
            var slot = slotGO.GetComponent<SlotInventario>();
            if (slot == null)
            {
                Debug.LogError("[SistemaInventario] O prefab de slot não tem SlotInventario.");
                continue;
            }

            slot.ConfigurarSlot(entrada.item, entrada.quantidade);

            if (slot.botaoSlot != null)
            {
                slot.botaoSlot.onClick.RemoveAllListeners();
                slot.botaoSlot.onClick.AddListener(() => AbrirPopup(slot));
            }
        }
    }

    public void AbrirPopup(SlotInventario slot)
    {
        slotSelecionado = slot;
        if (popupMenu == null) return;

        popupMenu.SetActive(true);
        popupMenu.transform.position = Input.mousePosition;

        if (botaoUsarPopup != null)
        {
            botaoUsarPopup.onClick.RemoveAllListeners();
            botaoUsarPopup.onClick.AddListener(() =>
            {
                // Usa 1 unidade do item do slot selecionado
                var item = slotSelecionado?.GetItem();
                if (item != null)
                {
                    HUD_Interacao.instancia?.MostrarNotificacao($"Usou {item.nomeItem}", item.iconeItem);
                    RemoverItem(item, 1);
                }
                FecharPopup();
            });
        }

        if (botaoCancelarPopup != null)
        {
            botaoCancelarPopup.onClick.RemoveAllListeners();
            botaoCancelarPopup.onClick.AddListener(FecharPopup);
        }
    }

    private void FecharPopup()
    {
        if (popupMenu != null) popupMenu.SetActive(false);
        slotSelecionado = null;
    }

    public List<EntradaInventario> GetItens() => itensNoInventario;
}
