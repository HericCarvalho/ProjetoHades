using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class ComboEntry
{
    [Tooltip("IDs na ordem exata. Preencha com 3 valores separados (ex: 1,2,3). Use 0 para slot vazio se quiser combos parciais.")]
    public int[] ids = new int[3] { 1, 2, 3 };

    [Tooltip("Evento chamado quando a combinação é ativada (quando a caixa passa a conter exactly esses ids).")]
    public UnityEvent onActivate;

    [Tooltip("Evento chamado quando a combinação é desfeita (quando a caixa muda para outra combinação).")]
    public UnityEvent onDeactivate;

    // utilitário: retorna string chave
    public string Key()
    {
        return $"{ids[0]},{ids[1]},{ids[2]}";
    }
}

public class CaixadeFusiveis : MonoBehaviour
{
    [Header("Slots")]
    [Tooltip("Somente 3 slots. Cada posição guarda um FuseItem (ScriptableObject).")]
    public Fusiveis[] slots = new Fusiveis[3];

    [Header("Combinações")]
    [Tooltip("Liste aqui as combinações possíveis e os eventos a ligar/desligar.")]
    public List<ComboEntry> combos = new List<ComboEntry>();

    // chave da combinação atualmente ativa (ou null se nenhuma)
    private string activeComboKey = null;

    [Header("Feedback (opcional)")]
    public AudioSource audioSource;
    public AudioClip soundPlace;
    public AudioClip soundRemove;
    public AudioClip soundComboActivate;
    public AudioClip soundComboDeactivate;

    [Header("Evento: chamado sempre que o conteúdo da caixa muda (útil para UI)")]
    public UnityEvent onChanged;

    private void Awake()
    {
        // valida combos para chaves únicas (ajuda durante edição)
        var seen = new HashSet<string>();
        foreach (var c in combos)
        {
            if (c == null) continue;
            string k = c.Key();
            if (seen.Contains(k))
                Debug.LogWarning($"[FuseBox] Combinação duplicada encontrada: {k}", this);
            else
                seen.Add(k);
        }
        // avalia estado inicial (caso haja fusíveis já na cena)
        EvaluateCombination();
    }

    #region API pública para manipular slots (chamado por UI/Interação)
    /// <summary>
    /// Tenta colocar um fuse no slotIndex (0..2).
    /// NÃO remove do inventário automaticamente — quem chamar pode fazer isso.
    /// </summary>
    public bool PlaceFuseAtSlot(int slotIndex, Fusiveis fuse)
    {
        if (slotIndex < 0 || slotIndex >= slots.Length)
            return false;
        if (fuse == null)
            return false;

        slots[slotIndex] = fuse;
        if (audioSource != null && soundPlace != null) audioSource.PlayOneShot(soundPlace);

        Debug.Log($"[FuseBox] Fuse id={fuse.fuseId} colocado no slot {slotIndex}");
        EvaluateCombination();
        onChanged?.Invoke();
        return true;
    }

    /// <summary>Remove e retorna o fuse que estava no slot (ou null se vazio).</summary>
    public Fusiveis RemoveFuseAtSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= slots.Length) return null;
        var f = slots[slotIndex];
        slots[slotIndex] = null;
        if (f != null && audioSource != null && soundRemove != null) audioSource.PlayOneShot(soundRemove);

        Debug.Log($"[FuseBox] Fuse removido do slot {slotIndex} (id={(f != null ? f.fuseId.ToString() : "null")})");
        EvaluateCombination();
        onChanged?.Invoke();
        return f;
    }

    /// <summary>Troca dois slots (ordem) — útil para interface drag/drop ou botões 'swap'.</summary>
    public void SwapSlots(int a, int b)
    {
        if (a < 0 || a >= slots.Length || b < 0 || b >= slots.Length) return;
        var t = slots[a];
        slots[a] = slots[b];
        slots[b] = t;
        Debug.Log($"[FuseBox] Slots trocados {a} <-> {b}");
        EvaluateCombination();
        onChanged?.Invoke();
    }

    /// <summary>Retorna a combinação atual como string chave "id1,id2,id3". Usa 0 para slot vazio.</summary>
    public string GetCurrentKey()
    {
        int id0 = slots[0] != null ? slots[0].fuseId : 0;
        int id1 = slots[1] != null ? slots[1].fuseId : 0;
        int id2 = slots[2] != null ? slots[2].fuseId : 0;
        return $"{id0},{id1},{id2}";
    }
    #endregion

    #region lógica de combinação
    private void EvaluateCombination()
    {
        string currentKey = GetCurrentKey();

        // se já temos uma combinação ativa diferente -> desativar
        if (!string.IsNullOrEmpty(activeComboKey) && activeComboKey != currentKey)
        {
            var prev = FindComboByKey(activeComboKey);
            if (prev != null)
            {
                Debug.Log($"[FuseBox] Desativando combo anterior {activeComboKey}");
                prev.onDeactivate?.Invoke();
                if (audioSource != null && soundComboDeactivate != null) audioSource.PlayOneShot(soundComboDeactivate);
            }
            activeComboKey = null;
        }

        // procura combo que bate exatamente com a chave atual
        var match = FindComboByKey(currentKey);

        if (match != null)
        {
            // se ainda não estava ativa, ativa
            if (activeComboKey != currentKey)
            {
                Debug.Log($"[FuseBox] Ativando combo {currentKey}");
                match.onActivate?.Invoke();
                if (audioSource != null && soundComboActivate != null) audioSource.PlayOneShot(soundComboActivate);
            }
            activeComboKey = currentKey;
        }
        else
        {
            // se não bateu nenhuma combinação, garante activeComboKey null (já desativamos acima)
            activeComboKey = null;
            Debug.Log($"[FuseBox] Nenhuma combinação ativa ({currentKey})");
        }
    }

    private ComboEntry FindComboByKey(string key)
    {
        foreach (var c in combos)
        {
            if (c == null) continue;
            if (string.Equals(c.Key(), key, StringComparison.Ordinal))
                return c;
        }
        return null;
    }
    #endregion

#if UNITY_EDITOR
    // chama EvaluateCombination no inspector quando algo muda, útil durante setup
    private void OnValidate()
    {
        if (!Application.isPlaying) return;
        EvaluateCombination();
    }
#endif
}
