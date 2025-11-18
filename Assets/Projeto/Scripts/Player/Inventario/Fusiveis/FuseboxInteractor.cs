using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CaixadeFusiveis))]
public class FuseboxInteractor : MonoBehaviour
{
    [Header("Referencias")]
    public Transform cameraPoint;
    public Camera jogadorCamera;
    public string playerTag = "Player";
    public KeyCode teclaInteragir = KeyCode.E;
    public KeyCode teclaFechar = KeyCode.Escape;

    [Header("Controles de interação")]
    public KeyCode nextSlotKey = KeyCode.RightArrow;
    public KeyCode prevSlotKey = KeyCode.LeftArrow;
    public KeyCode pickPlaceKey = KeyCode.Space;
    public KeyCode swapKey = KeyCode.LeftShift;

    [Header("Transição câmera")]
    public float tempoTransicao = 0.4f;

    [Header("Opções")]
    public bool autoFillOnOpen = true;

    [Header("Visual (prefabs na cena)")]
    public FuseboxVisual visual; // arraste aqui o componente que instancia os prefabs

    // estado interno
    private CaixadeFusiveis caixa;
    private MovimentaçãoPlayer movPlayer;
    private Transform playerTransform;
    private bool isInteracting = false;
    private Vector3 initialCamPos;
    private Quaternion initialCamRot;
    private Transform originalCameraParent;
    private int selectedSlot = 0;
    private FusivelItem heldFuse = null;
    private bool heldFromInventory = false;
    private CursorLockMode prevCursorLock;
    private bool prevCursorVisible;

    private void Awake()
    {
        caixa = GetComponent<CaixadeFusiveis>();
        if (cameraPoint == null)
        {
            GameObject cp = new GameObject("CameraPoint");
            cp.transform.SetParent(transform, false);
            cp.transform.localPosition = Vector3.forward * 0.5f + Vector3.up * 0.6f;
            cp.transform.localRotation = Quaternion.identity;
            cameraPoint = cp.transform;
        }
    }

    private void Update()
    {
        if (!isInteracting && Input.GetKeyDown(teclaInteragir))
        {
            GameObject p = GameObject.FindGameObjectWithTag(playerTag);
            if (p != null)
            {
                float d = Vector3.Distance(p.transform.position, transform.position);
                if (d <= 3.0f)
                {
                    StartCoroutine(StartInteraction(p));
                }
                else
                {
                    // REMOVIDO: mensagem de "está longe" — não queremos mais mostrar nada aqui.
                    // Se quiser, pode adicionar um feedback visual leve no crosshair em vez de texto.
                }
            }
            else
            {
                StartCoroutine(StartInteraction(null));
            }
        }

        if (isInteracting)
        {
            if (Input.GetKeyDown(nextSlotKey))
                SelectSlot((selectedSlot + 1) % caixa.slots.Length);

            if (Input.GetKeyDown(prevSlotKey))
            {
                int len = caixa.slots.Length;
                SelectSlot((selectedSlot - 1 + len) % len);
            }

            if (Input.GetKeyDown(pickPlaceKey))
                HandlePickOrPlace();

            if (Input.GetKeyDown(teclaFechar))
                StopInteraction();
        }
    }

    public void StartInteractionFromPlayer(GameObject player)
    {
        if (isInteracting) return;
        StartCoroutine(StartInteraction(player));
    }

    private IEnumerator StartInteraction(GameObject playerObj)
    {
        isInteracting = true;

        if (jogadorCamera == null)
            jogadorCamera = Camera.main;
        if (jogadorCamera == null)
        {
            Debug.LogWarning("[FuseboxInteractor] Camera principal não encontrada.");
            isInteracting = false;
            yield break;
        }

        // salva estado da câmera
        initialCamPos = jogadorCamera.transform.position;
        initialCamRot = jogadorCamera.transform.rotation;
        originalCameraParent = jogadorCamera.transform.parent;

        // bloqueia controles do jogador
        if (playerObj != null)
        {
            movPlayer = playerObj.GetComponent<MovimentaçãoPlayer>();
            playerTransform = playerObj.transform;
            if (movPlayer != null)
            {
                movPlayer.SetCanMove(false);
                movPlayer.SetCanRotate(false);
                movPlayer.SetCameraControl(false);
            }
            var rb = playerObj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        // salva cursor
        prevCursorLock = Cursor.lockState;
        prevCursorVisible = Cursor.visible;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // transição da câmera (mantém roll 0 para evitar "pulos")
        float t = 0f;
        Vector3 startPos = jogadorCamera.transform.position;
        Quaternion startRot = jogadorCamera.transform.rotation;
        Vector3 targetPos = cameraPoint.position;
        Quaternion targetRot = Quaternion.Euler(cameraPoint.rotation.eulerAngles.x, cameraPoint.rotation.eulerAngles.y, 0f);

        while (t < tempoTransicao)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / tempoTransicao);
            jogadorCamera.transform.position = Vector3.Lerp(startPos, targetPos, k);
            Quaternion interp = Quaternion.Slerp(startRot, targetRot, k);
            jogadorCamera.transform.rotation = Quaternion.Euler(interp.eulerAngles.x, interp.eulerAngles.y, 0f);
            yield return null;
        }

        jogadorCamera.transform.position = targetPos;
        jogadorCamera.transform.rotation = targetRot;

