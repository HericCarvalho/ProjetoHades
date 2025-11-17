using System.Collections;
using UnityEngine;

/// <summary>
/// Controle da interação *no mundo* com a CaixadeFusiveis:
/// - aproxima a câmera para inspeção
/// - instancia visuais dos fusíveis sobre os 3 slots
/// - permite pressionar E olhando para um slot/visual para colocar/remover
/// - sincroniza com sistema de inventário
/// </summary>
public class VisualCaixadeFusiveis : MonoBehaviour
{
    [Header("Model / Box (logic)")]
    public CaixadeFusiveis fuseBox;               // referência à lógica da caixa (combos, slots[])
    [Header("Slots world positions")]
    public Transform[] slotTransforms = new Transform[3]; // transforms na cena onde os visuais aparecem (3)
    [Header("Prefab visual")]
    public GameObject prefabFuseVisual;           // prefab com FuseVisual + Collider + renderer
    [Header("Camera move")]
    public Transform cameraPointUI;               // onde a câmera deve parar _para interagir_
    public float tempoTransicaoCamera = 0.6f;
    public AnimationCurve curvaTransicao = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Player")]
    public MovimentaçãoPlayer playerController;   // se não preenchido, será buscado
    public KeyCode teclaInteragir = KeyCode.E;

    // runtime
    private Transform playerCamera;
    private bool aberto = false;
    private Transform cameraOriginalParent;
    private Vector3 cameraOriginalPos;
    private Quaternion cameraOriginalRot;
    private Coroutine camRoutine;
    private Vector3 cameraOriginalLocalPos;
    private Quaternion cameraOriginalLocalRot;
    private bool cameraHadParent = false;
    private bool cameraOriginalWasParented = false;

    // visuais instanciados (mesma ordem de slots)
    private GameObject[] instVisuals = new GameObject[3];

