using System.Collections;
using UnityEngine;
public class Locker : MonoBehaviour
{
    [Header("Animator da porta")]
    public Animator portaAnimator;            // setar no inspector (Animator do objeto 'Porta')
    public string parametroOpen = "Open";     // nome do bool parameter no Animator

    [Header("Pontos de camera")]
    public Transform cameraPointOutside;      // ponto para câmera quando fora
    public Transform cameraPointInside;       // ponto para câmera quando dentro

    [Header("Transição")]
    public float tempoTransicao = 0.6f;
    public AnimationCurve curvaTransicao = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Config")]
    public bool usarAnimador = true;   


    // estado
    private bool estaDentro = false;
    private bool isAnimating = false;

    // referências runtime
    private MovimentaçãoPlayer playerController;
    private Transform playerCamera;


    private void Start()
    {
        playerController = FindObjectOfType<MovimentaçãoPlayer>();
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

        Vector3 startPos = playerCamera.position;
        Quaternion startRot = playerCamera.rotation;
        Vector3 targetPos = cameraPointInside != null ? cameraPointInside.position : startPos;
        Quaternion targetRot = cameraPointInside != null ? cameraPointInside.rotation : startRot;

        // permite que o jogador olhe enquanto está dentro (ajuste conforme desejar)
        playerController.SetCanRotate(true);

        // realiza a transição suavemente
        float t = 0f;
        while (t < tempoTransicao)
        {
            t += Time.unscaledDeltaTime;
            float k = curvaTransicao.Evaluate(Mathf.Clamp01(t / tempoTransicao));
            playerCamera.position = Vector3.Lerp(startPos, targetPos, k);
            playerCamera.rotation = Quaternion.Slerp(startRot, targetRot, k);
            yield return null;
        }

        playerCamera.position = targetPos;
        playerCamera.rotation = targetRot;

        // parent opcional para seguir o locker
        if (cameraPointInside != null) playerCamera.SetParent(cameraPointInside, true);

        estaDentro = true;
        isAnimating = false;
        Debug.Log("[Locker] EnterRoutine end — dentro");
    }

    private IEnumerator ExitRoutine()
    {
        Debug.Log("[Locker] ExitRoutine start");
        if (!TryCapturePlayer()) { Debug.LogWarning("[Locker] ExitRoutine: player não capturado."); yield break; }

        isAnimating = true;

        // anima porta fechar
        if (usarAnimador && portaAnimator != null)
            portaAnimator.SetBool(parametroOpen, false);

        playerCamera = playerController.cameraReferencia;
        if (playerCamera == null) { Debug.LogWarning("[Locker] cameraReferencia não encontrada."); isAnimating = false; yield break; }

        // preparar retorno
        Vector3 startPos = playerCamera.position;
        Quaternion startRot = playerCamera.rotation;
        Vector3 targetPos = cameraPointOutside != null ? cameraPointOutside.position : startPos;
        Quaternion targetRot = cameraPointOutside != null ? cameraPointOutside.rotation : startRot;

        // se estava parented, unparent para transitar em world space
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

        playerCamera.position = targetPos;
        playerCamera.rotation = targetRot;

        // reparent se necessario (coloca como child do parent original se existir)
        if (cameraPointOutside != null && cameraPointOutside.parent != null)
            playerCamera.SetParent(cameraPointOutside.parent, true);

        // mostrar corpo e restaurar controles
        playerController.SetBodyVisible(true);
        playerController.SetCanRotate(true);
        playerController.SetCanMove(true);

        estaDentro = false;
        isAnimating = false;
        Debug.Log("[Locker] ExitRoutine end — fora");
    }


    // tenta capturar o MovimentaçãoPlayer
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
