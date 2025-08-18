using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Sistema_Inventario : MonoBehaviour
{
    public static Sistema_Inventario instancia;

    [Header("Camera")]
    [SerializeField] private MonoBehaviour cameraMovimento;

    [Header("UI Inventário")]
    public GameObject painelInventario;
    public Transform conteudoInventario;
    public GameObject prefabSlot;

    [Header("Popup de Itens")]
    public GameObject popupMenu;
    public Button botaoUsarPopup;
    public Button botaoCancelarPopup;

    private bool inventarioAberto = false;
    private List<ItemSO> itensNoInventario = new List<ItemSO>();
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
        if (cameraMovimento != null) cameraMovimento.enabled = !inventarioAberto;
        if (inventarioAberto) AtualizarUI();
        else if (popupMenu != null) popupMenu.SetActive(false);
    }

    public void AdicionarItem(ItemSO item)
    {
        itensNoInventario.Add(item);
        HUD_Interacao.instancia?.MostrarNotificacao($"Pegou {item.nomeItem}", item.iconeItem);
        if (inventarioAberto) AtualizarUI();
    }

    public void RemoverQuantidade(ItemSO item, int quantidade)
    {
        if (item == null || quantidade <= 0) return;
        if (!itensNoInventario.Contains(item)) return;

        for (int i = 0; i < quantidade && itensNoInventario.Contains(item); i++)
            itensNoInventario.Remove(item);

        HUD_Interacao.instancia?.MostrarNotificacao($"Removeu {item.nomeItem}", item.iconeItem);
        if (inventarioAberto) AtualizarUI();
    }

    private void AtualizarUI()
    {
        foreach (Transform t in conteudoInventario) Destroy(t.gameObject);

        foreach (ItemSO item in itensNoInventario)
        {
            GameObject slotGO = Instantiate(prefabSlot, conteudoInventario);
            SlotInventario slot = slotGO.GetComponent<SlotInventario>();
            slot.ConfigurarSlot(item, 1, this);
        }
    }

    public void AbrirPopup(SlotInventario slot)
    {
        slotSelecionado = slot;
        if (popupMenu == null) return;

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

    public void FecharPopup()
    {
        if (popupMenu != null) popupMenu.SetActive(false);
        slotSelecionado = null;
    }
}
