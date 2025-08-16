using System.Collections.Generic;
using UnityEngine;

public class Sistema_Inventario : MonoBehaviour
{
    public static Sistema_Inventario instancia;   // Singleton para acesso global

    [Header("UI do Invent�rio")]
    public GameObject painelInventario;           // Painel principal do invent�rio
    public Transform conteudoInventario;          // Onde os slots ser�o instanciados
    public GameObject prefabSlot;                 // Prefab de cada slot

    private bool inventarioAberto = false;        // Estado do invent�rio
    private List<ItemSO> itensNoInventario = new List<ItemSO>(); // Lista interna de itens

    void Awake()
    {
        // Singleton
        if (instancia == null) instancia = this;
        else Destroy(gameObject);

        // Painel come�a fechado
        painelInventario.SetActive(false);
    }

    void Update()
    {
        // Tecla para abrir/fechar invent�rio
        if (Input.GetKeyDown(KeyCode.I))
            AlternarInventario();
    }

    // Alterna o estado do invent�rio
    public void AlternarInventario()
    {
        inventarioAberto = !inventarioAberto;
        painelInventario.SetActive(inventarioAberto);

        // Controla cursor
        Cursor.lockState = inventarioAberto ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = inventarioAberto;

        // Atualiza slots sempre que abrir
        if (inventarioAberto)
            AtualizarUI();
    }

    // Adiciona um item e atualiza a UI
    public void AdicionarItem(ItemSO item)
    {
        itensNoInventario.Add(item);
        HUD_Intera��o.instancia.MostrarNotificacao($"Pegou {item.nomeItem}", item.iconeItem); // exibe notifica��o
        if (inventarioAberto)
            AtualizarUI();
    }

    // Remove um item e atualiza a UI
    public void RemoverItem(ItemSO item)
    {
        itensNoInventario.Remove(item);
        if (inventarioAberto)
            AtualizarUI();
    }

    // Atualiza visualmente os slots do invent�rio
    private void AtualizarUI()
    {
        // Limpa slots antigos
        foreach (Transform t in conteudoInventario)
            Destroy(t.gameObject);

        // Cria slots novos
        foreach (ItemSO item in itensNoInventario)
        {
            GameObject slot = Instantiate(prefabSlot, conteudoInventario);
            slot.GetComponent<SlotInventario>().ConfigurarSlot(item);
        }
    }

    // Fecha o invent�rio por script
    public void FecharInventario()
    {
        inventarioAberto = false;
        painelInventario.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Para obter a lista de itens de outros scripts
    public List<ItemSO> GetItens() => itensNoInventario;
}
