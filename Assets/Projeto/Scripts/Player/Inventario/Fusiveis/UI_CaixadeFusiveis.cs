using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UI_CaixadeFusiveis : MonoBehaviour
{
    [Header("Referências")]
    public CaixadeFusiveis targetBox;                       // caixa que este UI manipula (pode ser setada em runtime)
    public GameObject painelUI;                     // painel Canvas com slots/buttons
    public Image[] slotImages = new Image[3];       // imagens dos 3 slots na UI
    public Button[] btnRemoveSlot = new Button[3];  // botões para remover do slot (voltar ao inventário)
    public Button btnSwap01, btnSwap12;             // botões swap (opcionais)
    public Button btnFechar;                        // fechar UI

    [Header("Camera / Transição")]
    public Transform cameraPointUI;                 // ponto para onde a câmera deve ir (coloque child no FuseBox)
    public float tempoTransicaoCamera = 0.6f;
    public AnimationCurve curvaTransicao = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Player")]
    public MovimentaçãoPlayer playerController;     // arraste o MovimentaçãoPlayer (ou será buscado automaticamente)

    [Header("Visuais")]
    public Sprite spriteVazio;                      // sprite para slot vazio

    // estado runtime
    private bool aberto = false;
    private Transform cameraOriginalParent;
    private Vector3 cameraOriginalPos;
    private Quaternion cameraOriginalRot;
    private Transform playerCamera;                 // referência para camera do player (transform)
    private Coroutine camRoutine;

    private void Awake()
    {
        if (painelUI != null) painelUI.SetActive(false);

        // listeners básicos
        if (btnFechar != null)
        {
            btnFechar.onClick.RemoveAllListeners();
            btnFechar.onClick.AddListener(CloseUI);
        }
        if (btnSwap01 != null) { btnSwap01.onClick.RemoveAllListeners(); btnSwap01.onClick.AddListener(() => Swap(0, 1)); }
        if (btnSwap12 != null) { btnSwap12.onClick.RemoveAllListeners(); btnSwap12.onClick.AddListener(() => Swap(1, 2)); }

        for (int i = 0; i < 3; i++)
        {
            int idx = i;
            if (btnRemoveSlot != null && btnRemoveSlot.Length > i && btnRemoveSlot[i] != null)
            {
                btnRemoveSlot[i].onClick.RemoveAllListeners();
                btnRemoveSlot[i].onClick.AddListener(() => RemoveSlotToInventory(idx));
            }
            if (slotImages != null && slotImages.Length > i && slotImages[i] != null)
            {
                // optional: if Image has a Button component, use it
                var b = slotImages[i].GetComponent<Button>();
                if (b != null)
                {
                    b.onClick.RemoveAllListeners();
                    b.onClick.AddListener(() => OnSlotClicked(idx));
                }
            }
        }

        // tenta achar player automaticamente se não setado
        if (playerController == null)
            playerController = FindObjectOfType<MovimentaçãoPlayer>();

        if (playerController != null)
            playerCamera = playerController.cameraReferencia;
        else
            playerCamera = Camera.main?.transform;
    }

    private void OnDestroy()
    {
        // desinscreve listener da caixa anterior
        if (targetBox != null)
            targetBox.onChanged?.RemoveAllListeners();
    }

    // Abre a UI para uma FuseBox específica (chame do InteracaoManager)
    public void OpenFor(CaixadeFusiveis box)
    {
        if (box == null) return;
        if (aberto) return;

        targetBox = box;

        // inscricao para atualizacoes quando a caixa mudar
        targetBox.onChanged?.RemoveAllListeners();
        targetBox.onChanged?.AddListener(UpdateUI);

        // 1) transferir do inventário para caixa ao abrir (com prioridade: preenche slots vazios)
        TransferFromInventoryToBox();

        // 2) trava controles do player, ativa UI e anima câmera
        StartCoroutine(OpenRoutine());
    }

    private IEnumerator OpenRoutine()
    {
        aberto = true;
        if (playerController != null)
            playerController.SetCanMove(false);

        // habilita cursor e bloqueio rotacao (a gente permite rotacionar a camera enquanto dentro)
        if (playerController != null)
            playerController.SetCanRotate(true);

        // cursor visible
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (painelUI != null) painelUI.SetActive(true);

        // guarda transform atual da câmera para restaurar depois
        if (playerCamera != null)
        {
            cameraOriginalParent = playerCamera.parent;
            cameraOriginalPos = playerCamera.position;
            cameraOriginalRot = playerCamera.rotation;

            // transição para cameraPointUI no world space
            if (cameraPointUI != null)
            {
                if (camRoutine != null) StopCoroutine(camRoutine);
                camRoutine = StartCoroutine(TransitionCamera(playerCamera, cameraPointUI.position, cameraPointUI.rotation, tempoTransicaoCamera, true));
            }
        }

        UpdateUI();

        yield return null;
    }

    public void CloseUI()
    {
        if (!aberto) return;
        StartCoroutine(CloseRoutine());
    }

    private IEnumerator CloseRoutine()
    {
        // desfaz efeitos UI
        if (painelUI != null) painelUI.SetActive(false);

        // cursor escondido
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // transição de retorno da câmera
        if (playerCamera != null)
        {
            if (camRoutine != null) StopCoroutine(camRoutine);
            camRoutine = StartCoroutine(TransitionCamera(playerCamera, cameraOriginalPos, cameraOriginalRot, tempoTransicaoCamera, false));
            yield return camRoutine;
        }

        // restaura controles
        if (playerController != null)
        {
            playerController.SetCanMove(true);
            playerController.SetCanRotate(true);
        }

        aberto = false;

        // desinscrever listener (evita múltiplas assinaturas)
        if (targetBox != null)
            targetBox.onChanged?.RemoveAllListeners();

        targetBox = null;
    }

    // transição suave da camera (pos/rot). se parentToTarget true, parenta à cameraPointUI no final
    private IEnumerator TransitionCamera(Transform cam, Vector3 targetPos, Quaternion targetRot, float duration, bool parentToTarget)
    {
        if (cam == null)
            yield break;

        float t = 0f;
        Vector3 startPos = cam.position;
        Quaternion startRot = cam.rotation;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = curvaTransicao.Evaluate(Mathf.Clamp01(t / duration));
            cam.position = Vector3.Lerp(startPos, targetPos, k);
            cam.rotation = Quaternion.Slerp(startRot, targetRot, k);
            yield return null;
        }
        cam.position = targetPos;
        cam.rotation = targetRot;

        if (parentToTarget && cameraPointUI != null)
            cam.SetParent(cameraPointUI, true);
        else if (!parentToTarget)
            cam.SetParent(cameraOriginalParent, true);
    }

    // preenche os slots vazios com fusíveis presentes no inventário (Remove do inventário)
    private void TransferFromInventoryToBox()
    {
        if (targetBox == null) return;
        var itens = SistemaInventario.instancia?.GetItens();
        if (itens == null) return;

        // percorre slots vazios e busca fuse no inventário
        for (int slot = 0; slot < 3; slot++)
        {
            if (targetBox.slots[slot] != null) continue; // já tem
            // procura primeiro FuseItem no inventário
            EntradaInventario foundEntrada = null;
            Fusiveis foundFuse = null;
            foreach (var e in itens)
            {
                if (e?.item is Fusiveis f && e.quantidade > 0)
                {
                    foundEntrada = e;
                    foundFuse = f;
                    break;
                }
            }
            if (foundFuse != null)
            {
                // coloca no slot e remove 1 do inventário
                bool ok = targetBox.PlaceFuseAtSlot(slot, foundFuse);
                if (ok)
                {
                    SistemaInventario.instancia?.RemoverItem(foundFuse, 1);
                    HUD_Interacao.instancia?.MostrarNotificacao($"Fusível {foundFuse.nomeItem} colocado.", foundFuse.iconeItem);
                }
            }
            else
            {
                // nenhum fuse restante no inventario
                break;
            }
        }
        UpdateUI();
    }

    // Remove slot para inventário (volta um ao inventário)
    private void RemoveSlotToInventory(int slotIndex)
    {
        if (targetBox == null) return;
        var removed = targetBox.RemoveFuseAtSlot(slotIndex);
        if (removed != null)
        {
            SistemaInventario.instancia?.AdicionarItem(removed, 1);
            HUD_Interacao.instancia?.MostrarNotificacao($"Fusível {removed.nomeItem} retornou ao inventário.", removed.iconeItem);
        }
        UpdateUI();
    }

    // troca dois slots na caixa
    public void Swap(int a, int b)
    {
        if (targetBox == null) return;
        targetBox.SwapSlots(a, b);
        UpdateUI();
    }

    // clique direto no slot (você pode usar para remover ou selecionar)
    private void OnSlotClicked(int idx)
    {
        // por enquanto: remove para o inventario
        RemoveSlotToInventory(idx);
    }

    // atualiza UI visual dos slots
    public void UpdateUI()
    {
        // segurança
        if (slotImages == null || slotImages.Length < 3) return;

        if (targetBox == null)
        {
            // limpa visuais se não há caixa
            for (int i = 0; i < 3; i++)
            {
                slotImages[i].sprite = spriteVazio;
                slotImages[i].enabled = spriteVazio != null;
                if (btnRemoveSlot != null && btnRemoveSlot.Length > i && btnRemoveSlot[i] != null)
                    btnRemoveSlot[i].interactable = false;
            }
            return;
        }

        for (int i = 0; i < 3; i++)
        {
            var f = targetBox.slots[i];
            if (slotImages[i] != null)
            {
                if (f != null && f.iconeItem != null)
                {
                    slotImages[i].sprite = f.iconeItem;
                    slotImages[i].enabled = true;
                }
                else
                {
                    slotImages[i].sprite = spriteVazio;
                    slotImages[i].enabled = spriteVazio != null;
                }
            }

            if (btnRemoveSlot != null && btnRemoveSlot.Length > i && btnRemoveSlot[i] != null)
            {
                btnRemoveSlot[i].interactable = (f != null);
            }
        }

        // atualiza swaps (se quiser bloquear swaps com slots vazios)
        if (btnSwap01 != null) btnSwap01.interactable = (targetBox.slots[0] != null || targetBox.slots[1] != null);
        if (btnSwap12 != null) btnSwap12.interactable = (targetBox.slots[1] != null || targetBox.slots[2] != null);
    }
}
