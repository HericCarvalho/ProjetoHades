using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class HUD_Interacao : MonoBehaviour
{
    public static HUD_Interacao instancia;

    [Header("Mensagens")]
    public GameObject caixaMensagem;
    public TMP_Text textoMensagem;
    public float tempoMensagem = 2f;
    public float fadeVel = 4f;

    [Header("Notificações")]
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
        if (instancia == null) instancia = this;
        else Destroy(gameObject);

        if (caixaMensagem != null)
        {
            cgMensagem = caixaMensagem.GetComponent<CanvasGroup>();
            posMensagemOriginal = caixaMensagem.transform.localPosition;
            cgMensagem.alpha = 0;
        }

        if (caixaNotificacao != null)
        {
            cgNotificacao = caixaNotificacao.GetComponent<CanvasGroup>();
            posNotificacaoOriginal = caixaNotificacao.transform.localPosition;
            cgNotificacao.alpha = 0;
        }
    }

    public void MostrarMensagem(string texto)
    {
        if (mensagemCoroutine != null) StopCoroutine(mensagemCoroutine);
        mensagemCoroutine = StartCoroutine(MostrarMensagemCoroutine(texto));
    }

    IEnumerator MostrarMensagemCoroutine(string texto)
    {
        textoMensagem.text = texto;
        cgMensagem.alpha = 0;
        caixaMensagem.transform.localPosition = posMensagemOriginal;
        caixaMensagem.SetActive(true);

        while (cgMensagem.alpha < 1f) { cgMensagem.alpha += Time.deltaTime * fadeVel; yield return null; }
        yield return new WaitForSeconds(tempoMensagem);
        while (cgMensagem.alpha > 0f) { cgMensagem.alpha -= Time.deltaTime * fadeVel; yield return null; }
        caixaMensagem.SetActive(false);
    }

    public void MostrarNotificacao(string texto, Sprite imagem)
    {
        if (notificacaoCoroutine != null) StopCoroutine(notificacaoCoroutine);
        notificacaoCoroutine = StartCoroutine(MostrarNotificacaoCoroutine(texto, imagem));
    }

    IEnumerator MostrarNotificacaoCoroutine(string texto, Sprite imagem)
    {
        textoNotificacao.text = texto;
        imagemNotificacao.sprite = imagem;
        cgNotificacao.alpha = 0;
        caixaNotificacao.transform.localPosition = posNotificacaoOriginal;
        caixaNotificacao.SetActive(true);

        float t = 0f;
        Vector3 basePos = posNotificacaoOriginal;

        while (cgNotificacao.alpha < 1f)
        {
            cgNotificacao.alpha += Time.deltaTime * fadeVelNotificacao;
            t += Time.deltaTime * pulsacaoVel;
            caixaNotificacao.transform.localScale = Vector3.one * (1 + Mathf.Sin(t) * pulsacaoMagnitude);
            yield return null;
        }
        caixaNotificacao.transform.localScale = Vector3.one;

        yield return new WaitForSeconds(tempoNotificacao);

        t = 0f;
        while (cgNotificacao.alpha > 0f)
        {
            cgNotificacao.alpha -= Time.deltaTime * fadeVelNotificacao;
            t += Time.deltaTime * pulsacaoVel;
            caixaNotificacao.transform.localScale = Vector3.one * (1 + Mathf.Sin(t) * pulsacaoMagnitude);
            yield return null;
        }

        caixaNotificacao.transform.localScale = Vector3.one;
        caixaNotificacao.SetActive(false);
    }
}
