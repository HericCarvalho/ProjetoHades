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
    [SerializeField] private AudioClip[] sonsPassosAgachado;
    [SerializeField] private AudioClip[] sonsPassosAndando;
    [SerializeField] private AudioClip[] sonsPassosCorrendo;

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

    // Variáveis de estado para evitar conflitos de Transform
    private float currentHeightY;
    private float currentBobRoll;

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
            currentHeightY = cameraReferencia.localPosition.y;

            // Garante que AlturaEmPé tenha valor válido se não configurado no Inspector
            if (Mathf.Abs(AlturaEmPé) < 0.001f) AlturaEmPé = currentHeightY;
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
        HeadbobAvancado();
    }

    #region Headbob
    private void HeadbobAvancado()
    {
        if (cameraReferencia == null) return;

        // 1. Calcular offsets de Headbob (apenas se houver controle e estiver no chão)
        float bobOffsetX = 0;
        float bobOffsetY = 0;
        float targetRoll = 0;
        Vector3 tremor = Vector3.zero;

        // Usar Input para detectar movimento garante que o headbob funcione 
        // mesmo se a física (RB.linearVelocity) estiver retornando valores baixos/instáveis
        float inputH = Input.GetAxis("Horizontal");
        float inputV = Input.GetAxis("Vertical");
        float inputMagnitude = new Vector2(inputH, inputV).magnitude;

        // Adicionado: && movimentoHabilitado para não tocar passos se o player estiver travado (ex: dentro do Locker)
        if (isGrounded && cameraControlEnabled && movimentoHabilitado && inputMagnitude > 0.1f)
        {
            float currentBobSpeed = velocidadeBob;
            // Ajustar frequência do passo conforme estado
            if (!isCrouching && Input.GetKey(KeyCode.LeftShift) && !isCansado && EstaminaAtual > 0)
                currentBobSpeed *= 1.45f;
            else if (isCrouching)
                currentBobSpeed *= 0.6f;

            contadorMovimento += Time.deltaTime * currentBobSpeed;

            bobOffsetY = Mathf.Sin(contadorMovimento) * intensidadeVertical;
            bobOffsetX = Mathf.Cos(contadorMovimento * 0.5f) * intensidadeHorizontal;
            targetRoll = Mathf.Sin(contadorMovimento * 0.5f) * intensidadeRotacao;

            if ((Input.GetKey(KeyCode.LeftShift) && !isCrouching) || isCansado)
            {
                tremor.x = Random.Range(-intensidadeTremor, intensidadeTremor);
                tremor.y = Random.Range(-intensidadeTremor, intensidadeTremor);
            }

            // Lógica de passos
            if (!passoTocado && bobOffsetY > intensidadeVertical * 0.8f)
            {
                TocarSomPasso();
                passoTocado = true;
            }
            else if (bobOffsetY < 0)
            {
                passoTocado = false;
            }
        }
        else
        {
            contadorMovimento = 0f;
        }

        // 2. Atualizar Roll suavizado
        currentBobRoll = Mathf.Lerp(currentBobRoll, targetRoll, Time.deltaTime * 10f);

        // 3. Aplicar Transform Final (Combinação de Altura Base + Mouse Look + Headbob)
        
        // Posição: Base Inicial X/Z + Offsets + Altura Dinâmica
        Vector3 finalPos = new Vector3(
            posicaoInicialCamera.x + bobOffsetX + tremor.x,
            currentHeightY + bobOffsetY + tremor.y,
            posicaoInicialCamera.z
        );
        
        // Usamos Lerp para a posição final para manter suavidade do tremor/offsets,
        // mas a currentHeightY já é interpolada, então isso adiciona um pouco mais de "peso".
        // Se ficar muito lento, podemos atribuir direto ou aumentar a velocidade do Lerp.
        cameraReferencia.localPosition = Vector3.Lerp(cameraReferencia.localPosition, finalPos, Time.deltaTime * 10f);

        // Rotação: Pitch Instantâneo (Mouse) + Roll Suavizado (Headbob)
        // Importante: Não usamos Slerp no Pitch para evitar input lag no mouse.
        Quaternion finalRot = Quaternion.Euler(RotacaoVertical, 0f, currentBobRoll);
        cameraReferencia.localRotation = finalRot;
    }

    private void TocarSomPasso()
    {
        if (audioPassos == null) return;

        AudioClip[] clipsSelecionados = null;

        // Determinar estado atual para seleção de som
        bool isRunning = !isCrouching && Input.GetKey(KeyCode.LeftShift) && !isCansado && EstaminaAtual > 0;

        if (isCrouching)
        {
            clipsSelecionados = sonsPassosAgachado;
        }
        else if (isRunning)
        {
            clipsSelecionados = sonsPassosCorrendo;
        }
        else
        {
            clipsSelecionados = sonsPassosAndando;
        }

        // Tocar som aleatório da lista selecionada
        if (clipsSelecionados != null && clipsSelecionados.Length > 0)
        {
            int index = Random.Range(0, clipsSelecionados.Length);
            AudioClip clip = clipsSelecionados[index];

            if (clip != null)
            {
                audioPassos.pitch = 0.9f + Random.Range(0f, 0.2f);
                audioPassos.PlayOneShot(clip);
            }
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
        
        // A aplicação da rotação vertical é feita no HeadbobAvancado (ou UpdateCamera) para evitar conflitos
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
        // Apenas atualiza o valor suavizado da altura base, sem setar o transform diretamente
        currentHeightY = Mathf.Lerp(currentHeightY, targetHeight, Time.deltaTime * VelocidadeTransição);
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
