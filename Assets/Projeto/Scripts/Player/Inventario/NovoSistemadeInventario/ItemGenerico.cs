using UnityEngine;

[CreateAssetMenu(fileName = "NovoItemGenerico", menuName = "Inventario/Item Gen�rico")]
public class ItemGenerico : ItemSistema
{
    public override void Usar()
    {
        Debug.Log($"Voc� usou o item: {nomeItem}!");
    }
}
