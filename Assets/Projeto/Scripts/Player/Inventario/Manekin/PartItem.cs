using UnityEngine;

public enum PartType
{
    RightArm,
    LeftLeg
}

[CreateAssetMenu(fileName = "NewPartItem", menuName = "Inventario/PartItem")]
public class PartItem : ItemSistema
{
    [Header("Dados da peça")]
    public PartType partType;

    // Implementação obrigatória de Usar() (não vamos usar por enquanto)
    public override void Usar()
    {
        Debug.Log($"[PartItem] Usar() chamado em {nomeItem} (parte {partType}) — uso padrão: nenhum.");
    }
}
