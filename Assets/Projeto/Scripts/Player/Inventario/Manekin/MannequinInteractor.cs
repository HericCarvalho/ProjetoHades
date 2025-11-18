using System.Linq;
using UnityEngine;

/// <summary>
/// Interactor para manequim (versão estendida):
/// - FindNearestMannequin: busca manequim mais próximo dentro do raio.
/// - TryInsertPartFromInventory: procura a peça no inventário e insere no manequim (consome do inventário).
/// - InsertPartDirect: insere sem mexer no inventário (útil para testes).
/// - Interact permanece responsável por tentar encaixar peças do inventário quando você interage.
/// </summary>
public class MannequinInteractor : MonoBehaviour
{
    [Header("Itens necessários (assets ItemSistema)")]
    public ItemSistema requiredRightArmItem;
    public ItemSistema requiredLeftLegItem;

    [Header("Visuais (prefabs)")]
    public GameObject rightArmPrefab;
    public GameObject leftLegPrefab;

    [Header("Attach points")]
    public Transform attachPointRightArm;
    public Transform attachPointLeftLeg;

    [Header("Chave (ItemSistema)")]
    public ItemSistema keyItem;
    [Header("Visual da chave (opcional)")]
    public GameObject keyVisualPrefab;
    public Transform keyVisualParent;

    [Header("Animator (boca)")]
    public Animator animator;
    public string openTrigger = "OpenMouth";

    [Header("Mensagens / Narrador")]
    public string msgMissing = "Algo está faltando aqui...";
    public string msgNeedRightArm = "Parece que falta o braço direito.";
    public string msgNeedLeftLeg = "Parece que falta a perna esquerda.";
    public string msgInsertedPart = "A peça se encaixa com um clique sutil.";
    public string msgMouthOpens = "A boca do manequim se abre revelando algo brilhante no interior...";
    public string msgTakeKey = "Peguei a chave!";

    // estado
    private bool rightArmAttached = false;
    private bool leftLegAttached = false;
    private bool mouthOpen = false;
    private bool keyCollected = false;

    // refs visuais
    private GameObject spawnedRightArm;
    private GameObject spawnedLeftLeg;
    private GameObject spawnedKeyVisual;

    // -------- Public API --------

    /// <summary>
    /// Busca o mannequim mais próximo de 'origin' dentro de 'radius'. Retorna null se nenhum.
    /// </summary>
    public static MannequinInteractor FindNearestMannequin(Vector3 origin, float radius)
    {
        var all = Object.FindObjectsOfType<MannequinInteractor>();
        MannequinInteractor best = null;
        float bestDist = float.MaxValue;
        float r2 = radius * radius;

        foreach (var m in all)
        {
            float d2 = (m.transform.position - origin).sqrMagnitude;
            if (d2 <= r2 && d2 < bestDist)
            {
                best = m;
                bestDist = d2;
            }
        }
        return best;
    }

