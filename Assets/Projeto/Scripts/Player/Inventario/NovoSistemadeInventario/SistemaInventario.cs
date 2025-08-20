using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
    private List<ItemSistema> itensNoInventario = new List<ItemSistema>();
    private SlotInventario slotSelecionado;

    void Awake()
    {
        if (instancia == null) instancia = this;
        else Destroy(gameObject);

        painelInventario.SetActive(false);
        if (popupMenu != null) popupMenu.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.I)) AlternarInventario();
    }

    public void AlternarInventario()
    {
        inventarioAberto = !inventarioAberto;
        painelInventario.SetActive(inventarioAberto);
        Cursor.lockState = inventarioAberto ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = inventarioAberto;

        if (inventarioAberto) AtualizarUI();
        else if (popupMenu != null) popupMenu.SetActive(false);
    }

    public void AdicionarItem(ItemSistema item)
    {
        itensNoInventario.Add(item);
        HUD_Interacao.instancia?.MostrarNotificacao($"Pegou {item.nomeItem}", item.iconeItem);
        if (inventarioAberto) AtualizarUI();
    }

    public void RemoverItem(ItemSistema item)
    {
        if (itensNoInventario.Contains(item))
        {
            itensNoInventario.Remove(item);
            HUD_Interacao.instancia?.MostrarNotificacao($"Usou {item.nomeItem}", item.iconeItem);
            AtualizarUI();
        }
    }

    private void AtualizarUI()
    {
        foreach (Transform t in conteudoInventario) Destroy(t.gameObject);

        foreach (ItemSistema item in itensNoInventario)
        {
            GameObject slotGO = Instantiate(prefabSlot, conteudoInventario);
            SlotInventario slot = slotGO.GetComponent<SlotInventario>();
            slot.ConfigurarSlot(item);
            slot.botaoSlot.onClick.RemoveAllListeners();
            slot.botaoSlot.onClick.AddListener(() => AbrirPopup(slot));
        }
    }

    public void AbrirPopup(SlotInventario slot)
    {
        slotSelecionado = slot;
        if (popupMenu != null)
        {
            popupMenu.SetActive(true);
            popupMenu.transform.position = Input.mousePosition;

            botaoUsarPopup.onClick.RemoveAllListeners();
            botaoCancelarPopup.onClick.RemoveAllListeners();

            botaoUsarPopup.onClick.AddListener(() =>
            {
                slotSelecionado?.UsarItem();
                FecharPopup();
            });
            botaoCancelarPopup.onClick.AddListener(() => FecharPopup());
        }
    }

    private void FecharPopup()
    {
        if (popupMenu != null) popupMenu.SetActive(false);
        slotSelecionado = null;
    }

    public List<ItemSistema> GetItens() => itensNoInventario;


}
