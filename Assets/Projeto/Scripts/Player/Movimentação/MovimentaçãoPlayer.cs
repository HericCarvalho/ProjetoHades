using UnityEngine;
using UnityEngine.Events;

public class MovimentaçãoPlayer : MonoBehaviour
{
    [Header("Movimento")]
    [SerializeField] private float velocidade;
    [SerializeField] private float velocidadeAgachado;
    [SerializeField] private float VelocidadeCorrendo;
    [SerializeField] private float velocidadeCansado;
    [SerializeField] private float PuloForca;
    [SerializeField] private float MouseSensibilidade;

    [Header("Câmera")]
    public Transform cameraReferencia;
    [SerializeField] private float AlturaEmPé;
    [SerializeField] private float AlturaAgachado;
    [SerializeField] private float VelocidadeTransição;

    [Header("Headbob (balanço da câmera)")]
    [SerializeField] private float intensidadeVertical = 0.05f;
    [SerializeField] private float intensidadeHorizontal = 0.03f;
    [SerializeField] private float intensidadeRotacao = 1.5f;
    [SerializeField] private float velocidadeBob = 6f;
    [SerializeField] private float intensidadeTremor = 0.01f;
    private float contadorMovimento;
    private Vector3 posicaoInicialCamera;
    private Quaternion rotacaoInicialCamera;
    private bool passoTocado;

    [Header("Sons de passos")]
    [SerializeField] private AudioSource audioPassos;
    [SerializeField] private AudioClip somPasso;

    [Header("Stamina")]
    [SerializeField] public float MaxEstamina;
    [SerializeField] private float RegeneracaoEstamina;
    [SerializeField] private float EstaminaGasta;

    [Header("Interação")]
    [SerializeField] private float alcanceInteracao = 2f;
    [SerializeField] private KeyCode teclaInteragir = KeyCode.E;

    private float EstaminaAtual;
    private bool podeCorrer = true;
    private bool isCrouching = false;
    private bool isCansado = false;

    private float RotacaoVertical;
    private Rigidbody RB;
    private bool isGrounded = true;

    private bool movimentoHabilitado = true;
    private bool rotacaoHabilitada = true;

    // NOVO: controle global da lógica de câmera (rotacao/headbob/altura)
    private bool cameraControlEnabled = true;

    private void Start()
    {
        RB = GetComponent<Rigidbody>();
        Cursor.lockState = CursorLockMode.Locked;
        EstaminaAtual = MaxEstamina;

        if (cameraReferencia != null)
        {
            posicaoInicialCamera = cameraReferencia.localPosition;
            rotacaoInicialCamera = cameraReferencia.localRotation;
        }

        if (audioPassos == null)
            Debug.LogWarning("AudioSource de passos não atribuído!");
    }

    private void Update()
    {
        RotacaoMouse();
        PuloAgachar();
        SistemaStamina();
        AlturaCamera();
        //DetectarInteracao();
        //HeadbobAvancado();
    }

    #region Headbob
    private void HeadbobAvancado()
    {
        // respeita bloqueio de câmera
        if (!isGrounded || cameraReferencia == null || !cameraControlEnabled) return;

        Vector3 horizontalVel = new Vector3(RB.linearVelocity.x, 0, RB.linearVelocity.z);
        float velocidadeAtual = horizontalVel.magnitude;

        if (velocidadeAtual > 0.1f)
        {
            contadorMovimento += Time.deltaTime * velocidadeBob;

            float offsetY = Mathf.Sin(contadorMovimento) * intensidadeVertical;
            float offsetX = Mathf.Cos(contadorMovimento * 0.5f) * intensidadeHorizontal;
            float rotZ = Mathf.Sin(contadorMovimento * 0.5f) * intensidadeRotacao;

            Vector3 tremor = Vector3.zero;
            if ((Input.GetKey(KeyCode.LeftShift) && !isCrouching) || isCansado)
            {
                tremor.x = Random.Range(-intensidadeTremor, intensidadeTremor);
                tremor.y = Random.Range(-intensidadeTremor, intensidadeTremor);
            }

            Vector3 targetPos = posicaoInicialCamera + new Vector3(offsetX, offsetY, 0) + tremor;
            cameraReferencia.localPosition = Vector3.Lerp(cameraReferencia.localPosition, targetPos, Time.deltaTime * 10f);

            Quaternion targetRot = rotacaoInicialCamera * Quaternion.Euler(0, 0, rotZ);
            cameraReferencia.localRotation = Quaternion.Slerp(cameraReferencia.localRotation, targetRot, Time.deltaTime * 10f);

            if (!passoTocado && offsetY > intensidadeVertical * 0.8f)
            {
                TocarSomPasso();
                passoTocado = true;
            }
            else if (offsetY < 0)
            {
                passoTocado = false;
            }
        }
        else
        {
            cameraReferencia.localPosition = Vector3.Lerp(cameraReferencia.localPosition, posicaoInicialCamera, Time.deltaTime * 5f);
            cameraReferencia.localRotation = Quaternion.Slerp(cameraReferencia.localRotation, rotacaoInicialCamera, Time.deltaTime * 5f);
            contadorMovimento = 0f;
        }
    }

