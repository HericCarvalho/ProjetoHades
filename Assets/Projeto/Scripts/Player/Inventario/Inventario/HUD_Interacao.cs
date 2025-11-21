using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Audio;

public class HUD_Interacao : MonoBehaviour
{
    public static HUD_Interacao instancia;

    [Header("Mensagens")]
    public GameObject caixaMensagem;
    public TMP_Text textoMensagem;
    public float tempoMensagem = 2f;
    public float fadeVel = 4f;

    [Header("Notificacoes (fila)")]
    public GameObject caixaNotificacao;
    public TMP_Text textoNotificacao;
    public Image imagemNotificacao;
    public float tempoNotificacao = 2f;
    public float fadeVelNotificacao = 5f;
    public float deslocamentoNotificacao = 30f;
    public float tempoFadeOut = 0.6f;

    [Header("Lanterna / UI")]
    [SerializeField] private Light lanternaLuz;
    [SerializeField] private Button botaoLanterna;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip somLanterna;
    private bool lanternaLigada = false;

    [Header("HUD do Diario")]
    public GameObject painelDiario;
    public TMP_Text textoPaginaUI;
    public Image imagemPaginaUI;
    public TMP_Text indicadorPaginaUI;
    public Button botaoAnterior;
    public Button botaoProximo;

    [Header("Config Do Diario")]
    public int totalPages = 20;
    public KeyCode teclaAbrir = KeyCode.Q;

    [Header("audio Do Diario")]
    [SerializeField] private AudioSource audioSourceDiario;
    [SerializeField] private AudioClip somColetaPagina;
    [SerializeField] private AudioClip somFolhear;

    [Header("Animacao de folheado")]
    public float duracaoFolheado = 0.28f;
    public AnimationCurve curvaFolheado = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Capa do Diário")]
    public Canvas capaCanvas;
    public TMP_Text capaTexto;
    public string Titulo = "Diário de Sobrevivencia";
    public float duracaoCapa = 0.8f;
    public float fadeInCapa = 0.12f;
    public float fadeOutCapa = 0.18f;
    public float scalePunch = 0.06f;
    public GameObject conteudoPaginas;
    public AudioClip somAbrirCapa;

    [Header("Bloco de Notas - Quests")]
    [SerializeField] private TMP_Text notasQuestsText; 

    [Header("Gerais")]
    [SerializeField] private GameObject HUD_Celular;
    [SerializeField] private GameObject HUD_blocodenotas;
    [SerializeField] private MonoBehaviour playerController;
    [SerializeField] private Rigidbody playerRigidbody;

    private bool HUDativa = false;
    private bool bloqueadoPorHUD = false;

    [SerializeField] private bool temCelular = false;

    // Diário
    private bool desbloqueado = false;
    private int paginaAtual = 1;
    private bool isAnimating = false;
    private RectTransform conteudoRect;

    private CanvasGroup cgCapa;
    private Coroutine capaCoroutine;

    private CanvasGroup cgMensagem;
    private CanvasGroup cgNotificacao;
    private Vector3 posMensagemOriginal;
    private Vector3 posNotificacaoOriginal;

    private readonly Queue<(string texto, Sprite imagem)> filaNotificacoes = new Queue<(string, Sprite)>();
    private readonly Dictionary<int, PageItem> paginasColetadas = new Dictionary<int, PageItem>();

    // Guarda as quests que aparecem no bloco de notas (questSO -> QuestEntry)
    private readonly Dictionary<QuestSO, QuestEntry> questsNoBloco = new Dictionary<QuestSO, QuestEntry>();

    private Coroutine processadorFila;

