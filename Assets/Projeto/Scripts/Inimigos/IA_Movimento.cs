using UnityEngine;
using UnityEngine.AI;

public class IA_Movimento : MonoBehaviour
{
    public enum AIState { Parado, Procurando, Perseguindo, Atacando }
    public AIState state = AIState.Parado;

    Animator anim;
    public NavMeshAgent agent;
    public Transform player;
    private PlayerHealth playerHealth;

    [Header("Detecção")]
    public float detectionRange = 15f;
    public float fieldOfView = 90f;
    public float attackRange = 2f;

    [Header("Timers e Movimentação")]
    public float waitTimeIdle = 3f;
    public float walkSpeed = 2f;
    public float runSpeed = 5f;
    public float attackCooldown = 2.5f;

    [Header("Ataque Fatal")]
    public float fatalAttackWindow = 30f;
    private bool firstHitDone = false;
    private float fatalTimer = 0f;
    private bool canAttack = true;

    Vector3 lastSeenPlayerPos;
    float idleTimer;

    void Start()
    {
        anim = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
        playerHealth = player.GetComponent<PlayerHealth>();

        idleTimer = waitTimeIdle;
        agent.speed = walkSpeed;
        state = AIState.Parado;
    }

    void Update()
    {
        float distance = Vector3.Distance(transform.position, player.position);

        // Contagem do ataque fatal
        if (firstHitDone)
        {
            fatalTimer -= Time.deltaTime;
            if (fatalTimer <= 0f)
                firstHitDone = false;
        }

        // Verificar se o player está dentro do campo de visão
        bool canSeePlayer = IsPlayerVisible(distance);

        switch (state)
        {
            case AIState.Parado:
                IdleState();
                if (canSeePlayer) ChangeState(AIState.Perseguindo);
                break;

            case AIState.Procurando:
                SearchState();
                if (canSeePlayer) ChangeState(AIState.Perseguindo);
                break;

            case AIState.Perseguindo:
                ChaseState();
                if (!canSeePlayer)
                {
                    lastSeenPlayerPos = player.position;
                    ChangeState(AIState.Procurando);
                }
                else if (distance <= attackRange)
                    ChangeState(AIState.Atacando);
                break;

            case AIState.Atacando:
                AttackState();
                if (distance > attackRange && canSeePlayer)
                    ChangeState(AIState.Perseguindo);
                break;
        }

        AtualizarAnimacoes();
    }

    // ----------------------- Campo de visão ----------------------- //

    bool IsPlayerVisible(float distance)
    {
        if (distance > detectionRange) return false;

        Vector3 dirToPlayer = (player.position - transform.position).normalized;
        float angle = Vector3.Angle(transform.forward, dirToPlayer);

        if (angle > fieldOfView / 2f) return false;

        return true;
    }

    // ----------------------- Estados ----------------------- //

    void IdleState()
    {
        agent.isStopped = true;
        idleTimer -= Time.deltaTime;
        if (idleTimer <= 0)
        {
            idleTimer = waitTimeIdle;
            ChangeState(AIState.Procurando);
        }
    }

    void SearchState()
    {
        agent.isStopped = false;
        if (!agent.pathPending && agent.remainingDistance <= 1f)
            agent.SetDestination(RandomNavSphere(transform.position, 12f));
    }

    void ChaseState()
    {
        agent.isStopped = false;
        agent.SetDestination(player.position);
    }

    void AttackState()
    {
        agent.isStopped = true;
        transform.LookAt(player.position);

        if (!canAttack) return;
        canAttack = false;

        if (!firstHitDone)
            anim.SetTrigger("Ataque");
        else
            anim.SetTrigger("Ataque 2");

        Invoke(nameof(ResetAttackCooldown), attackCooldown);
    }

    void ResetAttackCooldown() => canAttack = true;

    // ----------------------- Eventos da animação ----------------------- //
    // Colocar no Animation Event nos frames do impacto

    public void HitAnimationEvent()
    {
        if (!firstHitDone)
        {
            playerHealth.ReceberDano(20);
            firstHitDone = true;
            fatalTimer = fatalAttackWindow;
        }
        else
        {
            playerHealth.Morrer();
            firstHitDone = false;
        }
        Debug.Log("Impacto do ataque ocorrido!");
    }

    // ----------------------- Transição de estados ----------------------- //

    public void ChangeState(AIState newState)
    {
        state = newState;

        switch (newState)
        {
            case AIState.Parado:
            case AIState.Procurando:
                agent.speed = walkSpeed;
                break;

            case AIState.Perseguindo:
                agent.speed = runSpeed;
                break;

            case AIState.Atacando:
                agent.speed = 0f;
                break;
        }
    }

    // ----------------------- Animações ----------------------- //

    void AtualizarAnimacoes()
    {
        anim.SetBool("Idle", state == AIState.Parado);
        anim.SetBool("Andando", state == AIState.Procurando);
        anim.SetBool("Correndo", state == AIState.Perseguindo);
    }

    // ----------------------- Utilidades ----------------------- //

    public static Vector3 RandomNavSphere(Vector3 origin, float dist)
    {
        for (int i = 0; i < 20; i++)
        {
            Vector3 randomPos = origin + Random.insideUnitSphere * dist;
            NavMeshHit navHit;
            if (NavMesh.SamplePosition(randomPos, out navHit, dist, NavMesh.AllAreas))
                return navHit.position;
        }
        return origin;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
