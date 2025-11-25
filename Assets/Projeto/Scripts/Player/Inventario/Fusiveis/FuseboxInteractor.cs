using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CaixadeFusiveis))]
public class FuseboxInteractor : MonoBehaviour
{
    [System.Serializable]
    public class SequenceAction
    {
        [Header("Zonas/Luzes/Corredores")]
        public NoiseZone[] noiseZones;
        public CorridorTrigger[] corridorTriggers;
        public LightController[] lightControllers;
        public float lightsTargetIntensity = 1f;
        public float lightsRampDuration = 1f;

        [Header("HUD / SFX")]
        public bool showSolvedMessage = true;
        [TextArea] public string solvedMessage = "As luzes acendem.";
        public AudioClip solvedSfx;

        [Header("Quest (opcional)")]
        public QuestSO questToAffect;       // a QuestSO correspondente a esta sequência
        public string questObjectiveId;     // id do objetivo (ex: "place_fuse" ou "solve_sequence")
        public bool addQuestIfMissing = true; // se true, adiciona a quest ao resolver (caso ainda não exista)
        public bool completeDirectly = false; // se true, chama CompleteQuest(QuestSO) em vez de MarkObjective

        [Header("Opções")]
        public bool deactivateNoiseGameObjects = true; // se true, desativa o gameobject da NoiseZone após Unlock()
    }

    [Header("Referencias gerais")]
    public Transform cameraPoint;
    public Camera jogadorCamera;
    public string playerTag = "Player";
    public KeyCode teclaInteragir = KeyCode.E;
    public KeyCode teclaFechar = KeyCode.Escape;

    [Header("Controles")]
    public KeyCode nextSlotKey = KeyCode.RightArrow;
    public KeyCode prevSlotKey = KeyCode.LeftArrow;
    public KeyCode pickPlaceKey = KeyCode.Space;

    [Header("Transição câmera")]
    public float tempoTransicao = 0.4f;

    [Header("Opções")]
    public bool autoFillOnOpen = true;

    [Header("Visual")]
    public FuseboxVisual visual;

    [Header("Ações por sequência (ordem deve seguir CaixadeFusiveis.sequenciasValidas)")]
    public List<SequenceAction> sequenceActions = new List<SequenceAction>();

    [Header("SFX fallback")]
    public AudioSource sfxSource;
    public AudioClip genericSolvedSfx;

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

    private void Reset()
    {
        // ajuda inicial no editor
        if (caixa == null) caixa = GetComponent<CaixadeFusiveis>();
        if (cameraPoint == null)
        {
            GameObject cp = new GameObject("CameraPoint");
            cp.transform.SetParent(transform, false);
            cp.transform.localPosition = Vector3.forward * 0.5f + Vector3.up * 0.6f;
            cp.transform.localRotation = Quaternion.identity;
            cameraPoint = cp.transform;
        }
    }

    private void Awake()
    {
        caixa = GetComponent<CaixadeFusiveis>();
        if (caixa == null)
            Debug.LogError("[FuseboxInteractor] CaixadeFusiveis não encontrada no GameObject.");

        if (cameraPoint == null)
        {
            GameObject cp = new GameObject("CameraPoint");
            cp.transform.SetParent(transform, false);
            cp.transform.localPosition = Vector3.forward * 0.5f + Vector3.up * 0.6f;
            cp.transform.localRotation = Quaternion.identity;
            cameraPoint = cp.transform;
        }

        // Registra listeners com segurança
        if (caixa != null)
        {
            // onSolved (legacy sem índice) - mapear para index 0 por compatibilidade
            caixa.onSolved.AddListener(OnFuseboxSolvedLegacy);

            // onSolvedWithIndex pode ser null se não definido no CaixadeFusiveis; usamos ?.
            caixa.onSolvedWithIndex?.AddListener(OnFuseboxSolvedWithIndex);
        }
    }

    private void OnDestroy()
    {
        if (caixa != null)
        {
            caixa.onSolved.RemoveListener(OnFuseboxSolvedLegacy);
            caixa.onSolvedWithIndex?.RemoveListener(OnFuseboxSolvedWithIndex);
        }
    }