    private void Awake()
    {
        if (fuseBox == null) fuseBox = GetComponent<CaixadeFusiveis>();
        if (playerController == null) playerController = FindObjectOfType<MovimentaçãoPlayer>();
        playerCamera = playerController != null ? playerController.cameraReferencia : Camera.main?.transform;

        // cria placeholders de visual (instancia/oculta)
        for (int i = 0; i < 3; i++)
        {
            if (prefabFuseVisual != null && slotTransforms != null && slotTransforms.Length > i && slotTransforms[i] != null)
            {
                var go = Instantiate(prefabFuseVisual, slotTransforms[i].position, slotTransforms[i].rotation, slotTransforms[i]);
                go.name = $"FuseVisual_Slot{i}";
                instVisuals[i] = go;
                var fv = go.GetComponent<VisualFusiveis>();
                if (fv == null) fv = go.AddComponent<VisualFusiveis>();
                fv.Set(null, i, fuseBox);
            }
        }

        // se fuseBox muda por código, atualize visuais
        if (fuseBox != null)
            fuseBox.onChanged?.AddListener(UpdateVisuals);

        // inicializa visuais conforme estado atual
        UpdateVisuals();
    }
    private void OnDestroy()
    {
        if (fuseBox != null)
            fuseBox.onChanged?.RemoveAllListeners();
    }
    public void OpenInteraction()
    {
        if (aberto) return;
        aberto = true;

        // para qualquer rotina de câmera anterior
        if (camRoutine != null) { StopCoroutine(camRoutine); camRoutine = null; }

        // trava controles — sem movimento nem rotação
        if (playerController != null)
        {
            playerController.SetCanMove(false);
            playerController.SetCanRotate(false);
        }

        // cursor e UI (no seu caso sem HUD talvez apenas cursor)
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // guarda a câmera do player (tenta pegar se nulo)
        if (playerCamera == null && playerController != null)
            playerCamera = playerController.cameraReferencia;

        if (playerCamera == null)
        {
            Debug.LogWarning("[FuseBox] playerCamera não atribuída - OpenInteraction abortado.");
            aberto = false;
            // restaura controles só por segurança
            if (playerController != null)
            {
                playerController.SetCanMove(true);
                playerController.SetCanRotate(true);
            }
            return;
        }

        // Salva parent/pos/rot de forma robusta — só grava se ainda não estiver salvo (evita sobreescrever em aberturas repetidas)
        // Isso garante que, se por algum motivo OpenInteraction for chamado duas vezes, não perdemos a referência original.
        if (cameraOriginalParent == null)
        {
            cameraOriginalParent = playerCamera.parent;
            if (cameraOriginalParent != null)
            {
                // se estava parented, guarde local pos/rot para restaurar localmente
                cameraOriginalLocalPos = playerCamera.localPosition;
                cameraOriginalLocalRot = playerCamera.localRotation;
                cameraOriginalWasParented = true;
            }
            else
            {
                // se não estava parented, guardamos a transform em world space
                cameraOriginalPos = playerCamera.position;
                cameraOriginalRot = playerCamera.rotation;
                cameraOriginalWasParented = false;
            }
            Debug.Log($"[FuseBox] OpenInteraction: salvo parent={(cameraOriginalParent != null ? cameraOriginalParent.name : "null")} pos={playerCamera.position} rot={playerCamera.rotation.eulerAngles}");
        }

        // desparenta para transitar em world space (preserva posição atual)
        playerCamera.SetParent(null, true);

        // inicia transição -> move câmera para o ponto da caixa
        Vector3 targetPos = cameraPointUI != null ? cameraPointUI.position : playerCamera.position;
        Quaternion targetRot = cameraPointUI != null ? cameraPointUI.rotation : playerCamera.rotation;

        camRoutine = StartCoroutine(TransitionCamera(playerCamera, targetPos, targetRot, tempoTransicaoCamera, parentToTarget: true));
    }
    public void CloseInteraction()
    {
        if (!aberto) return;
        aberto = false;

        // se tiver uma rotina em andamento, pare e prepare o retorno (evita conflitos)
        if (camRoutine != null)
        {
            try { StopCoroutine(camRoutine); } catch { }
            camRoutine = null;
        }

        // esconder cursor (padrão)
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (playerCamera == null)
        {
            // nada a fazer, só restaurar controles
            if (playerController != null)
            {
                playerController.SetCanMove(true);
                playerController.SetCanRotate(true);
            }
            return;
        }

        // se a câmera estiver parented ao cameraPointUI, unparent para transição em world space
        if (cameraPointUI != null && playerCamera.parent == cameraPointUI)
            playerCamera.SetParent(null, true);

        // determina destino de retorno com base no que foi salvo
        Vector3 targetPos;
        Quaternion targetRot;
        if (cameraOriginalWasParented && cameraOriginalParent != null)
        {
            // se originalmente estava parented, reconstruímos o world transform a partir do parent + local
            targetPos = cameraOriginalParent.TransformPoint(cameraOriginalLocalPos);
            targetRot = cameraOriginalParent.rotation * cameraOriginalLocalRot;
        }
        else
        {
            // se originalmente era world-space, usamos os valores salvos (podem ser default se não salvos)
            targetPos = cameraOriginalPos;
            targetRot = cameraOriginalRot;
        }

        camRoutine = StartCoroutine(TransitionCamera(playerCamera, targetPos, targetRot, tempoTransicaoCamera, parentToTarget: false));
    }
    private IEnumerator PostCloseRestore()
    {
        // espera a coroutine de câmera terminar (se existir)
        if (camRoutine != null)
            yield return camRoutine;

        // garante restauração dos controles
        if (playerController != null)
        {
            playerController.enabled = true;    // reativa o componente do player
            playerController.SetCanMove(true);
            playerController.SetCanRotate(true);
        }

        camRoutine = null;
        yield break;
    }
    private IEnumerator TransitionCamera(Transform cam, Vector3 targetPos, Quaternion targetRot, float duration, bool parentToTarget)
    {
        if (cam == null)
            yield break;

        // se duration zero: aplica imediatamente e parenta/restaura controles
        if (duration <= 0f)
        {
            cam.position = targetPos;
            cam.rotation = targetRot;

            if (parentToTarget)
            {
                if (cameraPointUI != null) cam.SetParent(cameraPointUI, true);
            }
            else
            {
                // ao retornar, reparenta ao parent original (pode ser null)
                cam.SetParent(cameraOriginalParent, true);

                // se originalmente era parented, restaura local transform
                if (cameraOriginalWasParented && cameraOriginalParent != null)
                {
                    cam.localPosition = cameraOriginalLocalPos;
                    cam.localRotation = cameraOriginalLocalRot;
                }
                else
                {
                    cam.position = cameraOriginalPos;
                    cam.rotation = cameraOriginalRot;
                }

                // restaura controles
                if (playerController != null)
                {
                    playerController.SetCanMove(true);
                    playerController.SetCanRotate(true);
                }

                // limpa o saved parent para permitir novo OpenInteraction salvá-lo novamente
                cameraOriginalParent = null;
            }
            yield break;
        }

        float t = 0f;
        Vector3 startPos = cam.position;
        Quaternion startRot = cam.rotation;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = (curvaTransicao != null) ? curvaTransicao.Evaluate(Mathf.Clamp01(t / duration)) : Mathf.Clamp01(t / duration);
            cam.position = Vector3.Lerp(startPos, targetPos, k);
            cam.rotation = Quaternion.Slerp(startRot, targetRot, k);
            yield return null;
        }

        // garante valores finais
        cam.position = targetPos;
        cam.rotation = targetRot;

        if (parentToTarget)
        {
            if (cameraPointUI != null)
                cam.SetParent(cameraPointUI, true);
        }
        else
        {
            // retornando -> reparenta ao parent original (pode ser null)
            cam.SetParent(cameraOriginalParent, true);

            // se originalmente era parented, restaura local transform
            if (cameraOriginalWasParented && cameraOriginalParent != null)
            {
                cam.localPosition = cameraOriginalLocalPos;
                cam.localRotation = cameraOriginalLocalRot;
            }
            else
            {
                cam.position = cameraOriginalPos;
                cam.rotation = cameraOriginalRot;
            }

            // somente ao retornar totalmente, restaura controles
            if (playerController != null)
            {
                playerController.SetCanMove(true);
                playerController.SetCanRotate(true);
            }

            // limpa o saved parent para permitir novo OpenInteraction salvá-lo novamente
            cameraOriginalParent = null;
        }

