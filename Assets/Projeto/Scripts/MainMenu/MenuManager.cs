using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro; // se você usar TextMeshPro (opcional). Se não, substitua por UnityEngine.UI.Text

public class MenuManager : MonoBehaviour
{
    [Header("Cena")]
    public int cenaInicioIndex = 1;

    [Header("Painéis UI")]
    public GameObject painelMenuPrincipal;
    public GameObject painelOpcoes;
    public GameObject painelCreditos;

    [Header("Animação de painéis")]
    public float duracaoAnimPainel = 0.25f;
    public float scaleAbrir = 1f;
    public float scaleFechar = 0.9f;
    // Os painéis devem ter CanvasGroup (para animar alpha). Se não tiver, o script cria automaticamente.

    [Header("Transição / Fade")]
    public Image imagemFade; // imagem preta cobrindo a tela (full screen)
    public float duracaoFade = 0.6f;
    public bool usarFade = true;

    [Header("Audio")]
    public AudioSource audioTransicao; // arraste um AudioSource com o clip (porta/respiração)
    public float waitBeforeFade = 0.15f; // espera curta após tocar o som antes de iniciar o fade (ajustável)

    [Header("Loading UI")]
    public GameObject painelLoading; // painel contendo barra e texto
    public Slider barraProgresso; // slider 0..1
    public TextMeshProUGUI textoProgressoTMP; // opcional (TextMeshPro). Se não usar TMP, troque por Text.
    public Text textoProgresso; // alternativa sem TMP (arraste se não usar TMP)
    public bool usarTMP = true;

    [Header("Camera / Outros")]
    public MonoBehaviour cameraMovementScript; // arraste aqui o script da câmera pra desativar quando necessário

    private bool carregando = false;

    private void Awake()
    {
        // garantir estados iniciais
        if (painelOpcoes != null) SetActiveImmediate(painelOpcoes, false);
        if (painelCreditos != null) SetActiveImmediate(painelCreditos, false);
        if (painelMenuPrincipal != null) SetActiveImmediate(painelMenuPrincipal, true);

        if (imagemFade != null)
        {
            Color c = imagemFade.color;
            c.a = usarFade ? 0f : 1f;
            imagemFade.color = c;
            imagemFade.raycastTarget = false;
        }

        if (painelLoading != null) SetActiveImmediate(painelLoading, false);
    }

    // ---------- BOTÕES ----------
    public void BotaoJogar()
    {
        if (carregando) return;
        StartCoroutine(TocarSom_FadeECarregar(cenaInicioIndex));
    }

    public void BotaoAbrirOpcoes()
    {
        if (carregando) return;
        StartCoroutine(AbrirPainel(painelOpcoes));
    }

    public void BotaoFecharOpcoes()
    {
        StartCoroutine(FecharPainel(painelOpcoes));
    }

    public void BotaoAbrirCreditos()
    {
        if (carregando) return;
        StartCoroutine(AbrirPainel(painelCreditos));
    }

    public void BotaoFecharCreditos()
    {
        StartCoroutine(FecharPainel(painelCreditos));
    }

