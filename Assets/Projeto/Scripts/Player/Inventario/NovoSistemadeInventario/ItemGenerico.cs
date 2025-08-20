using UnityEngine;

[CreateAssetMenu(fileName = "NovoItemGenerico", menuName = "Inventario/Item Genérico")]
public class ItemGenerico : ItemSistema
{
    public override void Usar()
    {
        Debug.Log($"Você usou o item: {nomeItem}!");
    }
}
