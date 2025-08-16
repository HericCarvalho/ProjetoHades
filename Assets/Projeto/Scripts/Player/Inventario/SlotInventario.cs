using UnityEngine;
using UnityEngine.UI;

public class SlotInventario : MonoBehaviour
{
    public Image icone;
    public void ConfigurarSlot(ItemSO item)
    {
        icone.sprite = item.iconeItem;
    }
}

