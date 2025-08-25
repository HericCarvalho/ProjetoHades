using UnityEngine;
using UnityEngine.Events;

public enum TipoItem
{
    Coletavel,
    Especial,
    Interativo
}

[CreateAssetMenu(fileName = "NovoItem", menuName = "Inventario/ItemSO")]
public abstract class ItemSistema : ScriptableObject
{
    [Header("Dados do Item")]
    public string nomeItem;
    public Sprite iconeItem;
    public bool abreJanela;
    public GameObject prefabMundo;
    [TextArea] public string descricao;

    // Método polimórfico: cada item define o que faz ao ser usado
    public abstract void Usar();
}
