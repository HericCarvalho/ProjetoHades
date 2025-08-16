using System.Collections.Generic;
using UnityEngine;

public class Inventario : MonoBehaviour
{
    public static Inventario instancia;
    public List<ItemSO> itens = new List<ItemSO>();
    public HUD_Inventario uiInventario;

    void Awake()
    {
        if (instancia == null) instancia = this;
    }

    public void AdicionarItem(ItemSO item)
    {
        itens.Add(item);
        uiInventario.AtualizarInventario(itens);
        HUD_Interação.instancia?.MostrarNotificacao($"Adicionado: {item.nomeItem}", item.iconeItem);
    }

    public void RemoverItem(ItemSO item)
    {
        if (itens.Contains(item))
        {
            itens.Remove(item);
            uiInventario.AtualizarInventario(itens);
            HUD_Interação.instancia?.MostrarNotificacao($"Removido: {item.nomeItem}", item.iconeItem);
        }
    }
}
