using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Sistema_Inventario : MonoBehaviour
{
    public static Sistema_Inventario instancia;

    [Header("Camera")]
    [SerializeField] private MonoBehaviour cameraMovimento;

    [Header("UI do Inventário")]
    public GameObject painelInventario;
    public Transform conteudoInventario;
    public GameObject prefabSlot;

    [Header("Popup de Itens")]
    public GameObject popupMenu; // Mini-menu de opções
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
        if (popupMenu != null)
            popupMenu.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
            AlternarInventario();
    }

    public void AlternarInventario()
    {
        inventarioAberto = !inventarioAberto;
        painelInventario.SetActive(inventarioAberto);

        Cursor.lockState = inventarioAberto ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = inventarioAberto;

        if (cameraMovimento != null) cameraMovimento.enabled = !inventarioAberto;

        if (inventarioAberto) AtualizarUI();
        if (!inventarioAberto && popupMenu != null)
            popupMenu.SetActive(false);
    }

    public void AdicionarItem(ItemSO item)
    {
        itensNoInventario.Add(item);
        HUD_Interação.instancia.MostrarNotificacao($"Pegou {item.nomeItem}", item.iconeItem);
        if (inventarioAberto) AtualizarUI();
    }

    public void RemoverItem(ItemSO item)
    {
        if (itensNoInventario.Contains(item))
        {
            itensNoInventario.Remove(item);
            HUD_Interação.instancia.MostrarNotificacao($"Usou {item.nomeItem}", item.iconeItem);
        }

        if (inventarioAberto) AtualizarUI();
    }

    private void AtualizarUI()
    {
        foreach (Transform t in conteudoInventario)
            Destroy(t.gameObject);

        foreach (ItemSO item in itensNoInventario)
        {
            GameObject slotGO = Instantiate(prefabSlot, conteudoInventario);
            SlotInventario slot = slotGO.GetComponent<SlotInventario>();
            slot.ConfigurarSlot(item);
            slot.botao.onClick.RemoveAllListeners();
            slot.botao.onClick.AddListener(() => AbrirPopup(slot));
        }
    }

    public void AbrirPopup(SlotInventario slot)
    {
        slotSelecionado = slot;
        if (popupMenu != null)
        {
            popupMenu.SetActive(true);
            popupMenu.transform.position = Input.mousePosition; // Aparece próximo ao mouse

            botaoUsarPopup.onClick.RemoveAllListeners();
            botaoCancelarPopup.onClick.RemoveAllListeners();

            botaoUsarPopup.onClick.AddListener(() => {
                UsarItem(slotSelecionado);
                FecharPopup();
            });

            botaoCancelarPopup.onClick.AddListener(() => FecharPopup());
        }
    }

    private void FecharPopup()
    {
        if (popupMenu != null)
            popupMenu.SetActive(false);
        slotSelecionado = null;
    }

    private void UsarItem(SlotInventario slot)
    {
        if (slot == null || slot.itemAtual == null) return;

        ItemSO item = slot.itemAtual;

        switch (item.tipoItem)
        {
            case TipoItem.ColetavelConsumivel:
                slot.UsarItem(); // Reduz quantidade ou remove
                break;

            case TipoItem.Especial:
                Debug.Log($"Item especial usado: {item.nomeItem}");
                // Adicione lógica especial aqui
                break;

            case TipoItem.Interativo:
                Debug.Log($"Interagiu com: {item.nomeItem}");
                if (item.abreJanela && item.prefabMundo != null)
                    Instantiate(item.prefabMundo); // Mostra conteúdo do item
                break;
        }
    }

    public void FecharInventario()
    {
        inventarioAberto = false;
        painelInventario.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (cameraMovimento != null) cameraMovimento.enabled = true;

        if (popupMenu != null)
            popupMenu.SetActive(false);
    }

    public List<ItemSO> GetItens() => itensNoInventario;
}
