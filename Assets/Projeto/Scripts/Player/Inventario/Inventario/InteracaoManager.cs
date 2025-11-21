using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class InteracaoManager : MonoBehaviour
{
    [Header("Player")]
    public Transform jogadorCamera;
    public float distanciaInteracao = 2f;
    public LayerMask camadaInteracao;
    public KeyCode teclaInteragir = KeyCode.E;
    [Range(0, 1)] public float precisaoOlhar = 0.97f;

    [Header("Popup Mundo 3D")]
    public GameObject prefabPopup;
    public float alturaPopup = 1.5f;
    public Sprite iconeInteracao;

    [Header("Contorno")]
    public Material materialContorno;
    public float pulsacaoMagnitude = 0.05f;
    public float pulsacaoVel = 3f;

    [Header("Desbloqueio Celular")]
    public string tagCelular = "Celular";

    [Header("Diário / Pickup")]
    public string tagDiary = "Diary";
    public string nomeItemDiary = "Diario";
    public ItemSistema diaryItem;

    private GameObject popupInstance;
    private TMP_Text popupTexto;
    private Image popupIcone;
    private Renderer objetoRend;
    private Material matOriginal;
    private ItemInterativo objetoInterativo;
    private bool mostrandoPopup = false;

    private GameObject jogadorGO;

    private void Start()
    {
        if (jogadorCamera == null && Camera.main != null)
            jogadorCamera = Camera.main.transform;

        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) jogadorGO = p;
    }

    private void Update()
    {
        DetectarInteracao();

        if (mostrandoPopup && Input.GetKeyDown(teclaInteragir))
        {
            Interagir();
        }
    }

    private void DetectarInteracao()
    {
        if (jogadorCamera == null) return;

        Ray ray = new Ray(jogadorCamera.position, jogadorCamera.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, distanciaInteracao, camadaInteracao))
        {
            // tenta obter ItemInterativo a partir do collider tocado (suporta múltiplos colliders por objeto)
            ItemInterativo item = hit.collider.GetComponentInParent<ItemInterativo>();
            if (item != null)
            {
                // garante que o ponto atingido esteja próximo do centro do olhar
                float dot = Vector3.Dot(jogadorCamera.forward.normalized, (hit.point - jogadorCamera.position).normalized);
                if (dot < precisaoOlhar)
                {
                    if (mostrandoPopup) RemoverPopup();
                    return;
                }

                objetoInterativo = item;
                if (!mostrandoPopup) CriarPopup(item, hit.collider.GetComponent<Renderer>(), hit.point, hit.normal);
                AtualizarPopup();
                PulsarContorno();
                return;
            }
        }

        // nada detectado -> remove popup se estava mostrando
        if (mostrandoPopup) RemoverPopup();
    }

    private void Interagir()
    {
        if (objetoInterativo == null) return;

        // pega referência do MovimentaçãoPlayer (fallbacks)
        MovimentaçãoPlayer jogador = null;
        if (jogadorGO == null && jogadorCamera != null)
        {
            var camGo = jogadorCamera.gameObject;
            jogador = camGo.GetComponent<MovimentaçãoPlayer>();
        }
        else if (jogadorGO != null)
        {
            jogador = jogadorGO.GetComponent<MovimentaçãoPlayer>();
        }

        // detectar celular/diário (para textos/efeitos)
        bool ehCelular = !string.IsNullOrEmpty(tagCelular) && objetoInterativo.gameObject.CompareTag(tagCelular);
        bool ehDiary = (!string.IsNullOrEmpty(tagDiary) && objetoInterativo.gameObject.CompareTag(tagDiary)) ||
                       (!string.IsNullOrEmpty(nomeItemDiary) && objetoInterativo.itemColetavel != null && objetoInterativo.itemColetavel.nomeItem == nomeItemDiary);

        // captura PageItem antes de possivelmente destruir o objeto
        PageItem paginaASerIntegrada = objetoInterativo.itemColetavel as PageItem;

        // celular
        if (ehCelular)
        {
            HUD_Interacao.instancia?.PegarCelular();
            HUD_Interacao.instancia?.MostrarMensagem("Finalmente, um celular... Talvez eu possa usá-lo para me orientar.");
        }

        // se for caixa de fusíveis — abre interação in-scene
        var box = objetoInterativo.GetComponentInParent<CaixadeFusiveis>();
        if (box != null)
        {
            var interactor = box.GetComponent<FuseboxInteractor>();
            if (interactor != null)
            {
                // prefira passar o GameObject do jogador (com MovimentaçãoPlayer) — fallback ao encontrar pela tag
                if (jogadorGO != null)
                    interactor.StartInteractionFromPlayer(jogadorGO);
                else
                {
                    var fallback = GameObject.FindGameObjectWithTag("Player");
                    if (fallback != null) interactor.StartInteractionFromPlayer(fallback);
                    else interactor.StartInteractionFromPlayer(null);
                }

                // fecha popup porque entramos no modo de interação in-scene
                RemoverPopup();
                return; // evita executar interação padrão sobre o mesmo objeto
            }
        }

        // se for manequim — delega toda lógica ao MannequinInteractor (ele lida com inventário/encaixe/coleta)
        var mannequin = objetoInterativo.GetComponentInParent<MannequinInteractor>();
        if (mannequin != null)
        {
            mannequin.Interact(jogador);
            RemoverPopup();
            return; // evita fluxo padrão
        }

        objetoInterativo.Interagir(jogador);

        var giver = objetoInterativo.GetComponentInParent<QuestGiver>();
        if (giver != null) giver.TryGive();

        // se era PageItem, integra ao HUD (independente do que a interação fez)
        if (paginaASerIntegrada != null)
        {
            if (HUD_Interacao.instancia != null)
            {
                HUD_Interacao.instancia.IntegrarPagina(paginaASerIntegrada);
                Debug.Log($"[InteracaoManager] Integrei página #{paginaASerIntegrada.numeroPagina}");
            }
            else
            {
                Debug.LogWarning("[InteracaoManager] HUD_Interacao.instancia == null — não integrou página.");
            }
        }

        // diário
        if (ehDiary)
        {
            TratarInteracaoDiary(objetoInterativo);
        }

        RemoverPopup();
    }

    private void TratarInteracaoDiary(ItemInterativo item)
    {
        if (item == null) return;

        if (diaryItem != null)
            SistemaInventario.instancia?.AdicionarItem(diaryItem, 1);

        HUD_Interacao.instancia?.PegarDiario();
        HUD_Interacao.instancia?.MostrarMensagem("Há anotações aqui. Algo pode me ajudar a entender o que aconteceu.");
        HUD_Interacao.instancia?.MostrarNotificacao("Diário coletado!", diaryItem != null ? diaryItem.iconeItem : null);

        try
        {
            if (item != null && item.gameObject != null)
                Destroy(item.gameObject);
        }
        catch { }
    }

    #region PopUp

    private void CriarPopup(ItemInterativo item, Renderer rend, Vector3 hitPoint, Vector3 hitNormal)
    {
        mostrandoPopup = true;
        objetoRend = rend;
        matOriginal = rend != null ? rend.material : null;

        // procura anchor customizado (porta, popup anchor, etc.)
        Transform customAnchor = null;
        DoorInteractable door = item.GetComponentInParent<DoorInteractable>();
        if (door != null && door.popupAnchor != null)
            customAnchor = door.popupAnchor;
        else
        {
            var itemAnchor = item.GetComponentInParent<PopupAnchor>();
            if (itemAnchor != null && itemAnchor.anchor != null)
                customAnchor = itemAnchor.anchor;
        }

        Vector3 worldPos;
        if (customAnchor != null)
        {
            worldPos = customAnchor.position + Vector3.up * 0.05f; // pequeno uplift
        }
        else
        {
            // desloca um pouco para fora pela normal do hit e um pouco para frente do objeto
            float outwardOffset = 0.25f;
            worldPos = hitPoint + hitNormal.normalized * outwardOffset + Vector3.up * alturaPopup;
        }

        popupInstance = Instantiate(prefabPopup, worldPos, Quaternion.identity);
        popupTexto = popupInstance.GetComponentInChildren<TMP_Text>();
        popupIcone = popupInstance.GetComponentInChildren<Image>();

        string narracao = "Existe algo aqui...";
        if (item != null) narracao = $"Pressione {teclaInteragir} para inspecionar ({item.tipo})";

        if (popupTexto != null) popupTexto.text = narracao;
        if (popupIcone != null)
        {
            popupIcone.sprite = iconeInteracao;
            popupIcone.enabled = iconeInteracao != null;
            popupIcone.preserveAspect = true;
        }

        // garantir que el popup fique legível (vira para camera)
        if (jogadorCamera != null)
        {
            popupInstance.transform.LookAt(jogadorCamera);
            popupInstance.transform.Rotate(0, 180f, 0);
        }
    }

    public void RemoverPopup()
    {
        mostrandoPopup = false;
        objetoInterativo = null;

        if (popupInstance != null) Destroy(popupInstance);
        popupInstance = null;
        popupTexto = null;
        popupIcone = null;

        if (objetoRend != null && matOriginal != null) objetoRend.material = matOriginal;
        objetoRend = null;
    }

    private void AtualizarPopup()
    {
        if (popupInstance == null || objetoInterativo == null) return;

        popupInstance.transform.position = GetTopWorldPosition(objetoInterativo.gameObject, alturaPopup);

        if (jogadorCamera != null)
        {
            popupInstance.transform.LookAt(jogadorCamera);
            popupInstance.transform.Rotate(0, 180f, 0);
        }
    }

    /// <summary>
    /// Determina posição ideal do popup:
    /// - se ItemInterativo tiver interactionPoint (ou popupOffset), usa;
    /// - senão calcula top bounds e aplica pequeno deslocamento para frente do objeto (reduz popups "dentro" da parede).
    /// </summary>
    private Vector3 GetTopWorldPosition(GameObject go, float offset)
    {
        ItemInterativo ii = go.GetComponent<ItemInterativo>();
        if (ii != null && ii.interactionPoint != null)
        {
            Vector3 world = ii.interactionPoint.position;
            if (ii.interactionPoint != null && ii.popupOffset != Vector3.zero)
                world += ii.interactionPoint.TransformVector(ii.popupOffset);
            return world;
        }

        Collider col = go.GetComponent<Collider>();
        if (col != null)
        {
            Bounds b = col.bounds;
            Vector3 top = b.center + Vector3.up * (b.extents.y + offset);

            Vector3 forward = go.transform.forward;
            float forwardOffset = Mathf.Max(0.15f, offset * 0.5f);
            Vector3 forwardShift = forward.normalized * forwardOffset;

            return top + forwardShift;
        }

        Renderer r = go.GetComponentInChildren<Renderer>();
        if (r != null)
        {
            Bounds b = r.bounds;
            Vector3 top = b.center + Vector3.up * (b.extents.y + offset);

            Vector3 forward = go.transform.forward;
            float forwardOffset = Mathf.Max(0.15f, offset * 0.5f);
            Vector3 forwardShift = forward.normalized * forwardOffset;

            return top + forwardShift;
        }

        Vector3 basePos = go.transform.position + Vector3.up * offset;
        basePos += go.transform.forward * 0.15f;
        return basePos;
    }

    #endregion

    private void PulsarContorno()
    {
        if (objetoRend == null || materialContorno == null) return;

        if (objetoInterativo != null)
        {
            Transform root = objetoInterativo.transform;
            if (root.CompareTag("SemOutline") || (root.root != null && root.root.CompareTag("SemOutline")))
                return;
        }

        float t = Mathf.Sin(Time.time * pulsacaoVel) * pulsacaoMagnitude + 0.5f;
        objetoRend.material.Lerp(matOriginal, materialContorno, t);
    }
}
