using UnityEngine;

[CreateAssetMenu(menuName = "Itens/Fusível", fileName = "NewFusivel")]
public class FusivelItem : ItemSistema
{
    [Tooltip("ID do fusível — usado para comparar com as sequências do puzzle.")]
    public int fusivelID = 1;

    [Tooltip("Ícone opcional para UI.")]
    public Sprite icone;

    public override void Usar()
    {
        Debug.Log($"[FusivelItem] Usado: {nomeItem} (ID {fusivelID}). Comportamento padrão: nenhum.");
    }
}