    private void TocarSomPasso()
    {
        if (audioPassos != null && somPasso != null)
        {
            audioPassos.pitch = 0.9f + Random.Range(0f, 0.2f);
            audioPassos.PlayOneShot(somPasso);
        }
    }
    #endregion

    #region Movimento e câmera
    private void RotacaoMouse()
    {
        // respeita controle de rotação e bloqueio global de câmera
        if (!rotacaoHabilitada || !cameraControlEnabled) return;

        float mouseX = Input.GetAxis("Mouse X") * MouseSensibilidade;
        transform.Rotate(Vector3.up * mouseX);

        float mouseY = Input.GetAxis("Mouse Y") * MouseSensibilidade;
        RotacaoVertical -= mouseY;
        RotacaoVertical = Mathf.Clamp(RotacaoVertical, -80f, 80f);
        if (cameraReferencia != null)
            cameraReferencia.localRotation = Quaternion.Euler(RotacaoVertical, 0f, cameraReferencia.localRotation.eulerAngles.z);
    }

    private void PuloAgachar()
    {
        if (Input.GetKeyDown(KeyCode.LeftControl))
            isCrouching = !isCrouching;
    }

    private void AlturaCamera()
    {
        if (!cameraControlEnabled) return;

        float targetHeight = isCrouching ? AlturaAgachado : AlturaEmPé;
        Vector3 cameraPos = cameraReferencia.localPosition;
        cameraPos.y = Mathf.Lerp(cameraPos.y, targetHeight, Time.deltaTime * VelocidadeTransição);
        cameraReferencia.localPosition = cameraPos;
    }
    #endregion

    #region Stamina
    private void SistemaStamina()
    {
        bool tentandoCorrer = Input.GetKey(KeyCode.LeftShift) && !isCrouching && !isCansado;

        if (tentandoCorrer)
        {
            EstaminaAtual -= EstaminaGasta * Time.deltaTime;
            if (EstaminaAtual <= 0f)
            {
                EstaminaAtual = 0f;
                isCansado = true;
                podeCorrer = false;
            }
        }
        else
        {
            EstaminaAtual += RegeneracaoEstamina * Time.deltaTime;
            if (EstaminaAtual >= MaxEstamina)
            {
                EstaminaAtual = MaxEstamina;
                isCansado = false;
                podeCorrer = true;
            }
        }
    }
    #endregion

    #region Movimento físico
    private void FixedUpdate()
    {
        if (!movimentoHabilitado) return;

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        float currentSpeed;
        if (isCrouching)
            currentSpeed = velocidadeAgachado;
        else if (Input.GetKey(KeyCode.LeftShift) && !isCansado && EstaminaAtual > 0)
            currentSpeed = VelocidadeCorrendo;
        else if (isCansado)
            currentSpeed = velocidadeCansado;
        else
            currentSpeed = velocidade;

        Vector3 move = transform.right * x + transform.forward * z;
        RB.MovePosition(RB.position + move * currentSpeed * Time.fixedDeltaTime);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.contacts.Length > 0 && collision.contacts[0].normal.y > 0.7f)
            isGrounded = true;
    }
    #endregion

    #region Interação
    // (mantido comentado)
    #endregion

    #region API pública para outros scripts (Locker, HUD, etc.)
    public void SetCanMove(bool canMove)
    {
        movimentoHabilitado = canMove;

        if (!canMove && RB != null)
        {
            RB.linearVelocity = Vector3.zero;
            RB.angularVelocity = Vector3.zero;
        }
    }

    public void SetCanRotate(bool canRotate)
    {
        rotacaoHabilitada = canRotate;
    }

    public void SetBodyVisible(bool visible)
    {
        Renderer[] rends = GetComponentsInChildren<Renderer>(true);
        int count = 0;
        foreach (var r in rends)
        {
            if (cameraReferencia != null && r.gameObject == cameraReferencia.gameObject) continue;
            r.enabled = visible;
            count++;
        }
        Debug.Log($"[MovimentaçãoPlayer] SetBodyVisible({visible}) — renderers afetados: {count}");
    }

    /// <summary>
    /// Bloqueia/permite que o MovimentaçãoPlayer atualize a câmera (rotacao, headbob, altura).
    /// Use para interações que movem a câmera por cena (lockers, puzzles, etc.).
    /// </summary>
    public void SetCameraControl(bool enable)
    {
        cameraControlEnabled = enable;
        if (!enable && cameraReferencia != null)
        {
            // assegura que as referências iniciais não causem jumps enquanto interação está ativa
            posicaoInicialCamera = cameraReferencia.localPosition;
            rotacaoInicialCamera = cameraReferencia.localRotation;
        }
    }
    #endregion

    public float GetCurrentStamina() => EstaminaAtual;
    public bool IsCansado() => isCansado;
}