    public void BotaoSair()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ---------- PAINÉIS (scale + alpha) ----------
    private IEnumerator AbrirPainel(GameObject painel)
    {
        if (painel == null) yield break;

        // Pause câmera enquanto modal está aberta
        SetCameraMovementEnabled(false);

        painel.SetActive(true);
        CanvasGroup cg = EnsureCanvasGroup(painel);
        // anima de 0 -> 1 alpha e scaleFechar->scaleAbrir
        float t = 0f;
        Vector3 startScale = Vector3.one * scaleFechar;
        Vector3 endScale = Vector3.one * scaleAbrir;
        cg.alpha = 0f;
        painel.transform.localScale = startScale;

        while (t < duracaoAnimPainel)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / duracaoAnimPainel);
            cg.alpha = Mathf.Lerp(0f, 1f, k);
            painel.transform.localScale = Vector3.Lerp(startScale, endScale, Mathf.SmoothStep(0f, 1f, k));
            yield return null;
        }
        cg.alpha = 1f;
        painel.transform.localScale = endScale;
    }

    private IEnumerator FecharPainel(GameObject painel)
    {
        if (painel == null) yield break;

        CanvasGroup cg = EnsureCanvasGroup(painel);
        float t = 0f;
        Vector3 startScale = painel.transform.localScale;
        Vector3 endScale = Vector3.one * scaleFechar;

        while (t < duracaoAnimPainel)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / duracaoAnimPainel);
            cg.alpha = Mathf.Lerp(1f, 0f, k);
            painel.transform.localScale = Vector3.Lerp(startScale, endScale, Mathf.SmoothStep(0f, 1f, k));
            yield return null;
        }
        cg.alpha = 0f;
        painel.transform.localScale = endScale;
        painel.SetActive(false);

        // Se nenhum painel modal está aberto, reativa a câmera
        if (!IsAnyModalOpen()) SetCameraMovementEnabled(true);
    }

    // ---------- TRANSIÇÃO + LOAD ASYNC ----------
    private IEnumerator TocarSom_FadeECarregar(int sceneIndex)
    {
        carregando = true;

        // desativa input e pausa camera
        SetCameraMovementEnabled(false);

        // toca audio de transição
        if (audioTransicao != null)
        {
            audioTransicao.Play();
            // espera um pouco do som pra começar o fade (ajustável)
            float wait = Mathf.Min(waitBeforeFade, audioTransicao.clip != null ? audioTransicao.clip.length : waitBeforeFade);
            yield return new WaitForSecondsRealtime(wait);
        }
        else
        {
            // sem som: pequena espera pra suavizar
            yield return new WaitForSecondsRealtime(0.05f);
        }

        // inicia fade-in (para preto)
        if (usarFade && imagemFade != null)
        {
            imagemFade.raycastTarget = true;
            yield return StartCoroutine(Fade(imagemFade, 0f, 1f, duracaoFade));
        }

        // exibe painel de loading
        if (painelLoading != null) SetActiveImmediate(painelLoading, true);

        // Inicia LoadSceneAsync sem prender o main thread
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneIndex);
        op.allowSceneActivation = false;

        // atualiza progress bar enquanto carrega
        while (!op.isDone)
        {
            float progress = Mathf.Clamp01(op.progress / 0.9f); // op.progress vai até 0.9 antes da ativação
            if (barraProgresso != null) barraProgresso.value = progress;
            if (usarTMP && textoProgressoTMP != null) textoProgressoTMP.text = Mathf.RoundToInt(progress * 100f) + "%";
            if (!usarTMP && textoProgresso != null) textoProgresso.text = Mathf.RoundToInt(progress * 100f) + "%";

            // quando estiver pronto (>= 0.9f), faz um pequeno delay e ativa a cena
            if (op.progress >= 0.9f)
            {
                // opcional: um pequeno delay pra mostrar 100%
                if (barraProgresso != null) barraProgresso.value = 1f;
                if (usarTMP && textoProgressoTMP != null) textoProgressoTMP.text = "100%";
                if (!usarTMP && textoProgresso != null) textoProgresso.text = "100%";

                yield return new WaitForSecondsRealtime(0.25f);
                op.allowSceneActivation = true;
            }

            yield return null;
        }

        // se jamais chegar aqui: garante flag
        carregando = false;
    }

    private IEnumerator Fade(Image img, float from, float to, float duration)
    {
        if (img == null) yield break;
        float t = 0f;
        Color c = img.color;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            c.a = Mathf.Lerp(from, to, Mathf.Clamp01(t / duration));
            img.color = c;
            yield return null;
        }
        c.a = to;
        img.color = c;
        img.raycastTarget = (to > 0.99f);
    }

    // ---------- UTILITÁRIOS ----------
    private CanvasGroup EnsureCanvasGroup(GameObject go)
    {
        CanvasGroup cg = go.GetComponent<CanvasGroup>();
        if (cg == null) cg = go.AddComponent<CanvasGroup>();
        return cg;
    }

    private void SetActiveImmediate(GameObject go, bool ativo)
    {
        if (go == null) return;
        go.SetActive(ativo);
        CanvasGroup cg = go.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.alpha = ativo ? 1f : 0f;
            cg.interactable = ativo;
            cg.blocksRaycasts = ativo;
        }
    }

    private bool IsAnyModalOpen()
    {
        if (painelOpcoes != null && painelOpcoes.activeSelf) return true;
        if (painelCreditos != null && painelCreditos.activeSelf) return true;
        if (painelLoading != null && painelLoading.activeSelf) return true;
        return false;
    }

    private void SetCameraMovementEnabled(bool enabled)
    {
        if (cameraMovementScript != null)
            cameraMovementScript.enabled = enabled;
    }
}
