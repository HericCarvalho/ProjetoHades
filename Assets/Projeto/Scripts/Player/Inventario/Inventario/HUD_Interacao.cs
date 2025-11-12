using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class HUD_Interacao : MonoBehaviour
{
    public static HUD_Interacao instancia;

    [Header("Mensagens")]
    public GameObject caixaMensagem;
    public TMP_Text textoMensagem;
    public float tempoMensagem = 2f;
    public float fadeVel = 4f;

    [Header("Notificações (fila)")]
    public GameObject caixaNotificacao;
    public TMP_Text textoNotificacao;
    public Image imagemNotificacao;
    public float tempoNotificacao = 2f;
    public float fadeVelNotificacao = 5f;
    public float deslocamentoNotificacao = 30f;
    public float tempoFadeOut = 0.6f;

    [SerializeField] private GameObject HUD_Celular;
    [SerializeField] private GameObject HUD_blocodenotas;
    [SerializeField] private MonoBehaviour playerController;
    [SerializeField] private Rigidbody playerRigidbody; 
    
    private bool HUDativa = false;
    private bool bloqueadoPorHUD = false;

    [SerializeField] private bool temCelular = false;

    private CanvasGroup cgMensagem;
    private CanvasGroup cgNotificacao;
    private Vector3 posMensagemOriginal;
    private Vector3 posNotificacaoOriginal;

    private readonly Queue<(string texto, Sprite imagem)> filaNotificacoes = new Queue<(string, Sprite)>();
    private Coroutine processadorFila;

    private void Update()
    {
        AbrirCelular();
        LigarLanterna();
    }
    private void Awake()
    {
        if (instancia == null) instancia = this;
        else { Destroy(gameObject); return; }

        // mensagens
        if (caixaMensagem != null)
        {
            cgMensagem = caixaMensagem.GetComponent<CanvasGroup>();
            if (cgMensagem == null) cgMensagem = caixaMensagem.AddComponent<CanvasGroup>();
            posMensagemOriginal = caixaMensagem.transform.localPosition;
            cgMensagem.alpha = 0f;
            caixaMensagem.SetActive(false);
        }

        // notificações
        if (caixaNotificacao != null)
        {
            cgNotificacao = caixaNotificacao.GetComponent<CanvasGroup>();
            if (cgNotificacao == null) cgNotificacao = caixaNotificacao.AddComponent<CanvasGroup>();
            posNotificacaoOriginal = caixaNotificacao.transform.localPosition;
            cgNotificacao.alpha = 0f;
            caixaNotificacao.SetActive(false);
        }
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

        // fade in (unscaled)
        while (cgMensagem.alpha < 1f)
        {
            cgMensagem.alpha = Mathf.Min(1f, cgMensagem.alpha + Time.unscaledDeltaTime * fadeVel);
            yield return null;
        }

        // espera o tempo de exibição (unscaled)
        yield return new WaitForSecondsRealtime(tempoMensagem);

        // fade out
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

            // prepara o visual
            textoNotificacao.text = texto ?? "";
            if (imagemNotificacao != null)
            {
                imagemNotificacao.sprite = imagem;
                imagemNotificacao.enabled = imagem != null;
            }

            // coloca na posição original e zera alpha
            caixaNotificacao.transform.localPosition = posNotificacaoOriginal;
            caixaNotificacao.SetActive(true);
            cgNotificacao.alpha = 0f;

            // FADE IN
            while (cgNotificacao.alpha < 1f)
            {
                cgNotificacao.alpha = Mathf.Min(1f, cgNotificacao.alpha + Time.unscaledDeltaTime * fadeVelNotificacao);
                yield return null;
            }

            // Mantém totalmente visível por 'tempoNotificacao' (unscaled)
            yield return new WaitForSecondsRealtime(tempoNotificacao);

            // FADE OUT + SUBIDA: interpolamos alpha 1->0 enquanto subimos de posNotificacaoOriginal
            float elapsed = 0f;
            Vector3 startPos = posNotificacaoOriginal;
            Vector3 endPos = posNotificacaoOriginal + Vector3.up * deslocamentoNotificacao;

            while (elapsed < tempoFadeOut)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / tempoFadeOut);

                // alpha diminui suavemente (pode usar curve mais tarde)
                cgNotificacao.alpha = Mathf.Lerp(1f, 0f, t);

                // posição sobe suavemente (ease-out)
                float ease = 1f - Mathf.Pow(1f - t, 2f); // ease out quadratic
                caixaNotificacao.transform.localPosition = Vector3.Lerp(startPos, endPos, ease);

                yield return null;
            }

            // garante final limpo
            cgNotificacao.alpha = 0f;
            caixaNotificacao.transform.localPosition = posNotificacaoOriginal;
            caixaNotificacao.SetActive(false);

            // pequena pausa entre notificações (opcional)
            yield return null;
        }

        processadorFila = null;
    }
    #endregion

    #region FunçõesCelular

    public void PegarCelular()
    {
        temCelular = true;
        Debug.Log("Celular adicionado ao inventário! Agora você pode abrir o HUD.");
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

            BloqueioAoAbrirCelular(HUDativa);
        }
    }
    private void BloqueioAoAbrirCelular(bool bloquear)
    {
        bloqueadoPorHUD = bloquear;

        if (playerController != null) playerController.enabled = !bloquear;

        if (playerRigidbody != null)
        {
            if (bloquear)
            {
                playerRigidbody.angularVelocity = Vector3.zero;
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

    private void LigarLanterna()
    {

    }

    public void AbrirBlocodeNotas()
    {
        if (HUD_blocodenotas == null || HUD_Celular == null) return;

        HUD_blocodenotas.SetActive(true);
        HUD_Celular.SetActive(false);
        HUDativa = false;

        BloqueioAoAbrirCelular(true);
    }

    public void FecharBlocodeNotas()
    {
        if (HUD_blocodenotas == null) return;

        HUD_blocodenotas.SetActive(false);

        BloqueioAoAbrirCelular(false);
    }

    #endregion

    private void StopCoroutineSafe(string coroName)
    {
        try { StopCoroutine(coroName); } catch { }
    }
}
