using UnityEngine;

[CreateAssetMenu(fileName = "NovoFuse", menuName = "Puzzle/FuseItem")]
public class Fusiveis : ItemSistema
{
    [Header("Identificador do Fusível")]
    [Tooltip("ID único (pequeno) usado para formar combinações. Ex: 1,2,3")]
    public int fuseId = 1;

    public override void Usar()
    {
        Debug.Log($"Usou FuseItem id={fuseId} nome={nomeItem}");
    }
}