    /// <summary>
    /// Procura no inventário por um PartItem correspondente ao partType (assume PartItem contém partType).
    /// Se achar, remove 1 do inventário, encaixa a peça e retorna true.
    /// </summary>
    public bool TryInsertPartFromInventory(PartType part)
    {
        var itens = SistemaInventario.instancia?.GetItens();
        if (itens == null) return false;

        foreach (var entrada in itens.ToArray()) // ToArray para evitar modificar durante iteração
        {
            if (entrada == null || entrada.item == null) continue;

            if (entrada.item is PartItem pi && pi.partType == part)
            {
                // remove do inventário e insere
                SistemaInventario.instancia.RemoverItem(pi, 1);
                InsertPartDirect(part);
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Insere a peça diretamente (não mexe no inventário).
    /// Retorna true se inseriu.
    /// </summary>
    public bool InsertPartDirect(PartType part)
    {
        if (part == PartType.RightArm)
        {
            if (rightArmAttached) return false;
            AttachRightArmVisual();
            rightArmAttached = true;
            HUD_Interacao.instancia?.MostrarMensagem(msgInsertedPart);
            CheckCompleteAndOpen();
            return true;
        }
        else if (part == PartType.LeftLeg)
        {
            if (leftLegAttached) return false;
            AttachLeftLegVisual();
            leftLegAttached = true;
            HUD_Interacao.instancia?.MostrarMensagem(msgInsertedPart);
            CheckCompleteAndOpen();
            return true;
        }

        return false;
    }

    // -------- Interaction entrypoint (mantive similar ao seu) --------

    /// <summary>
    /// Chamado pelo InteracaoManager quando o jogador interage com o manequim.
    /// Ele **tenta** encaixar peças do inventário (se existirem) — esse é o comportamento desejado.
    /// Se já estiver completo e a boca aberta, coleta a chave.
    /// </summary>
    public void Interact(MovimentaçãoPlayer player)
    {
        // primeira fala
        if (!rightArmAttached && !leftLegAttached && !mouthOpen)
        {
            HUD_Interacao.instancia?.MostrarMensagem(msgMissing);
        }

        // tenta encaixar RIGHT se disponível no inventário (prioriza encaixar ao interagir)
        if (!rightArmAttached && TryInsertPartFromInventory(PartType.RightArm))
        {
            return;
        }

        // tenta encaixar LEFT
        if (!leftLegAttached && TryInsertPartFromInventory(PartType.LeftLeg))
        {
            return;
        }

        // se já estiver completo e boca aberta e chave não coletada
        if (rightArmAttached && leftLegAttached && mouthOpen && !keyCollected)
        {
            CollectKey();
            return;
        }

        // se nada foi inserido, dá dicas narrativas
        if (!rightArmAttached || !leftLegAttached)
        {
            if (!rightArmAttached && !leftLegAttached)
                HUD_Interacao.instancia?.MostrarMensagem($"{msgNeedRightArm} {msgNeedLeftLeg}");
            else if (!rightArmAttached)
                HUD_Interacao.instancia?.MostrarMensagem(msgNeedRightArm);
            else
                HUD_Interacao.instancia?.MostrarMensagem(msgNeedLeftLeg);
        }
    }

    // -------- internos --------

    private void AttachRightArmVisual()
    {
        if (rightArmPrefab != null && attachPointRightArm != null)
        {
            spawnedRightArm = Instantiate(rightArmPrefab, attachPointRightArm, false);
            spawnedRightArm.transform.localPosition = Vector3.zero;
            spawnedRightArm.transform.localRotation = Quaternion.identity;
        }
    }

    private void AttachLeftLegVisual()
    {
        if (leftLegPrefab != null && attachPointLeftLeg != null)
        {
            spawnedLeftLeg = Instantiate(leftLegPrefab, attachPointLeftLeg, false);
            spawnedLeftLeg.transform.localPosition = Vector3.zero;
            spawnedLeftLeg.transform.localRotation = Quaternion.identity;
        }
    }

    private void CheckCompleteAndOpen()
    {
        if (rightArmAttached && leftLegAttached && !mouthOpen)
        {
            mouthOpen = true;
            if (animator != null && !string.IsNullOrEmpty(openTrigger))
                animator.SetTrigger(openTrigger);

            HUD_Interacao.instancia?.MostrarMensagem(msgMouthOpens);

            if (keyVisualPrefab != null && keyVisualParent != null)
            {
                spawnedKeyVisual = Instantiate(keyVisualPrefab, keyVisualParent, false);
                spawnedKeyVisual.transform.localPosition = Vector3.zero;
                spawnedKeyVisual.transform.localRotation = Quaternion.identity;
            }
        }
    }

    private void CollectKey()
    {
        if (keyCollected) return;
        keyCollected = true;

        if (keyItem != null)
            SistemaInventario.instancia?.AdicionarItem(keyItem, 1);

        if (spawnedKeyVisual != null)
        {
            Destroy(spawnedKeyVisual);
            spawnedKeyVisual = null;
        }

        HUD_Interacao.instancia?.MostrarMensagem(msgTakeKey);
    }

    // utilidades expostas
    public bool IsComplete() => rightArmAttached && leftLegAttached;
    public bool HasKeyBeenCollected() => keyCollected;
}
