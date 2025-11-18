using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class DoorInteractable : MonoBehaviour
{
    [Header("Trancamento / Chave")]
    public bool locked = true;
    public ItemSistema requiredKey;
    public bool consumeKeyOnUse = true;

    [Header("Animator")]
    public Animator animator;
    public string openTrigger = "OpenTrigger";
    public string closeTrigger = "CloseTrigger";

    [Header("Auto-close (segundos)")]
    public float autoCloseDelay = 5f;
    [Range(0.05f, 2f)]
    public float closeSpeedMultiplier = 0.5f;
    public float closeAnimationDuration = 1f;

    [Header("Notificações")]
    public UnityEvent onOpened;
    public UnityEvent onClosed;

    [Header("Popup / Interação")]
    public Transform popupAnchor;

    private bool isOpen = false;
    private Coroutine autoCloseCoroutine;
    private float prevAnimatorSpeed = 1f;

    public void Interact(MovimentaçãoPlayer player = null)
    {
        // Se fechada e requer chave
        if (locked)
        {
            if (requiredKey != null)
            {
                // busca no inventário se existe a chave (mesma técnica usada em outros scripts)
                var itens = SistemaInventario.instancia?.GetItens();
                bool found = false;
                if (itens != null)
                {
                    foreach (var entrada in itens)
                    {
                        if (entrada == null || entrada.item == null) continue;
                        // comparar por referência ao asset ou por nome
                        if (entrada.item == requiredKey || entrada.item.nomeItem == requiredKey.nomeItem)
                        {
                            found = true;
                            break;
                        }
                    }
                }

                if (!found)
                {
                    // Narrador-style message quando não tem chave
                    HUD_Interacao.instancia?.MostrarMensagem("Está trancada! parece que preciso de uma chave específica para abri-la.");
                    return;
                }

                // se achou a chave: consome (opcional) e destranca
                if (consumeKeyOnUse)
                {
                    SistemaInventario.instancia?.RemoverItem(requiredKey, 1);
                    HUD_Interacao.instancia?.MostrarMensagem("A chave encaixa com um clique seco. A fechadura cedeu.");
                }
                else
                {
                    HUD_Interacao.instancia?.MostrarMensagem("A chave destrancou a porta.");
                }

                locked = false;
                // prossegue para abrir
                OpenDoor();
                return;
            }
            else
            {
                // porta trancada sem chave especificada
                HUD_Interacao.instancia?.MostrarMensagem("Está trancada. Preciso de algo para abrir isso.");
                return;
            }
        }

        // se já destrancada, abre/fecha normalmente ao interagir
        if (!isOpen)
            OpenDoor();
        else
            CloseDoorImmediate();
    }
    private void OpenDoor()
    {
        if (animator != null)
        {
            // garante animator a speed padrão
            prevAnimatorSpeed = animator.speed;
            animator.speed = 1f;
            animator.ResetTrigger(closeTrigger);
            animator.SetTrigger(openTrigger);
        }

        isOpen = true;
        onOpened?.Invoke();

        // cancel existing autoclose then schedule a new one (se necessário)
        if (autoCloseCoroutine != null)
        {
            StopCoroutine(autoCloseCoroutine);
            autoCloseCoroutine = null;
        }
        if (autoCloseDelay > 0f)
            autoCloseCoroutine = StartCoroutine(AutoCloseCoroutine());
    }
    private IEnumerator AutoCloseCoroutine()
    {
        yield return new WaitForSeconds(autoCloseDelay);

        // faz fechamento mais lento (multiplicador < 1)
        if (animator != null)
        {
            prevAnimatorSpeed = animator.speed;
            animator.speed = closeSpeedMultiplier;
            animator.ResetTrigger(openTrigger);
            animator.SetTrigger(closeTrigger);
        }

        // espera duração ajustada da animação (divide por multiplier para manter proporcional)
        float wait = Mathf.Max(0.01f, closeAnimationDuration / Mathf.Max(0.01f, closeSpeedMultiplier));
        yield return new WaitForSeconds(wait);

        // restaura speed do animator
        if (animator != null)
            animator.speed = prevAnimatorSpeed;

        isOpen = false;
        autoCloseCoroutine = null;
        onClosed?.Invoke();

        // Mensagem do narrador quando se fecha automaticamente (opcional)
        HUD_Interacao.instancia?.MostrarNotificacao("A porta se fecha sozinha, rangendo devagar.", null);
    }
    private void CloseDoorImmediate()
    {
        // fecha sem a desaceleração (chamada manual pelo jogador)
        if (animator != null)
        {
            animator.speed = 1f;
            animator.ResetTrigger(openTrigger);
            animator.SetTrigger(closeTrigger);
        }

        if (autoCloseCoroutine != null)
        {
            StopCoroutine(autoCloseCoroutine);
            autoCloseCoroutine = null;
        }

        isOpen = false;
        onClosed?.Invoke();
    }

    public void ForceUnlock()
    {
        locked = false;
        HUD_Interacao.instancia?.MostrarMensagem("A tranca foi forçada — posso abrir a porta agora.");
    }

#if UNITY_EDITOR
    [ContextMenu("Test Open")]
    private void EditorTestOpen() => OpenDoor();
    [ContextMenu("Test Close")]
    private void EditorTestClose() => CloseDoorImmediate();
#endif
}