        camRoutine = null;
    }

    private void Update()
    {
        if (!aberto) return;

        // interação por olhar + tecla E
        if (Input.GetKeyDown(teclaInteragir))
        {
            bool handled = false;

            Ray ray = new Ray(playerCamera.position, playerCamera.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, 3f))
            {
                // 1) prioriza FuseVisual (clicar no modelo do fusível)
                var fuseVis = hit.collider.GetComponentInParent<VisualFusiveis>();
                if (fuseVis != null && fuseVis.parentBox == fuseBox)
                {
                    // remover para inventário
                    int idx = fuseVis.slotIndex;
                    var removed = fuseBox.RemoveFuseAtSlot(idx);
                    if (removed != null)
                    {
                        SistemaInventario.instancia?.AdicionarItem(removed, 1);
                        HUD_Interacao.instancia?.MostrarNotificacao($"Fusível {removed.nomeItem} retornou ao inventário.", removed.iconeItem);
                    }
                    UpdateVisuals();
                    handled = true;
                }

                if (!handled)
                {
                    // 2) ou clicar no slotTransform (use um collider no slot empty ou collider do corpo da caixa)
                    for (int i = 0; i < slotTransforms.Length; i++)
                    {
                        if (slotTransforms[i] == null) continue;
                        float d = Vector3.Distance(hit.point, slotTransforms[i].position);
                        if (d < 0.35f) // tolerância
                        {
                            // se slot preenchido -> remove
                            if (fuseBox.slots[i] != null)
                            {
                                var removed = fuseBox.RemoveFuseAtSlot(i);
                                if (removed != null)
                                {
                                    SistemaInventario.instancia?.AdicionarItem(removed, 1);
                                    HUD_Interacao.instancia?.MostrarNotificacao($"Fusível {removed.nomeItem} retornou ao inventário.", removed.iconeItem);
                                }
                            }
                            else
                            {
                                // slot vazio -> tenta pegar primeiro fuse disponível no inventário e colocar aqui
                                var itens = SistemaInventario.instancia?.GetItens();
                                if (itens != null)
                                {
                                    EntradaInventario found = null;
                                    Fusiveis foundFuse = null;
                                    foreach (var e in itens)
                                    {
                                        if (e?.item is Fusiveis f && e.quantidade > 0)
                                        {
                                            found = e;
                                            foundFuse = f;
                                            break;
                                        }
                                    }
                                    if (foundFuse != null)
                                    {
                                        bool ok = fuseBox.PlaceFuseAtSlot(i, foundFuse);
                                        if (ok)
                                        {
                                            SistemaInventario.instancia?.RemoverItem(foundFuse, 1);
                                            HUD_Interacao.instancia?.MostrarNotificacao($"Fusível {foundFuse.nomeItem} colocado.", foundFuse.iconeItem);
                                        }
                                    }
                                    else
                                    {
                                        HUD_Interacao.instancia?.MostrarMensagem("Sem fusíveis no inventário.");
                                    }
                                }
                            }

                            UpdateVisuals();
                            handled = true;
                            break;
                        }
                    }
                }
            } // fim raycast

            // Se não tratou nada (ou seja: E apertado sem clicar em slot/visual) -> FECHAR interação (sair com E)
            if (!handled)
            {
                CloseInteraction();
            }
        }

        // fechar com Esc (opcional, mantém)
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CloseInteraction();
        }
    }
    private void TransferFromInventoryToBox()
    {
        if (fuseBox == null) return;
        var itens = SistemaInventario.instancia?.GetItens();
        if (itens == null) return;

        for (int slot = 0; slot < 3; slot++)
        {
            if (fuseBox.slots[slot] != null) continue;
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
                bool ok = fuseBox.PlaceFuseAtSlot(slot, foundFuse);
                if (ok)
                {
                    SistemaInventario.instancia?.RemoverItem(foundFuse, 1);
                    HUD_Interacao.instancia?.MostrarNotificacao($"Fusível {foundFuse.nomeItem} colocado.", foundFuse.iconeItem);
                }
            }
        }
    }
    public void UpdateVisuals()
    {
        if (fuseBox == null) return;
        for (int i = 0; i < 3; i++)
        {
            var data = fuseBox.slots[i]; // Fusiveis or null
            if (instVisuals != null && instVisuals.Length > i && instVisuals[i] != null)
            {
                var fv = instVisuals[i].GetComponent<VisualFusiveis>();
                if (fv != null) fv.Set(data, i, fuseBox);

                // posiciona exatamente no transform (caso risco de offset)
                if (slotTransforms != null && slotTransforms.Length > i && slotTransforms[i] != null)
                {
                    instVisuals[i].transform.SetParent(slotTransforms[i], true);
                    instVisuals[i].transform.localPosition = Vector3.zero;
                    instVisuals[i].transform.localRotation = Quaternion.identity;
                }

                // ativa/desativa visual
                instVisuals[i].SetActive(data != null);
            }
        }
    }
}