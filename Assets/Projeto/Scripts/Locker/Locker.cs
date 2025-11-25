using System.Collections;
using UnityEngine;
public class Locker : MonoBehaviour
{
    [Header("Animator da porta")]
    public Animator portaAnimator;         
    public string parametroOpen = "Abrido";

    [Header("Pontos de camera")]
    public Transform cameraPointOutside;   
    public Transform cameraPointInside;    

    [Header("Transição")]
    public float tempoTransicao = 0.6f;
    public AnimationCurve curvaTransicao = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Config")]
    public bool usarAnimador = true;   
    [SerializeField] private KeyCode teclaInteragir = KeyCode.E;

    private bool estaDentro = false;
    private bool isAnimating = false;

    private MovimentaçãoPlayer playerController;
    private Transform playerCamera;
    private Coroutine exitListenerCoroutine = null;

    //PosiçãoOriginal
    private Transform originalCameraParent = null;
    private Vector3 originalCameraLocalPos;
    private Quaternion originalCameraLocalRot;

    private void Start()
    {
        playerController = FindFirstObjectByType<MovimentaçãoPlayer>();
    }

    public void ToggleLocker()
    {
        Debug.Log("[Locker] ToggleLocker chamado. estaDentro=" + estaDentro + " isAnimating=" + isAnimating);
        if (isAnimating) return;
        if (!estaDentro) StartCoroutine(EnterRoutine());
        else StartCoroutine(ExitRoutine());
    }


    private IEnumerator EnterRoutine()
    {
        Debug.Log("[Locker] EnterRoutine start");
        if (!TryCapturePlayer()) { Debug.LogWarning("[Locker] EnterRoutine: player não capturado."); yield break; }

        isAnimating = true;

        // trava e esconde o corpo **antes** da transição
        playerController.SetCanMove(false);
        playerController.SetBodyVisible(false);

        // anima porta abrir
        if (usarAnimador && portaAnimator != null)
            portaAnimator.SetBool(parametroOpen, true);

        // pega câmera do player
        playerCamera = playerController.cameraReferencia;
        if (playerCamera == null) { Debug.LogWarning("[Locker] cameraReferencia não encontrada."); isAnimating = false; yield break; }

        // SALVA estado original da câmera para restaurar depois
        originalCameraParent = playerCamera.parent;
        originalCameraLocalPos = playerCamera.localPosition;
        originalCameraLocalRot = playerCamera.localRotation;

        Vector3 startPos = playerCamera.position;
        Quaternion startRot = playerCamera.rotation;
        Vector3 targetPos = cameraPointInside != null ? cameraPointInside.position : startPos;
        Quaternion targetRot = cameraPointInside != null ? cameraPointInside.rotation : startRot;

        // enquanto dentro, normalmente queremos que o jogador consiga olhar (pitch/yaw)
        // mas a rotação yaw depende do transform do jogador — mantemos SetCanRotate(true)
        playerController.SetCanRotate(true);

        // TRANSIÇÃO: movemos a câmera no espaço mundial para o ponto interno
        float t = 0f;
        while (t < tempoTransicao)
        {
            t += Time.unscaledDeltaTime;
            float k = curvaTransicao.Evaluate(Mathf.Clamp01(t / tempoTransicao));
            playerCamera.position = Vector3.Lerp(startPos, targetPos, k);
            playerCamera.rotation = Quaternion.Slerp(startRot, targetRot, k);
            yield return null;
        }

        // garante posição final
        playerCamera.position = targetPos;
        playerCamera.rotation = targetRot;

        // parent opcional para seguir o locker: parented localmente ao ponto interno
        if (cameraPointInside != null) playerCamera.SetParent(cameraPointInside, true);

        estaDentro = true;
        isAnimating = false;
        Debug.Log("[Locker] EnterRoutine end — dentro");

        // inicia listener que aguarda a tecla para sair
        if (exitListenerCoroutine != null) StopCoroutine(exitListenerCoroutine);
        exitListenerCoroutine = StartCoroutine(ExitListener());
    }



