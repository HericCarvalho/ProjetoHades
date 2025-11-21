using UnityEngine;

[RequireComponent(typeof(Collider))]
public class BoardsInteractable : MonoBehaviour
{
    [Header("Configuração")]
    public PryBarItem requiredPryBarItem; // arraste aqui o asset do pé de cabra (PryBarItem)
    public bool consumePryBarOnUse = false; // true se quiser remover o pé de cabra do inventário ao usar

    [Header("Feedback")]
    public string msgHint = "Está tudo pregado com tábuas. Parece que precisa de algo para arrancá-las.";
    public string msgBreak = "Usei o pé de cabra para arrancar as tábuas — abriu passagem!";
    public AudioClip breakSound;
    public GameObject breakVFXPrefab; // opcional: partículas/poeira ao quebrar
    public Transform vfxSpawnPoint;    // onde spawnar o VFX (opcional)

    [Header("Comportamento de remoção")]
    public bool destroyOnBreak = true; // destrói o objeto inteiro
    public GameObject[] partsToDisable; // ou desativa meshes / colliders específicos

    private bool broken = false;

    // Chamado pelo InteracaoManager (passa o jogador se quiser)
    public void Interact(MovimentaçãoPlayer player)
    {
        if (broken)
        {
            // já quebradas — nada a fazer (poderia colocar msg diferente)
            HUD_Interacao.instancia?.MostrarMensagem("As tábuas já foram removidas.");
            return;
        }

        // verifica inventário
        if (!HasPryBarInInventory())
        {
            // mostra dica narrativa: pode ser "o personagem" falando
            HUD_Interacao.instancia?.MostrarMensagem(msgHint);
            return;
        }

        // tem pé de cabra: quebra as tábuas
        BreakBoards();
    }

    private bool HasPryBarInInventory()
    {
        if (requiredPryBarItem == null) return false;
        var itens = SistemaInventario.instancia?.GetItens();
        if (itens == null) return false;
        foreach (var e in itens)
        {
            if (e == null || e.item == null) continue;
            if (e.item == requiredPryBarItem) return true;
        }
        return false;
    }

    private void BreakBoards()
    {
        broken = true;

        // feedback sonoro
        if (breakSound != null)
            AudioSource.PlayClipAtPoint(breakSound, transform.position);

        // vfx
        if (breakVFXPrefab != null)
        {
            Transform spawn = vfxSpawnPoint != null ? vfxSpawnPoint : this.transform;
            Instantiate(breakVFXPrefab, spawn.position, Quaternion.identity);
        }

        // mensagem
        HUD_Interacao.instancia?.MostrarMensagem(msgBreak);

        // opcional: consumir o pé de cabra
        if (consumePryBarOnUse && requiredPryBarItem != null)
            SistemaInventario.instancia?.RemoverItem(requiredPryBarItem, 1);

        // desativar partes específicas (se setadas) ou destruir o objeto todo
        if (partsToDisable != null && partsToDisable.Length > 0)
        {
            foreach (var go in partsToDisable)
            {
                if (go == null) continue;
                // tenta desativar renderers e colliders
                go.SetActive(false);
            }
        }
        else if (destroyOnBreak)
        {
            Destroy(gameObject);
        }
        else
        {
            // como fallback, desativa o collider para impedir nova interação
            var col = GetComponent<Collider>();
            if (col != null) col.enabled = false;
        }
    }
}