    private void Awake()
    {
        if (instancia == null)
        {
            instancia = this;
            Debug.Log("[HUD_Interacao] instância criada.");
        }
        else
        {
            Debug.Log("[HUD_Interacao] outra instância encontrada — destruindo este objeto.");
            Destroy(gameObject);
            return;
        }

        InicializarMensagensENotificacoes();
        InicializarLanternaUI();
        InicializarDiario();
    }
    private void OnEnable()
    {
        // Inscreve-se nos eventos do QuestManager para atualizar o bloco de notas
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestAdded += HandleQuestAdded;
            QuestManager.Instance.OnQuestCompleted += HandleQuestCompleted;
        }
    }
    private void OnDisable()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestAdded -= HandleQuestAdded;
            QuestManager.Instance.OnQuestCompleted -= HandleQuestCompleted;
        }
    }
    private void Start()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestAdded -= HandleQuestAdded;
            QuestManager.Instance.OnQuestCompleted -= HandleQuestCompleted;

            QuestManager.Instance.OnQuestAdded += HandleQuestAdded;
            QuestManager.Instance.OnQuestCompleted += HandleQuestCompleted;

            Debug.Log("[HUD] Subscribed to QuestManager events in Start()");

            var active = QuestManager.Instance.GetActiveQuests();
            if (active != null && active.Count > 0)
            {
                Debug.Log("[HUD] Sincronizando quests ativas: count=" + active.Count);
                foreach (var e in active) HandleQuestAdded(e);
            }
        }
        else
        {
            Debug.LogWarning("[HUD] QuestManager.Instance is NULL in Start()");
        }

        Debug.Log("[HUD] notasQuestsText = " + (notasQuestsText == null ? "NULL" : "OK"));
    }

    #region Inicializadores
    private void InicializarMensagensENotificacoes()
    {
        if (caixaMensagem != null)
        {
            cgMensagem = caixaMensagem.GetComponent<CanvasGroup>();
            if (cgMensagem == null) cgMensagem = caixaMensagem.AddComponent<CanvasGroup>();
            posMensagemOriginal = caixaMensagem.transform.localPosition;
            cgMensagem.alpha = 0f;
            caixaMensagem.SetActive(false);
        }

        if (caixaNotificacao != null)
        {
            cgNotificacao = caixaNotificacao.GetComponent<CanvasGroup>();
            if (cgNotificacao == null) cgNotificacao = caixaNotificacao.AddComponent<CanvasGroup>();
            posNotificacaoOriginal = caixaNotificacao.transform.localPosition;
            cgNotificacao.alpha = 0f;
            caixaNotificacao.SetActive(false);
        }
    }
    private void InicializarLanternaUI()
    {
        // debug rápido
        Debug.Log("[HUD] InicializarLanternaUI: temCelular=" + temCelular + ", botaoLanterna != null: " + (botaoLanterna != null) + ", lanternaLuz != null: " + (lanternaLuz != null));

        // garante que a luz esteja em estado conhecido (desligada por padrão)
        if (lanternaLuz != null)
        {
            // ativa o GameObject da luz se estiver desativado na hierarquia
            if (!lanternaLuz.gameObject.activeInHierarchy)
                lanternaLuz.gameObject.SetActive(false); // mantemos OFF por padrão

            // garante parâmetros visíveis caso alguém esqueça no Inspector
            if (lanternaLuz.intensity <= 0f) lanternaLuz.intensity = 2f;
            if (lanternaLuz.range <= 0f) lanternaLuz.range = 30f;

            // forçamos disabled por segurança (estado inicial)
            lanternaLigada = false;
            lanternaLuz.enabled = false;
        }

        // configura o botão (se houver)
        if (botaoLanterna != null)
        {
            botaoLanterna.onClick.RemoveAllListeners();
            // adiciona listener seguro (usa lambda para evitar problemas com referências)
            botaoLanterna.onClick.AddListener(() =>
            {
                LigarLanterna();
            });

            // interatividade depende de temCelular (será atualizada em PegarCelular também)
            botaoLanterna.interactable = temCelular;
        }
        else
        {
            Debug.LogWarning("[HUD] botaoLanterna NÃO atribuído no Inspector.");
            // tentativa de fallback: procura um Button dentro do painel do celular (opcional)
            if (HUD_Celular != null)
            {
                var btn = HUD_Celular.GetComponentInChildren<UnityEngine.UI.Button>();
                if (btn != null)
                {
                    botaoLanterna = btn;
                    Debug.Log("[HUD] botaoLanterna encontrado como fallback em HUD_Celular: " + btn.name);
                    botaoLanterna.onClick.RemoveAllListeners();
                    botaoLanterna.onClick.AddListener(LigarLanterna);
                    botaoLanterna.interactable = temCelular;
                }
            }
        }

        // proteção: configura audioSource para não estar em loop
        if (audioSource != null)
        {
            audioSource.loop = false;
            audioSource.playOnAwake = false;
        }
    }
    private void InicializarDiario()
    {
        if (painelDiario != null)
            painelDiario.SetActive(false);

        if (botaoAnterior != null)
        {
            botaoAnterior.onClick.RemoveAllListeners();
            botaoAnterior.onClick.AddListener(() => PreviousPage());
        }

        if (botaoProximo != null)
        {
            botaoProximo.onClick.RemoveAllListeners();
            botaoProximo.onClick.AddListener(() => NextPage());
        }

        if (textoPaginaUI != null)
            conteudoRect = textoPaginaUI.GetComponentInParent<RectTransform>();

        if (conteudoRect == null && imagemPaginaUI != null)
            conteudoRect = imagemPaginaUI.GetComponentInParent<RectTransform>();

        if (conteudoRect == null && painelDiario != null)
            conteudoRect = painelDiario.GetComponent<RectTransform>();

        if (capaCanvas != null)
        {
            cgCapa = capaCanvas.GetComponent<CanvasGroup>();
            if (cgCapa == null) cgCapa = capaCanvas.gameObject.AddComponent<CanvasGroup>();
            capaCanvas.gameObject.SetActive(false);
            cgCapa.alpha = 0f;

            // define texto padrão se existir TMP
            if (capaTexto != null && !string.IsNullOrEmpty(Titulo))
                capaTexto.text = Titulo;
        }

        if (conteudoPaginas == null)
        {
            if (textoPaginaUI != null)
                conteudoPaginas = textoPaginaUI.transform.parent?.gameObject;

            if (conteudoPaginas == null && painelDiario != null)
            {
                foreach (Transform t in painelDiario.transform)
                {
                    string n = t.name.ToLower();
                    if (n.Contains("content") || n.Contains("conteudo") || n.Contains("pages") || n.Contains("pagina"))
                    {
                        conteudoPaginas = t.gameObject;
                        break;
                    }
                }
            }
        }
    }

    #endregion

    private void Update()
    {
        AbrirCelular();
        VerificarEntradaDiario();
    }

    #region Mensagens simples
    public void MostrarMensagem(string texto)
    {
        if (caixaMensagem == null || textoMensagem == null) return;
        StopCoroutineSafe(nameof(MostrarMensagemCoroutine));
        StartCoroutine(MostrarMensagemCoroutine(texto));
    }
    private IEnumerator MostrarMensagemCoroutine(string texto)
    {
        textoMensagem.text = texto;
        caixaMensagem.SetActive(true);
        cgMensagem.alpha = 0f;
        caixaMensagem.transform.localPosition = posMensagemOriginal;

        while (cgMensagem.alpha < 1f)
        {
            cgMensagem.alpha = Mathf.Min(1f, cgMensagem.alpha + Time.unscaledDeltaTime * fadeVel);
            yield return null;
        }

        yield return new WaitForSecondsRealtime(tempoMensagem);

        while (cgMensagem.alpha > 0f)
        {
            cgMensagem.alpha = Mathf.Max(0f, cgMensagem.alpha - Time.unscaledDeltaTime * fadeVel);
            yield return null;
        }

        caixaMensagem.SetActive(false);
    }
    #endregion

    #region Notificações com subida suave e fade-out
    public void MostrarNotificacao(string texto, Sprite imagem)
    {
        if (string.IsNullOrEmpty(texto) && imagem == null) return;
        filaNotificacoes.Enqueue((texto, imagem));
        if (processadorFila == null)
            processadorFila = StartCoroutine(ProcessarFilaNotificacoes());
    }
    private IEnumerator ProcessarFilaNotificacoes()
    {
        if (caixaNotificacao == null || textoNotificacao == null || cgNotificacao == null)
        {
            filaNotificacoes.Clear();
            processadorFila = null;
            yield break;
        }

        while (filaNotificacoes.Count > 0)
        {
            var (texto, imagem) = filaNotificacoes.Dequeue();

            textoNotificacao.text = texto ?? "";
            if (imagemNotificacao != null)
            {
                imagemNotificacao.sprite = imagem;
                imagemNotificacao.enabled = imagem != null;
            }

            caixaNotificacao.transform.localPosition = posNotificacaoOriginal;
            caixaNotificacao.SetActive(true);
            cgNotificacao.alpha = 0f;

            while (cgNotificacao.alpha < 1f)
            {
                cgNotificacao.alpha = Mathf.Min(1f, cgNotificacao.alpha + Time.unscaledDeltaTime * fadeVelNotificacao);
                yield return null;
            }

            yield return new WaitForSecondsRealtime(tempoNotificacao);

            float elapsed = 0f;
            Vector3 startPos = posNotificacaoOriginal;
            Vector3 endPos = posNotificacaoOriginal + Vector3.up * deslocamentoNotificacao;

            while (elapsed < tempoFadeOut)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / tempoFadeOut);

                cgNotificacao.alpha = Mathf.Lerp(1f, 0f, t);

                float ease = 1f - Mathf.Pow(1f - t, 2f);
                caixaNotificacao.transform.localPosition = Vector3.Lerp(startPos, endPos, ease);

                yield return null;
            }

            cgNotificacao.alpha = 0f;
            caixaNotificacao.transform.localPosition = posNotificacaoOriginal;
            caixaNotificacao.SetActive(false);

            yield return null;
        }

        processadorFila = null;
    }
    #endregion

    #region FuncoesCelular
    public void PegarCelular()
    {
        if (temCelular) return;

        temCelular = true;

        if (botaoLanterna != null)
            botaoLanterna.interactable = true;
    }
    private void AbrirCelular()
    {
        if (temCelular && Input.GetKeyDown(KeyCode.F))
        {
            HUDativa = !HUDativa;

            if (HUD_Celular != null)
                HUD_Celular.SetActive(HUDativa);

            if (HUDativa && HUD_blocodenotas != null && HUD_blocodenotas.activeSelf)
                HUD_blocodenotas.SetActive(false);

            BloqueioAoAbrirCelularDiario(HUDativa);
        }
    }
    private void BloqueioAoAbrirCelularDiario(bool bloquear)
    {
        bloqueadoPorHUD = bloquear;

        if (playerController != null)
            playerController.enabled = !bloquear;

        if (playerRigidbody != null)
        {
            if (bloquear)
            {
                // zera rotação e velocidade linear (propriedade correta: velocity)
                playerRigidbody.angularVelocity = Vector3.zero;
                playerRigidbody.linearVelocity = Vector3.zero;
            }
            else
            {
                // nada especial ao desbloquear por enquanto
            }
        }

        if (bloquear)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
    public void LigarLanterna()
    {
        if (!temCelular) return;

        lanternaLigada = lanternaLuz.enabled;

        lanternaLigada = !lanternaLigada;
        lanternaLuz.enabled = lanternaLigada;

        MostrarNotificacao(lanternaLigada ? "Lanterna ligada" : "Lanterna desligada", null);

        if (audioSource != null && somLanterna != null)
            audioSource.PlayOneShot(somLanterna);
    }
    public void AbrirBlocodeNotas()
    {
        if (HUD_blocodenotas == null || HUD_Celular == null) return;

        HUD_blocodenotas.SetActive(true);
        HUD_Celular.SetActive(false);
        HUDativa = false;

        BloqueioAoAbrirCelularDiario(true);
    }
    public void FecharBlocodeNotas()
    {
        if (HUD_blocodenotas == null) return;

        HUD_blocodenotas.SetActive(false);

        BloqueioAoAbrirCelularDiario(false);
    }
    private void HandleQuestAdded(QuestEntry entry)
    {
        Debug.Log("[HUD] HandleQuestAdded called for: " + (entry?.quest?.name ?? "NULL"));
        if (entry == null || entry.quest == null) return;
        if (!questsNoBloco.ContainsKey(entry.quest))
            questsNoBloco.Add(entry.quest, entry);
        RefreshNotasQuestsUI();
    }
    private void HandleQuestCompleted(QuestEntry entry)
    {
        Debug.Log("[HUD] HandleQuestCompleted called for: " + (entry?.quest?.name ?? "NULL"));
        if (entry == null || entry.quest == null) return;
        if (questsNoBloco.ContainsKey(entry.quest))
            questsNoBloco[entry.quest] = entry;
        RefreshNotasQuestsUI();
    }
    private void RefreshNotasQuestsUI()
    {
        if (notasQuestsText == null)
        {
            Debug.LogWarning("[HUD] RefreshNotasQuestsUI: notasQuestsText == NULL");
            return;
        }

        var list = new List<QuestEntry>(questsNoBloco.Values);
        list.Sort((a, b) => a.addedTime.CompareTo(b.addedTime));

        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        foreach (var e in list)
        {
            if (e == null || e.quest == null) continue;

            string tipoLabel;
            switch (e.quest.tipo)
            {
                case QuestType.Principal: tipoLabel = "Principal"; break;
                case QuestType.Ramificacao: tipoLabel = "Ramificação"; break;
                case QuestType.Secundaria: tipoLabel = "Secundária"; break;
                default: tipoLabel = "Outro"; break;
            }

            string descricao = string.IsNullOrEmpty(e.quest.descricao) ? "" : e.quest.descricao.Trim();

            // Exibe só o tipo + quebra de linha + descrição (sem indent)
            if (e.completed)
            {
                sb.AppendLine($"<s>[{tipoLabel}]</s>");
                if (!string.IsNullOrEmpty(descricao))
                    sb.AppendLine($"<s>{descricao}</s>");
            }
            else
            {
                sb.AppendLine($"[{tipoLabel}]");
                if (!string.IsNullOrEmpty(descricao))
                    sb.AppendLine(descricao);
            }

            sb.AppendLine(); // separador entre quests
        }

        notasQuestsText.text = sb.ToString();

        // Força rebuild do layout (útil se estiver dentro de LayoutGroups / Content Size Fitters)
        var rt = notasQuestsText.GetComponent<RectTransform>();
        if (rt != null)
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
    }
    #endregion

    #region FuncoesDiario
    public void AbrirFecharDiario()
    {
        if (painelDiario == null) return;

        bool novo = !painelDiario.activeSelf;
        painelDiario.SetActive(novo);

        if (novo)
        {
            if (HUD_Celular != null && HUD_Celular.activeSelf)
                HUD_Celular.SetActive(false);

            // se temos capa configurada, inicia animação; caso contrário, mostra imediatamente as paginas
            if (capaCanvas != null && capaTexto != null)
            {
                // atualiza o texto da capa (se quiser definir dinamicamente antes)
                if (string.IsNullOrEmpty(capaTexto.text)) capaTexto.text = Titulo;

                // começa coroutine de capa (ela chamará AtualizarPaginaUI no final)
                if (capaCoroutine != null) StopCoroutine(capaCoroutine);
                capaCoroutine = StartCoroutine(AparicaoCapa());
            }
            else
            {
                AtualizarPaginaUI(immediate: true);
            }
        }
        else
        {
            // fechar: cancela qualquer animação de capa em andamento
            if (capaCoroutine != null) { StopCoroutine(capaCoroutine); capaCoroutine = null; }
            if (capaCanvas != null) { capaCanvas.gameObject.SetActive(false); if (cgCapa != null) cgCapa.alpha = 0f; }
        }

        BloqueioAoAbrirCelularDiario(novo);
    }
    public void PegarDiario()
    {
        if (desbloqueado) return;

        desbloqueado = true;
        paginaAtual = 1;

        if (painelDiario != null) painelDiario.SetActive(false);

        var itens = SistemaInventario.instancia?.GetItens();
        if (itens != null)
        {
            foreach (var e in itens)
            {
                if (e.item is PageItem p)
                {
                    if (!paginasColetadas.ContainsKey(p.numeroPagina))
                        paginasColetadas[p.numeroPagina] = p;
                }
            }
        }

        AtualizarPaginaUI(immediate: true);

        if (HUD_Interacao.instancia != null) MostrarNotificacao("Diario coletado! Pressione Q para abrir.", null);
    }
    public void IntegrarPagina(PageItem page)
    {
        if (this == null)
        {
            Debug.LogWarning("[HUD] IntegrarPagina chamado, mas HUD invalida.");
            return;
        }

        // Caso 1: chamaram sem passar a página — tentar encontrar no inventário
        if (page == null)
        {
            var itens = SistemaInventario.instancia?.GetItens();
            if (itens == null || itens.Count == 0)
            {
                Debug.Log("[HUD] IntegrarPagina recebeu null e não encontrou nada no inventário.");
                return;
            }

            bool anyIntegrated = false;

            foreach (var entrada in itens.ToArray()) // ToArray() para evitar alteração da lista durante iteração
            {
                if (entrada == null || entrada.item == null) continue;

                if (entrada.item is PageItem p)
                {
                    if (paginasColetadas.ContainsKey(p.numeroPagina))
                    {
                        Debug.Log($"[HUD] IntegrarPagina: página #{p.numeroPagina} já integrada, pulando.");
                        continue;
                    }

                    paginasColetadas[p.numeroPagina] = p;
                    Debug.Log($"[HUD] IntegrarPagina: integração automática da página #{p.numeroPagina} (vinda do inventário).");

                    if (audioSourceDiario != null && somColetaPagina != null)
                        audioSourceDiario.PlayOneShot(somColetaPagina);

                    MostrarNotificacao($"Página {p.numeroPagina} coletada. Pagina {p.numeroPagina} Foi adicionada ao Diario", p.iconeItem);

                    SistemaInventario.instancia?.RemoverItem(p, 1);

                    anyIntegrated = true;
                }
            }

            if (anyIntegrated)
                AtualizarPaginaUI(immediate: false);
            else
                Debug.Log("[HUD] IntegrarPagina: não encontrou PageItem não integrado no inventário.");

            return;
        }

        // --- A partir daqui, page != null -- validações e integração direta ---
        if (page.numeroPagina < 1 || page.numeroPagina > totalPages)
        {
            Debug.LogWarning($"[HUD] IntegrarPagina: número de página inválido ({page.numeroPagina}). totalPages={totalPages}");
            return;
        }

        bool jaExistia = paginasColetadas.ContainsKey(page.numeroPagina);

        // salva a página (idempotente)
        paginasColetadas[page.numeroPagina] = page;

        Debug.Log($"[HUD] IntegrarPagina chamado: #{page.numeroPagina} (asset: {page.name}) - jaExistia={jaExistia}");

        if (!jaExistia)
        {
            if (audioSourceDiario != null && somColetaPagina != null)
                audioSourceDiario.PlayOneShot(somColetaPagina);

            MostrarNotificacao($"Página {page.numeroPagina} coletada", page.iconeItem);

            // se a página estiver no inventário (coleta padrão), remove 1
            if (SistemaInventario.instancia != null)
            {
                // tenta remover uma unidade do item correspondente
                SistemaInventario.instancia.RemoverItem(page, 1);
                Debug.Log($"[HUD] IntegrarPagina: removeu a PageItem #{page.numeroPagina} do inventário (se existia).");
            }
        }
        else
        {
            Debug.Log($"[HUD] IntegrarPagina: página #{page.numeroPagina} já estava integrada — apenas atualizando UI.");
        }

        // atualiza a UI
        AtualizarPaginaUI(immediate: false);
    }
    public void IrParaPagina(int numero, bool animate = true)
    {
        numero = Mathf.Clamp(numero, 1, totalPages);
        if (numero == paginaAtual && !animate) return;

        if (isAnimating) return;

        if (animate && audioSourceDiario != null && somFolhear != null)
            audioSourceDiario.PlayOneShot(somFolhear);

        if (animate && conteudoRect != null)
            StartCoroutine(FlipAnimation(numero));
        else
        {
            paginaAtual = numero;
            AtualizarPaginaUI(immediate: true);
        }
    }
    private void AtualizarPaginaUI(bool immediate = false)
    {
        if (painelDiario == null) return;

        if (indicadorPaginaUI != null)
            indicadorPaginaUI.text = $"Página {paginaAtual} / {totalPages}";

        if (paginasColetadas.TryGetValue(paginaAtual, out PageItem page))
        {
            if (textoPaginaUI != null) textoPaginaUI.text = string.IsNullOrEmpty(page.textoPagina) ? "<Página vazia>" : page.textoPagina;
            if (imagemPaginaUI != null)
            {
                imagemPaginaUI.sprite = page.imagemPagina;
                imagemPaginaUI.enabled = page.imagemPagina != null;
            }
        }
        else
        {
            if (textoPaginaUI != null) textoPaginaUI.text = "<Página não coletada>";
            if (imagemPaginaUI != null)
            {
                imagemPaginaUI.sprite = null;
                imagemPaginaUI.enabled = false;
            }
        }
    }
    private void VerificarEntradaDiario()
    {
        if (!desbloqueado) return;

        if (Input.GetKeyDown(teclaAbrir))
            AbrirFecharDiario();
    }
    public void NextPage()
    {
        int alvo = Mathf.Min(paginaAtual + 1, totalPages);
        IrParaPagina(alvo, animate: true);
    }
    public void PreviousPage()
    {
        int alvo = Mathf.Max(paginaAtual - 1, 1);
        IrParaPagina(alvo, animate: true);
    }
    private IEnumerator FlipAnimation(int targetPage)
    {
        if (conteudoRect == null)
        {
            paginaAtual = targetPage;
            AtualizarPaginaUI(immediate: true);
            yield break;
        }

        isAnimating = true;
        InteracoesBotoes(false);

        Vector3 originalScale = conteudoRect.localScale;
        float half = duracaoFolheado * 0.5f;
        float elapsed = 0f;

        while (elapsed < half)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / half);
            float k = curvaFolheado.Evaluate(t);
            Vector3 s = originalScale;
            s.x = Mathf.Lerp(1f, 0f, k);
            conteudoRect.localScale = s;
            yield return null;
        }

        paginaAtual = Mathf.Clamp(targetPage, 1, totalPages);
        AtualizarPaginaUI(immediate: true);

        elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / half);
            float k = curvaFolheado.Evaluate(t);
            Vector3 s = originalScale;
            s.x = Mathf.Lerp(0f, 1f, k);
            conteudoRect.localScale = s;
            yield return null;
        }

        conteudoRect.localScale = originalScale;

        InteracoesBotoes(true);
        isAnimating = false;
    }
    private IEnumerator AparicaoCapa()
    {
        if (capaCanvas == null || capaTexto == null || cgCapa == null)
        {
            AtualizarPaginaUI(immediate: true);
            yield break;
        }

        // bloqueia inputs durante animação
        isAnimating = true;
        InteracoesBotoes(false);

        // === ESCONDE o conteúdo das páginas para evitar bleed-through ===
        bool conteudoAtivoOriginal = true;
        if (conteudoPaginas != null)
        {
            conteudoAtivoOriginal = conteudoPaginas.activeSelf;
            conteudoPaginas.SetActive(false);
        }
        else
        {
            // fallback: desativa elementos individuais se não houver container
            if (textoPaginaUI != null) textoPaginaUI.gameObject.SetActive(false);
            if (imagemPaginaUI != null) imagemPaginaUI.gameObject.SetActive(false);
            if (indicadorPaginaUI != null) indicadorPaginaUI.gameObject.SetActive(false);
            if (botaoAnterior != null) botaoAnterior.gameObject.SetActive(false);
            if (botaoProximo != null) botaoProximo.gameObject.SetActive(false);
        }

        // ativa e configura capa
        capaCanvas.gameObject.SetActive(true);
        cgCapa.alpha = 0f;
        Vector3 originalScale = capaCanvas.transform.localScale;
        capaCanvas.transform.localScale = originalScale * (1f - scalePunch);

        // toca som de capa (opcional)
        if (audioSource != null && somAbrirCapa != null)
            audioSource.PlayOneShot(somAbrirCapa);

        // fade-in + leve scale up (pop)
        float t = 0f;
        while (t < fadeInCapa)
        {
            t += Time.unscaledDeltaTime;
            float alpha = Mathf.Clamp01(t / Mathf.Max(0.0001f, fadeInCapa));
            cgCapa.alpha = alpha;
            float s = Mathf.Lerp(1f - scalePunch, 1f + scalePunch * 0.2f, alpha);
            capaCanvas.transform.localScale = originalScale * s;
            yield return null;
        }
        cgCapa.alpha = 1f;
        capaCanvas.transform.localScale = originalScale;

        // hold
        float hold = Mathf.Max(0f, duracaoCapa - fadeInCapa - fadeOutCapa);
        if (hold > 0f) yield return new WaitForSecondsRealtime(hold);

        // fade-out
        t = 0f;
        while (t < fadeOutCapa)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Clamp01(1f - (t / Mathf.Max(0.0001f, fadeOutCapa)));
            cgCapa.alpha = a;
            float s = Mathf.Lerp(1f, 1f - scalePunch * 0.2f, 1f - a);
            capaCanvas.transform.localScale = originalScale * s;
            yield return null;
        }

        cgCapa.alpha = 0f;
        capaCanvas.gameObject.SetActive(false);

        // === ATIVA o conteúdo das páginas agora que a capa saiu ===
        if (conteudoPaginas != null)
        {
            conteudoPaginas.SetActive(true);
        }
        else
        {
            // fallback: reativa elementos individuais
            if (textoPaginaUI != null) textoPaginaUI.gameObject.SetActive(true);
            if (imagemPaginaUI != null) imagemPaginaUI.gameObject.SetActive(true);
            if (indicadorPaginaUI != null) indicadorPaginaUI.gameObject.SetActive(true);
            if (botaoAnterior != null) botaoAnterior.gameObject.SetActive(true);
            if (botaoProximo != null) botaoProximo.gameObject.SetActive(true);
        }

        // mostra o conteúdo do diário (as páginas)
        AtualizarPaginaUI(immediate: true);

        // libera inputs
        InteracoesBotoes(true);
        isAnimating = false;
        capaCoroutine = null;
    }
    private void InteracoesBotoes(bool valor)
    {
        if (botaoAnterior != null) botaoAnterior.interactable = valor;
        if (botaoProximo != null) botaoProximo.interactable = valor;
    }
    public bool TemPagina(int numero) => paginasColetadas.ContainsKey(numero);

    #endregion

    private void OnDestroy()
    {
        if (instancia == this)
            instancia = null;
    }


    private void StopCoroutineSafe(string coroName)
    {
        try { StopCoroutine(coroName); } catch { }
    }
}
