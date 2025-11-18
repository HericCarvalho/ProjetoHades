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

    // cache do jogador (GameObject) — tenta encontrar no Start se possível
    private GameObject jogadorGO;

    private void Start()
    {
        if (jogadorCamera == null && Camera.main != null)
            jogadorCamera = Camera.main.transform;
        // tenta achar player pela tag (opcional)
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) jogadorGO = p;
    }

    void Update()
    {
        DetectarInteracao();

        // Só processa input de interação se houver um objeto com popup visível
        if (mostrandoPopup && Input.GetKeyDown(teclaInteragir))
        {
            Interagir();
        }
    }

    void DetectarInteracao()
    {
        if (jogadorCamera == null) return;

        Ray ray = new Ray(jogadorCamera.position, jogadorCamera.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, distanciaInteracao, camadaInteracao))
        {
            ItemInterativo item = hit.collider.GetComponent<ItemInterativo>();
            if (item != null)
            {
                // garante que o objeto esteja suficientemente no centro da mira
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

        if (mostrandoPopup) RemoverPopup();
    }

    void Interagir()
    {
        if (objetoInterativo == null) return;

        // pega referência do jogador uma vez (fallback para jogadorGO ou a camera)
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

        // 1) detecta se é celular (pela tag)
        bool ehCelular = false;
        if (!string.IsNullOrEmpty(tagCelular) && objetoInterativo.gameObject.CompareTag(tagCelular))
            ehCelular = true;

        // 2) detecta se é diário (pela tag ou pelo nome do item coletável)
        bool ehDiary = false;
        if (!string.IsNullOrEmpty(tagDiary) && objetoInterativo.gameObject.CompareTag(tagDiary))
            ehDiary = true;
        else if (!ehDiary && !string.IsNullOrEmpty(nomeItemDiary) && objetoInterativo.itemColetavel != null)
        {
            if (objetoInterativo.itemColetavel.nomeItem == nomeItemDiary)
                ehDiary = true;
        }

        // 3) captura a PageItem (se houver) ANTES de executar a interação que pode destruir o objeto
        PageItem paginaASerIntegrada = objetoInterativo.itemColetavel as PageItem;

        // 4) se for celular, faz a lógica de desbloqueio (mantive seu texto narrativo)
        if (ehCelular)
        {
            HUD_Interacao.instancia?.PegarCelular();
            HUD_Interacao.instancia?.MostrarMensagem("Finalmente, um celular... Talvez eu possa usá-lo para me orientar.");
        }

        // 5) se o objeto tem um FuseboxInteractor, abra a interação em cena (camera se aproxima, trava jogador)
        FuseboxInteractor fuse = objetoInterativo.GetComponentInParent<FuseboxInteractor>();
        // Se o objeto interativo pertence a uma caixa de fusíveis, abrir interação in-scene
        var box = objetoInterativo.GetComponentInParent<CaixadeFusiveis>();
        if (box != null)
        {
            var interactor = box.GetComponent<FuseboxInteractor>();
            if (interactor != null)
            {
                // em vez de passar jogadorCamera.gameObject, passe jogadorGO (o objeto com MovimentaçãoPlayer)
                if (jogadorGO != null)
                    interactor.StartInteractionFromPlayer(jogadorGO);
                else
                    interactor.StartInteractionFromPlayer(GameObject.FindGameObjectWithTag("Player")); // fallback

            }
        }


        // 6) executa a interação normal (pode adicionar ao inventário / destruir o objeto, etc.)
        objetoInterativo.Interagir(jogador);

        // 7) se era uma PageItem, avisa o HUD para integrar (garante integração mesmo se o objeto de cena foi destruído)
        if (paginaASerIntegrada != null)
        {
            if (HUD_Interacao.instancia != null)
            {
                HUD_Interacao.instancia.IntegrarPagina(paginaASerIntegrada);
                Debug.Log($"[InteracaoManager] Pediu integração da página #{paginaASerIntegrada.numeroPagina}");
            }
            else
            {
                Debug.LogWarning("[InteracaoManager] HUD_Interacao.instancia == null — não integrou página.");
            }
        }

        // 8) se era diário, executa o tratamento específico (mantive seu fluxo original)
        if (ehDiary)
        {
            TratarInteracaoDiary(objetoInterativo);
        }

        RemoverPopup();
    }


    private void InteragirComCelular(ItemInterativo item, MovimentaçãoPlayer jogador, bool abrirImediatamente = true)
    {
        if (item == null) return;

        HUD_Interacao.instancia?.PegarCelular();
        HUD_Interacao.instancia?.MostrarMensagem("Você conseguiu um celular! Use-o com cuidado — a lanterna e o bloco de notas podem salvar sua pele.");
    }

    #region InteraçãoDiario

    private void TratarInteracaoDiary(ItemInterativo item)
    {
        if (item == null) return;

        if (diaryItem != null)
        {
            SistemaInventario.instancia?.AdicionarItem(diaryItem, 1);
        }

        HUD_Interacao.instancia?.PegarDiario();

        HUD_Interacao.instancia?.MostrarMensagem("Há anotações aqui. Algo pode me ajudar a entender o que aconteceu.");
        HUD_Interacao.instancia?.MostrarNotificacao("Diário coletado!", diaryItem != null ? diaryItem.iconeItem : null);

        try
        {
            if (item != null && item.gameObject != null)
            {
                Destroy(item.gameObject);
            }
        }
        catch { /* defensivo */ }
    }

    #endregion

    #region PopUp
    void CriarPopup(ItemInterativo item, Renderer rend, Vector3 hitPoint, Vector3 hitNormal)
    {
        mostrandoPopup = true;
        objetoRend = rend;
        matOriginal = rend != null ? rend.material : null;

        // primeiro, tenta detectar um anchor customizado (ex: DoorInteractable.popupAnchor)
        Transform customAnchor = null;
        DoorInteractable door = item.GetComponentInParent<DoorInteractable>();
        if (door != null && door.popupAnchor != null)
        {
            customAnchor = door.popupAnchor;
        }
        else
        {
            // também checa se o ItemInterativo tem uma referência opcional (se você a adicionar)
            var itemAnchor = item.GetComponentInParent<PopupAnchor>();
            if (itemAnchor != null && itemAnchor.anchor != null)
                customAnchor = itemAnchor.anchor;
        }

        Vector3 worldPos;
        if (customAnchor != null)
        {
            // usa a posição do anchor (permite colocar o pivot manualmente no prefab)
            worldPos = customAnchor.position + Vector3.up * 0.05f; // pequeno uplift para segurança
        }
        else
        {
            // usa o ponto de impacto e desloca para fora da superfície pela normal do hit
            float outwardOffset = 0.25f; // quanto afastar do objeto (ajuste se necessário)
            worldPos = hitPoint + hitNormal.normalized * outwardOffset + Vector3.up * alturaPopup;
        }

        // instancia sem parent (world space)
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

        // garante que o popup sempre olhe para a câmera do jogador imediatamente
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

    void AtualizarPopup()
    {
        if (popupInstance == null || objetoInterativo == null) return;

        // reposiciona com base nos bounds (evita popup "entrar" no locker)
        popupInstance.transform.position = GetTopWorldPosition(objetoInterativo.gameObject, alturaPopup);

        // sempre virar para a câmera
        if (jogadorCamera != null)
        {
            popupInstance.transform.LookAt(jogadorCamera);
            popupInstance.transform.Rotate(0, 180f, 0);
        }
    }

    private Vector3 GetTopWorldPosition(GameObject go, float offset)
    {
        // 1) Se o objeto tem ItemInterativo com interactionPoint definido, use esse ponto (muito preciso)
        ItemInterativo ii = go.GetComponent<ItemInterativo>();
        if (ii != null && ii.interactionPoint != null)
        {
            Vector3 world = ii.interactionPoint.position;
            // aplica offset local (transforma de local para world)
            if (ii.interactionPoint != null && ii.popupOffset != Vector3.zero)
                world += ii.interactionPoint.TransformVector(ii.popupOffset);
            return world;
        }

        // 2) Caso contrário, tenta Collider primeiro (mais exato para objetos grandes)
        Collider col = go.GetComponent<Collider>();
        if (col != null)
        {
            Bounds b = col.bounds;
            Vector3 top = b.center + Vector3.up * (b.extents.y + offset);

            // deslocamento para frente: usa a forward do próprio objeto (reduz chances de popup "entrar" na parede)
            Vector3 forward = go.transform.forward;
            float forwardOffset = Mathf.Max(0.15f, offset * 0.5f); // ajuste fino: 0.15m mínimo
            Vector3 forwardShift = forward.normalized * forwardOffset;

            return top + forwardShift;
        }

        // 3) Fallback para Renderer bounds (se tiver)
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

        // 4) fallback final: posição do transform + offset + pequeno forward
        Vector3 basePos = go.transform.position + Vector3.up * offset;
        basePos += go.transform.forward * 0.15f;
        return basePos;
    }

    #endregion

    void PulsarContorno()
    {
        if (objetoRend == null || materialContorno == null) return;

        // ignora se o objeto interativo (ou seu root) estiver marcado para não ter outline
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