    private void Update()
    {
        if (caixa == null) return;

        if (!isInteracting && Input.GetKeyDown(teclaInteragir))
        {
            GameObject p = GameObject.FindGameObjectWithTag(playerTag);
            if (p != null)
            {
                float d = Vector3.Distance(p.transform.position, transform.position);
                if (d <= 3.0f) StartCoroutine(StartInteraction(p));
                else { /* opcional: feedback */ }
            }
            else StartCoroutine(StartInteraction(null));
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

    public void StartInteractionFromPlayer(GameObject player) { if (isInteracting) return; StartCoroutine(StartInteraction(player)); }

    private IEnumerator StartInteraction(GameObject playerObj)
    {
        isInteracting = true;
        if (jogadorCamera == null) jogadorCamera = Camera.main;
        if (jogadorCamera == null) { Debug.LogWarning("[FuseboxInteractor] Camera principal não encontrada."); isInteracting = false; yield break; }

        initialCamPos = jogadorCamera.transform.position;
        initialCamRot = jogadorCamera.transform.rotation;
        originalCameraParent = jogadorCamera.transform.parent;

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
            if (rb != null) { rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }
        }

        prevCursorLock = Cursor.lockState;
        prevCursorVisible = Cursor.visible;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // transição locomotion -> cameraPoint
        float t = 0f;
        Vector3 startPos = jogadorCamera.transform.position;
        Quaternion startRot = jogadorCamera.transform.rotation;
        Vector3 targetPos = cameraPoint != null ? cameraPoint.position : transform.position + transform.forward * 0.5f + Vector3.up * 0.6f;
        Quaternion targetRot = cameraPoint != null ? Quaternion.Euler(cameraPoint.rotation.eulerAngles.x, cameraPoint.rotation.eulerAngles.y, 0f) : Quaternion.identity;

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

        if (autoFillOnOpen) { caixa.TryAutoFillFromInventory(); UpdateVisuals(); } else UpdateVisuals();
    }

    private void StopInteraction() { StartCoroutine(StopInteractionCoroutine()); }

    private IEnumerator StopInteractionCoroutine()
    {
        if (heldFuse != null && heldFromInventory)
        {
            SistemaInventario.instancia?.AdicionarItem(heldFuse, 1);
            heldFuse = null;
            heldFromInventory = false;
        }

        if (jogadorCamera == null) { isInteracting = false; yield break; }

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
        if (caixa == null || caixa.slots == null || caixa.slots.Length == 0) return;
        selectedSlot = Mathf.Clamp(index, 0, caixa.slots.Length - 1);
        HighlightSlot(selectedSlot);
        HUD_Interacao.instancia?.MostrarMensagem($"Slot selecionado: {selectedSlot + 1}");
    }

    private void HighlightSlot(int index) { Debug.Log($"[FuseboxInteractor] Highlight slot {index}"); }

    private void HandlePickOrPlace()
    {
        if (caixa == null) return;

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

                if (heldFuse == null) HUD_Interacao.instancia?.MostrarMensagem("Não há fusível aqui nem no inventário.");
                else UpdateVisuals();

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
        if (caixa == null) return;

        string s = "Estado slots: ";
        for (int i = 0; i < caixa.slots.Length; i++) s += (caixa.slots[i] == null ? "0" : caixa.slots[i].fusivelID.ToString()) + (i < caixa.slots.Length - 1 ? "," : "");
        s += " | Mão: " + (heldFuse == null ? "vazia" : heldFuse.fusivelID.ToString());
        Debug.Log("[FuseboxInteractor] " + s);

        if (visual != null) visual.UpdateVisual(caixa.GetCurrentIDs());
    }

    // Handlers de solved
    private void OnFuseboxSolvedLegacy() { Debug.Log("[FuseboxInteractor] CaixadeFusiveis (legacy) resolveu. Aplicando ações index=0."); ApplySequenceActions(0); }
    private void OnFuseboxSolvedWithIndex(int seqIndex) { Debug.Log($"[FuseboxInteractor] CaixadeFusiveis solved index={seqIndex}"); ApplySequenceActions(seqIndex); }

    private void ApplySequenceActions(int seqIndex)
    {
        if (sequenceActions == null || sequenceActions.Count == 0) { Debug.LogWarning("[FuseboxInteractor] sequenceActions vazio."); return; }
        if (seqIndex < 0 || seqIndex >= sequenceActions.Count) { Debug.LogWarning($"[FuseboxInteractor] seqIndex {seqIndex} fora do range."); return; }

        var action = sequenceActions[seqIndex];

        // Noise zones
        if (action.noiseZones != null)
        {
            foreach (var nz in action.noiseZones)
            {
                if (nz == null) continue;
                nz.Unlock();
                if (action.deactivateNoiseGameObjects)
                {
                    try { nz.gameObject.SetActive(false); }
                    catch { }
                }
            }
        }

        // Corridor triggers
        if (action.corridorTriggers != null)
        {
            foreach (var ct in action.corridorTriggers)
            {
                if (ct == null) continue;
                ct.UnlockCorridor();
            }
        }

        // Light controllers
        /*if (action.lightControllers != null)
        {
            foreach (var lc in action.lightControllers)
            {
                if (lc == null) continue;
                lc.UnlockLights(action.lightsTargetIntensity, action.lightsRampDuration);
            }
        }*/

        // HUD / SFX
        if (action.showSolvedMessage && HUD_Interacao.instancia != null) HUD_Interacao.instancia.MostrarNotificacao(action.solvedMessage, null);
        AudioClip toPlay = action.solvedSfx != null ? action.solvedSfx : genericSolvedSfx;
        if (toPlay != null)
        {
            if (sfxSource != null) sfxSource.PlayOneShot(toPlay);
            else AudioSource.PlayClipAtPoint(toPlay, transform.position);
        }

        // --- QUEST LOGIC ---
        if (action.questToAffect != null && QuestManager.Instance != null)
        {
            if (action.addQuestIfMissing && !QuestManager.Instance.HasQuest(action.questToAffect))
            {
                var e = QuestManager.Instance.AddQuest(action.questToAffect);
                if (e == null)
                {
                    // fallback: forçar add (remove+add) se existir problema com lookup
                    var forced = QuestManager.Instance.AddQuest_Force(action.questToAffect);
                    Debug.Log($"[FuseboxInteractor] AddQuest fallback forçado: {(forced != null)}");
                }
                else Debug.Log($"[FuseboxInteractor] Quest adicionada: {action.questToAffect.name}");
            }

            if (action.completeDirectly)
            {
                bool ok = QuestManager.Instance.CompleteQuest(action.questToAffect);
                Debug.Log($"[FuseboxInteractor] CompleteQuest chamado para '{action.questToAffect.name}' -> {ok}");
            }
            else if (!string.IsNullOrEmpty(action.questObjectiveId))
            {
                bool ok = QuestManager.Instance.MarkObjective(action.questToAffect, action.questObjectiveId, 1);
                Debug.Log($"[FuseboxInteractor] MarkObjective chamado para '{action.questToAffect.name}' obj='{action.questObjectiveId}' -> {ok}");
            }
        }
    }
}
