using UnityEngine;

[CreateAssetMenu(fileName = "NovoItem", menuName = "Inventario/Item")]
public class ItemSO : ScriptableObject
{
    public string nomeItem;
    public Sprite iconeItem; 
   
    [TextArea] public string descricaoItem;
}
