using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class CaixadeFusiveis : MonoBehaviour
{
    [Header("Config de slots (defina o tamanho para 3 no Inspector)")]
    public FusivelItem[] slots; // defina o tamanho para 3 no Inspector e deixe os elementos null inicialmente

    [Header("Sequências válidas — editar no Inspector (cada entrada deve ter o mesmo tamanho de 'slots')")]
    public List<SerializableIntArray> sequenciasValidas = new List<SerializableIntArray>();

    [Header("Opções")]
    public bool tentarAutoFillAoAbrir = true;
    public bool removerDoInventarioAoColocar = true;

    [Header("Eventos (simples)")]
    public UnityEvent onPlaced;
    public UnityEvent onRemoved;
    public UnityEvent onSolved;              // o seu evento original (sem índice)

    [Header("Eventos (com índice da sequência)")]
    public UnityEventInt onSolvedWithIndex; // novo: invoca com o índice da sequência correta

    [System.Serializable]
    public class UnityEventInt : UnityEvent<int> { }

    private void Reset()
    {
        // ajuda inicial: cria 3 slots por padrão
        slots = new FusivelItem[3];
    }

    #region API pública
    public bool PlaceFuseAtSlot(int slotIndex, FusivelItem fuse)
    {
        if (slotIndex < 0 || slotIndex >= slots.Length) return false;
        if (fuse == null) return false;

        // coloca / troca
        FusivelItem previous = slots[slotIndex];
        slots[slotIndex] = fuse;

        if (removerDoInventarioAoColocar)
            SistemaInventario.instancia?.RemoverItem(fuse, 1);

        // Se havia um anterior, devolve ao inventario automaticamente
        if (previous != null)
            SistemaInventario.instancia?.AdicionarItem(previous, 1);

        onPlaced?.Invoke();
        CheckSolved();
        return true;
    }

    public bool RemoveFuseAtSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= slots.Length) return false;
        if (slots[slotIndex] == null) return false;

        FusivelItem f = slots[slotIndex];
        slots[slotIndex] = null;

        SistemaInventario.instancia?.AdicionarItem(f, 1);
        onRemoved?.Invoke();
        return true;
    }

    public int[] GetCurrentIDs()
    {
        int[] ids = new int[slots.Length];
        for (int i = 0; i < slots.Length; i++)
            ids[i] = slots[i] == null ? 0 : slots[i].fusivelID;
        return ids;
    }

    public void TryAutoFillFromInventory()
    {
        if (!tentarAutoFillAoAbrir) return;

        var itens = SistemaInventario.instancia?.GetItens();
        if (itens == null) return;

        bool anyPlaced = false;

        for (int slotIndex = 0; slotIndex < slots.Length; slotIndex++)
        {
            if (slots[slotIndex] != null) continue;

            var compativeis = GetSequenciasCompatíveisComEstadoAtual();
            bool placedThisSlot = false;

            foreach (var seq in compativeis)
            {
                int required = seq[slotIndex];
                if (required == 0) continue;

                foreach (var entrada in itens)
                {
                    if (entrada == null || entrada.item == null) continue;
                    if (entrada.item is FusivelItem f && f.fusivelID == required)
                    {
                        // coloca no slot e remove do inventário
                        slots[slotIndex] = f;
                        if (removerDoInventarioAoColocar)
                            SistemaInventario.instancia?.RemoverItem(f, 1);

                        anyPlaced = true;
                        placedThisSlot = true;
                        break;
                    }
                }
                if (placedThisSlot) break;
            }
        }

        if (anyPlaced) Debug.Log("[Caixa] Auto-fill colocou pelo menos 1 fusível.");
        else Debug.Log("[Caixa] Auto-fill não encontrou fusíveis compatíveis no inventário.");

        CheckSolved();
    }
    #endregion

    #region Sequência / validação
    private List<int[]> GetSequenciasCompatíveisComEstadoAtual()
    {
        var result = new List<int[]>();
        int[] current = GetCurrentIDs();

        foreach (var s in sequenciasValidas)
        {
            if (s == null) continue;
            int[] seq = s.ToArray();
            if (seq.Length != current.Length) continue;

            bool ok = true;
            for (int i = 0; i < current.Length; i++)
            {
                if (current[i] == 0) continue;
                if (seq[i] != current[i]) { ok = false; break; }
            }

            if (ok) result.Add(seq);
        }

        return result;
    }

    /// <summary>
    /// Verifica se o estado atual dos slots corresponde exatamente a alguma sequência válida.
    /// Se corresponder, invoca onSolved e onSolvedWithIndex(index).
    /// Retorna true se resolveu.
    /// </summary>
    public bool CheckSolved()
    {
        int[] current = GetCurrentIDs();

        for (int i = 0; i < sequenciasValidas.Count; i++)
        {
            var s = sequenciasValidas[i];
            if (s == null) continue;
            int[] seq = s.ToArray();
            if (seq.Length != current.Length) continue;

            bool igual = true;
            for (int j = 0; j < current.Length; j++)
            {
                if (seq[j] != current[j]) { igual = false; break; }
            }

            if (igual)
            {
                Debug.Log($"[Caixa] Sequência correta! Puzzle resolvido. indice={i}");

                // invoca eventos (mantendo compatibilidade com seu fluxo atual)
                onSolved?.Invoke();

                if (onSolvedWithIndex != null)
                    onSolvedWithIndex.Invoke(i);

                return true;
            }
        }

        return false;
    }
    #endregion

    // retorna índice do primeiro slot vazio ou -1
    public int FirstEmptySlot()
    {
        for (int i = 0; i < slots.Length; i++)
            if (slots[i] == null) return i;
        return -1;
    }

    // troca dois slots
    public bool SwapSlots(int a, int b)
    {
        if (a < 0 || a >= slots.Length || b < 0 || b >= slots.Length) return false;
        var tmp = slots[a];
        slots[a] = slots[b];
        slots[b] = tmp;
        onRemoved?.Invoke();
        onPlaced?.Invoke();
        CheckSolved();
        return true;
    }

    // define slot com fusivel (pode passar null para limpar)
    public bool SetSlot(int index, FusivelItem fuse, bool removeFromInventory = false)
    {
        if (index < 0 || index >= slots.Length) return false;
        slots[index] = fuse;
        if (removeFromInventory && fuse != null) SistemaInventario.instancia?.RemoverItem(fuse, 1);
        if (fuse == null) onRemoved?.Invoke(); else onPlaced?.Invoke();
        CheckSolved();
        return true;
    }
}
