using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HUD_Interação : MonoBehaviour
{
    public static HUD_Interação instancia;

    [Header("Mensagens de Observação")]
    public GameObject caixaMensagem;
    public TMP_Text textoMensagem;
    public float tempoMensagem = 2f;
    public float fadeVel = 4f;

    [Header("Notificação de Inventário")]
    public GameObject caixaNotificacao;
    public TMP_Text textoNotificacao;
    public Image imagemNotificacao;
    public float tempoNotificacao = 2f;
    public float fadeVelNotificacao = 5f;
    public float pulsacaoMagnitude = 0.03f;
    public float pulsacaoVel = 3f;

    private CanvasGroup cgMensagem;
    private CanvasGroup cgNotificacao;
    private Coroutine mensagemCoroutine;
    private Coroutine notificacaoCoroutine;
    private Vector3 posMensagemOriginal;
    private Vector3 posNotificacaoOriginal;

    void Awake()
    {
        // Singleton
        if (instancia == null) instancia = this;
        else { Destroy(gameObject); return; }

        // CanvasGroup mensagem (cria se não existir)
        if (caixaMensagem != null)
        {
            cgMensagem = caixaMensagem.GetComponent<CanvasGroup>();
            if (cgMensagem == null) cgMensagem = caixaMensagem.AddComponent<CanvasGroup>();
            cgMensagem.alpha = 0f;
            posMensagemOriginal = caixaMensagem.transform.localPosition;
        }

        // CanvasGroup notificação (cria se não existir)
        if (caixaNotificacao != null)
        {
            cgNotificacao = caixaNotificacao.GetComponent<CanvasGroup>();
            if (cgNotificacao == null) cgNotificacao = caixaNotificacao.AddComponent<CanvasGroup>();
            cgNotificacao.alpha = 0f;
            posNotificacaoOriginal = caixaNotificacao.transform.localPosition;
        }
    }

    // ====== MENSAGEM (observável) ======
    public void MostrarMensagem(string mensagem)
    {
        if (caixaMensagem == null || textoMensagem == null || cgMensagem == null) return;

        if (mensagemCoroutine != null) StopCoroutine(mensagemCoroutine);
        mensagemCoroutine = StartCoroutine(MostrarMensagemRotina(mensagem));
    }

    private IEnumerator MostrarMensagemRotina(string mensagem)
    {
        // set
        if (!caixaMensagem.activeSelf) caixaMensagem.SetActive(true);
        textoMensagem.text = mensagem;
        caixaMensagem.transform.localScale = Vector3.one;
        caixaMensagem.transform.localPosition = posMensagemOriginal;

        // fade in
        while (cgMensagem.alpha < 1f)
        {
            cgMensagem.alpha += Time.deltaTime * fadeVel;
            yield return null;
        }

        // pulsação durante o tempo
        float t = 0f;
        while (t < tempoMensagem)
        {
            t += Time.deltaTime;
            float s = 1f + Mathf.Sin(Time.time * pulsacaoVel) * pulsacaoMagnitude;
            caixaMensagem.transform.localScale = Vector3.one * s;
            yield return null;
        }

        // fade out
        while (cgMensagem.alpha > 0f)
        {
            cgMensagem.alpha -= Time.deltaTime * fadeVel;
            yield return null;
        }

        caixaMensagem.transform.localScale = Vector3.one;
        caixaMensagem.SetActive(false);
    }

    // ====== NOTIFICAÇÃO (coleta) ======

    // SOBRECARGA 1: compatível com chamadas antigas (só texto)
    public void MostrarNotificacao(string mensagem)
    {
        MostrarNotificacao(mensagem, null);
    }

    // SOBRECARGA 2: texto + sprite (imagem opcional)
    public void MostrarNotificacao(string mensagem, Sprite imagem)
    {
        if (caixaNotificacao == null || textoNotificacao == null || cgNotificacao == null) return;

        if (notificacaoCoroutine != null) StopCoroutine(notificacaoCoroutine);
        notificacaoCoroutine = StartCoroutine(MostrarNotificacaoRotina(mensagem, imagem));
    }

    private IEnumerator MostrarNotificacaoRotina(string mensagem, Sprite imagem)
    {
        // set
        if (!caixaNotificacao.activeSelf) caixaNotificacao.SetActive(true);
        textoNotificacao.text = mensagem;

        if (imagemNotificacao != null)
        {
            if (imagem != null)
            {
                imagemNotificacao.sprite = imagem;
                imagemNotificacao.enabled = true;
            }
            else
            {
                imagemNotificacao.enabled = false; // se não tiver sprite, oculta a imagem
            }
        }

        caixaNotificacao.transform.localScale = Vector3.one;
        caixaNotificacao.transform.localPosition = posNotificacaoOriginal;

        // fade in
        while (cgNotificacao.alpha < 1f)
        {
            cgNotificacao.alpha += Time.deltaTime * fadeVelNotificacao;
            yield return null;
        }

        // pulsação durante o tempo
        float t = 0f;
        while (t < tempoNotificacao)
        {
            t += Time.deltaTime;
            float s = 1f + Mathf.Sin(Time.time * pulsacaoVel) * pulsacaoMagnitude;
            caixaNotificacao.transform.localScale = Vector3.one * s;
            yield return null;
        }

        // fade out
        while (cgNotificacao.alpha > 0f)
        {
            cgNotificacao.alpha -= Time.deltaTime * fadeVelNotificacao;
            yield return null;
        }

        caixaNotificacao.transform.localScale = Vector3.one;
        caixaNotificacao.SetActive(false);
    }
}