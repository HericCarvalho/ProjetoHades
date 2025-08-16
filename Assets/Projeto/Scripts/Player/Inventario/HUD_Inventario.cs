using System.Collections.Generic;
using UnityEngine;

public class HUD_Inventario : MonoBehaviour
{
    public Transform conteudoInventario;
    public GameObject prefabSlot;

    public void AtualizarInventario(List<ItemSO> itens)
    {
        foreach (Transform t in conteudoInventario)
            Destroy(t.gameObject);

        foreach (ItemSO item in itens)
        {
            GameObject slot = Instantiate(prefabSlot, conteudoInventario);
            slot.GetComponent<SlotInventario>().ConfigurarSlot(item);
        }
    }
}