    private IEnumerator ExitRoutine()
    {
        if (isAnimating) yield break;
        if (!TryCapturePlayer()) { Debug.LogWarning("[Locker] ExitRoutine: player não capturado."); yield break; }

        // cancela listener pra não começar duas saídas
        if (exitListenerCoroutine != null)
        {
            StopCoroutine(exitListenerCoroutine);
            exitListenerCoroutine = null;
        }

        isAnimating = true;
        Debug.Log("[Locker] ExitRoutine start");

        // anima porta fechar
        if (usarAnimador && portaAnimator != null)
            portaAnimator.SetBool(parametroOpen, false);

        playerCamera = playerController.cameraReferencia;
        if (playerCamera == null) { Debug.LogWarning("[Locker] cameraReferencia não encontrada."); isAnimating = false; yield break; }

        // PREPARA transição de volta (world space)
        Vector3 startPos = playerCamera.position;
        Quaternion startRot = playerCamera.rotation;

        // target será o transform original (se salvarmos) ou o ponto outside se houver
        Vector3 targetPos;
        Quaternion targetRot;

        if (originalCameraParent != null)
        {
            // se havia parent original, restauramos local target com base nele
            // calculamos world pos/rot correspondentes ao original local transform
            targetPos = originalCameraParent.TransformPoint(originalCameraLocalPos);
            targetRot = originalCameraParent.rotation * originalCameraLocalRot;
        }
        else
        {
            // fallback para cameraPointOutside world transform
            targetPos = cameraPointOutside != null ? cameraPointOutside.position : playerCamera.position;
            targetRot = cameraPointOutside != null ? cameraPointOutside.rotation : playerCamera.rotation;
        }

        // se estava parented ao cameraPointInside, unparent para permitir movimento em world space
        playerCamera.SetParent(null, true);

        float t = 0f;
        while (t < tempoTransicao)
        {
            t += Time.unscaledDeltaTime;
            float k = curvaTransicao.Evaluate(Mathf.Clamp01(t / tempoTransicao));
            playerCamera.position = Vector3.Lerp(startPos, targetPos, k);
            playerCamera.rotation = Quaternion.Slerp(startRot, targetRot, k);
            yield return null;
        }

        // garante posição final
        playerCamera.position = targetPos;
        playerCamera.rotation = targetRot;

        // restaura parent e valores locais originais (se existiam)
        if (originalCameraParent != null)
        {
            playerCamera.SetParent(originalCameraParent, true);
            playerCamera.localPosition = originalCameraLocalPos;
            playerCamera.localRotation = originalCameraLocalRot;
        }
        else if (cameraPointOutside != null && cameraPointOutside.parent != null)
        {
            // se não havia parent original, opcionalmente parent para cameraPointOutside.parent
            playerCamera.SetParent(cameraPointOutside.parent, true);
        }

        // mostrar corpo e restaurar controles
        playerController.SetBodyVisible(true);

        // restaurar rotação/movimentação
        playerController.SetCanRotate(true);
        playerController.SetCanMove(true);

        estaDentro = false;
        isAnimating = false;
        Debug.Log("[Locker] ExitRoutine end — fora");
    }


    private IEnumerator ExitListener()
    {
        // enquanto estiver dentro, aguarda a tecla de interação para sair
        while (estaDentro)
        {
            if (Input.GetKeyDown(teclaInteragir))
            {
                // não tenta sair se já estiver animando
                if (!isAnimating)
                {
                    StartCoroutine(ExitRoutine());
                    yield break;
                }
            }
            yield return null;
        }
    }


    private bool TryCapturePlayer()
    {
        if (playerController != null) return true;
        var pObj = GameObject.FindGameObjectWithTag("Player");
        if (pObj != null) playerController = pObj.GetComponent<MovimentaçãoPlayer>();
        if (playerController != null) return true;
        playerController = FindObjectOfType<MovimentaçãoPlayer>();
        if (playerController != null) return true;
        Debug.LogWarning("[Locker] MovimentaçãoPlayer não encontrado.");
        return false;
    }

}