        HUD_Interacao.instancia?.MostrarNotificacao("Você selecionou a caixa de fusíveis", null);
        HUD_Interacao.instancia?.MostrarMensagem("Use Espaço para pegar/colocar. ← → para trocar slot. Esc para sair.");

        selectedSlot = 0;
        HighlightSlot(selectedSlot);

        if (autoFillOnOpen)
        {
            caixa.TryAutoFillFromInventory();
            // garantir atualização visual após auto-fill
            UpdateVisuals();
        }
        else
        {
            UpdateVisuals();
        }
    }

    private void StopInteraction()
    {
        StartCoroutine(StopInteractionCoroutine());
    }

    private IEnumerator StopInteractionCoroutine()
    {
        if (heldFuse != null && heldFromInventory)
        {
            SistemaInventario.instancia?.AdicionarItem(heldFuse, 1);
            heldFuse = null;
            heldFromInventory = false;
        }

        if (jogadorCamera == null)
        {
            isInteracting = false;
            yield break;
        }

        float t = 0f;
        Vector3 startPos = jogadorCamera.transform.position;
        Quaternion startRot = jogadorCamera.transform.rotation;
        Vector3 targetPos = initialCamPos;
        Quaternion targetRot = initialCamRot;

        while (t < tempoTransicao)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / tempoTransicao);
            jogadorCamera.transform.position = Vector3.Lerp(startPos, targetPos, k);
            jogadorCamera.transform.rotation = Quaternion.Slerp(startRot, targetRot, k);
            yield return null;
        }

        jogadorCamera.transform.position = targetPos;
        jogadorCamera.transform.rotation = targetRot;

        if (movPlayer != null)
        {
            movPlayer.SetCanMove(true);
            movPlayer.SetCanRotate(true);
            movPlayer.SetCameraControl(true);
        }

        Cursor.lockState = prevCursorLock;
        Cursor.visible = prevCursorVisible;

        HUD_Interacao.instancia?.MostrarNotificacao("Interação finalizada.", null);
        isInteracting = false;
    }

    private void SelectSlot(int index)
    {
        selectedSlot = Mathf.Clamp(index, 0, caixa.slots.Length - 1);
        HighlightSlot(selectedSlot);
        HUD_Interacao.instancia?.MostrarMensagem($"Slot selecionado: {selectedSlot + 1}");
    }

    private void HighlightSlot(int index)
    {
        Debug.Log($"[FuseboxInteractor] Highlight slot {index}");
    }

    private void HandlePickOrPlace()
    {
        if (heldFuse == null)
        {
            FusivelItem f = caixa.slots[selectedSlot];
            if (f == null)
            {
                var itens = SistemaInventario.instancia?.GetItens();
                if (itens != null)
                {
                    foreach (var entrada in itens)
                    {
                        if (entrada.item is FusivelItem fi)
                        {
                            heldFuse = fi;
                            heldFromInventory = true;
                            SistemaInventario.instancia.RemoverItem(fi, 1);
                            HUD_Interacao.instancia?.MostrarMensagem($"Pegou do inventário: {fi.nomeItem}");
                            break;
                        }
                    }
                }

                if (heldFuse == null)
                    HUD_Interacao.instancia?.MostrarMensagem("Não há fusível aqui nem no inventário.");
                else
                    UpdateVisuals();

                return;
            }

            caixa.slots[selectedSlot] = null;
            heldFuse = f;
            heldFromInventory = false;
            HUD_Interacao.instancia?.MostrarMensagem($"Pegou fusível do slot {selectedSlot + 1}");
            UpdateVisuals();
            caixa.onRemoved?.Invoke();
        }
        else
        {
            FusivelItem existing = caixa.slots[selectedSlot];
            if (existing == null)
            {
                caixa.slots[selectedSlot] = heldFuse;
                if (heldFromInventory) heldFromInventory = false;
                HUD_Interacao.instancia?.MostrarMensagem($"Colocado {heldFuse.nomeItem} no slot {selectedSlot + 1}");
                caixa.onPlaced?.Invoke();
                heldFuse = null;
                UpdateVisuals();
                caixa.CheckSolved();
            }
            else
            {
                FusivelItem temp = existing;
                caixa.slots[selectedSlot] = heldFuse;
                heldFuse = temp;
                HUD_Interacao.instancia?.MostrarMensagem($"Swap: coloquei {caixa.slots[selectedSlot].nomeItem} e peguei {heldFuse.nomeItem}");
                UpdateVisuals();
                caixa.onPlaced?.Invoke();
                caixa.CheckSolved();
            }
        }
    }

    private void UpdateVisuals()
    {
        // atualiza text/log
        string s = "Estado slots: ";
        for (int i = 0; i < caixa.slots.Length; i++)
            s += (caixa.slots[i] == null ? "0" : caixa.slots[i].fusivelID.ToString()) + (i < caixa.slots.Length - 1 ? "," : "");
        s += " | Mão: " + (heldFuse == null ? "vazia" : heldFuse.fusivelID.ToString());
        Debug.Log("[FuseboxInteractor] " + s);

        // se houver componente visual atribuído, atualiza a cena (prefabs)
        if (visual != null)
        {
            visual.UpdateVisual(caixa.GetCurrentIDs());
        }
    }
}
