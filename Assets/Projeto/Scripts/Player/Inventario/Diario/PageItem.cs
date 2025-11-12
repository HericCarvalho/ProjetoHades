using UnityEngine;

[CreateAssetMenu(fileName = "NovaPagina", menuName = "Diary/PageItem")]
public class PageItem : ItemSistema
{
    [Header("Dados da Página")]
    public int numeroPagina = 1;
    [TextArea(3, 8)] public string textoPagina;
    public Sprite imagemPagina;

    public override void Usar()
    {
        Debug.Log($"Usou página #{numeroPagina}: {nomeItem}");
    }
}
