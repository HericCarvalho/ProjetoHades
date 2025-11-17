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

    void Update()
    {
        DetectarInteracao();
        if (Input.GetKeyDown(teclaInteragir)) Interagir();
    }

    void DetectarInteracao()
    {
        Ray ray = new Ray(jogadorCamera.position, jogadorCamera.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, distanciaInteracao, camadaInteracao))
        {
            ItemInterativo item = hit.collider.GetComponent<ItemInterativo>();
            if (item != null)
            {
                objetoInterativo = item;
                if (!mostrandoPopup) CriarPopup(item, hit.collider.GetComponent<Renderer>());
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

        // pega referência do jogador uma vez
        MovimentaçãoPlayer jogador = jogadorCamera.GetComponent<MovimentaçãoPlayer>();

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

        // 4) se for celular, faz a lógica de desbloqueio/aviso antes da interação
        if (ehCelular)
        {
            InteragirComCelular(objetoInterativo, jogador, true);
        }

        // 5) executa a interação normal (pode adicionar ao inventário / destruir o objeto, etc.)
        objetoInterativo.Interagir(jogador);

        // 6) se era uma PageItem, avisa o HUD para integrar (garante integração mesmo se o objeto de cena foi destruído)
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

        // 7) se era diário, executa o tratamento específico (mantive seu fluxo original)
        if (ehDiary)
        {
            TratarInteracaoDiary(objetoInterativo);
        }

        // supondo que objetoInterativo é um ItemInterativo ligado à fusebox (ou que você detectou a FuseBox via collider)
        CaixadeFusiveis box = objetoInterativo.GetComponentInParent<CaixadeFusiveis>();
        if (box != null)
        {
            // supondo que exista um único FuseBoxUI na cena (arraste via inspector)
            var ui = FindObjectOfType<UI_CaixadeFusiveis>();
            if (ui != null)
            {
                ui.OpenFor(box);
            }
        }


        // 8) limpa o popup
        RemoverPopup();
    }


    private void InteragirComCelular(ItemInterativo item, MovimentaçãoPlayer jogador, bool abrirImediatamente = true)
    {
        if (item == null) return;

        HUD_Interacao.instancia?.PegarCelular();
        HUD_Interacao.instancia?.MostrarMensagem($"Você conseguiu um celular! Use-o a Lanterna dele para iluminar e o bloco de notas para anotar seu proximo passo.");
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

        HUD_Interacao.instancia?.MostrarMensagem("Aparentemente outras pessoas estiveram aqui antes de mim, talvez esse diario me ajude a sair daqui");
        HUD_Interacao.instancia?.MostrarNotificacao("Diario coletado!", diaryItem != null ? diaryItem.iconeItem : null);

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
    void CriarPopup(ItemInterativo item, Renderer rend)
    {
        mostrandoPopup = true;
        objetoRend = rend;
        matOriginal = rend != null ? rend.material : null;

        // calcula ponto acima do objeto usando bounds (mais confiável que transform.position)
        Vector3 worldPos = GetTopWorldPosition(item.gameObject, alturaPopup);

        // instancia sem parent (world space)
        popupInstance = Instantiate(prefabPopup, worldPos, Quaternion.identity);
        popupTexto = popupInstance.GetComponentInChildren<TMP_Text>();
        popupIcone = popupInstance.GetComponentInChildren<Image>();

        if (popupTexto != null) popupTexto.text = $"Pressione {teclaInteragir} para {item.tipo}";
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
        // tente Collider primeiro (mais exato para objetos grandes)
        Collider col = go.GetComponent<Collider>();
        if (col != null)
        {
            Bounds b = col.bounds;
            return b.center + Vector3.up * (b.extents.y + offset);
        }

        // fallback para Renderer bounds
        Renderer r = go.GetComponentInChildren<Renderer>();
        if (r != null)
        {
            Bounds b = r.bounds;
            return b.center + Vector3.up * (b.extents.y + offset);
        }

        // fallback final: posição do transform + offset
        return go.transform.position + Vector3.up * offset;
    }
    #endregion

    #region Fusiveis

    public void PlaceFuseAutomatic(CaixadeFusiveis box, Fusiveis fuse)
    {
        for (int i = 0; i < 3; i++)
        {
            if (box.slots[i] == null)
            {
                box.PlaceFuseAtSlot(i, fuse);
                // opcional: remover do inventário se estava lá
                SistemaInventario.instancia?.RemoverItem(fuse, 1);
                return;
            }
        }
        // se não há slot vazio, pode mostrar mensagem
        HUD_Interacao.instancia?.MostrarMensagem("Caixa cheia: remova um fusível primeiro.");
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
