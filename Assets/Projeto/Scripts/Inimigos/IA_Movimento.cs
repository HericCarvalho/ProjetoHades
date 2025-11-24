using UnityEngine;
using UnityEngine.AI;

public class IA_Movimento : MonoBehaviour
{
    public enum AIState { Parado, Procurando, Perseguindo, Atacando }
    public AIState state = AIState.Parado;

    Animator anim;
    public NavMeshAgent agent;
    public Transform player;

    [Header("Detecção | Audição e Visão")]
    public float detectionRange = 15f;
    public float viewAngle = 110f;
    public LayerMask playerMask;
    public LayerMask obstacleMask;

    [Header("Ataque")]
    public float attackRange = 2f;
    public float waitTimeIdle = 3f;

    [Header("Velocidades")]
    public float walkSpeed = 2f;
    public float runSpeed = 5f;
    public float sprintBurstSpeed = 8f;
    public float sprintBurstDuration = 1.8f;

    Vector3 lastSeenPlayerPos;
    float idleTimer;

    bool sprintBurstActive = false;
    float sprintBurstTimer = 0f;

    void Start()
    {
        anim = GetComponentInChildren<Animator>();
        agent = GetComponent<NavMeshAgent>();
        state = AIState.Parado;
        idleTimer = waitTimeIdle;
        agent.speed = walkSpeed;
    }

    void Update()
    {
        float distance = Vector3.Distance(transform.position, player.position);

        
        if (sprintBurstActive)
        {
            sprintBurstTimer -= Time.deltaTime;
            if (sprintBurstTimer <= 0)
            {
                sprintBurstActive = false;
                agent.speed = runSpeed;
            }
        }

        
        bool podeVerPlayer = CanSeePlayer();

        switch (state)
        {
            case AIState.Parado:
                IdleState();
                if (podeVerPlayer) ActivateChase();
                break;

            case AIState.Procurando:
                SearchState();
                if (podeVerPlayer) ActivateChase();
                break;

            case AIState.Perseguindo:
                ChaseState();
                if (!podeVerPlayer && distance > detectionRange)
                {
                    lastSeenPlayerPos = player.position;
                    ChangeState(AIState.Procurando);
                }
                else if (distance <= attackRange)
                {
                    ChangeState(AIState.Atacando);
                }
                break;

            case AIState.Atacando:
                AttackState();
                if (distance > attackRange && podeVerPlayer)
                    ChangeState(AIState.Perseguindo);
                break;
        }

        AtualizarAnimacoes();
    }

   
    bool CanSeePlayer()
    {
        float dist = Vector3.Distance(transform.position, player.position);
        if (dist > detectionRange) return false;

        Vector3 dirToPlayer = (player.position - transform.position).normalized;

        if (Vector3.Angle(transform.forward, dirToPlayer) > viewAngle / 2)
            return false;

       
        if (Physics.Raycast(transform.position + Vector3.up, dirToPlayer, out RaycastHit hit, detectionRange, obstacleMask))
        {
            if (!hit.collider.CompareTag("Player"))
                return false;
        }

        return true;
    }

   
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

    void ActivateChase()
    {
        ChangeState(AIState.Perseguindo);

        
        agent.speed = sprintBurstSpeed;
        sprintBurstActive = true;
        sprintBurstTimer = sprintBurstDuration;
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
    }

    
    void AtualizarAnimacoes()
    {
        anim.ResetTrigger("Ataque1");
        anim.ResetTrigger("Ataque2");

        anim.SetBool("Idle", state == AIState.Parado);
        anim.SetBool("Andando", state == AIState.Procurando);
        anim.SetBool("Correndo", state == AIState.Perseguindo);

        if (state == AIState.Atacando)
        {
            if (Random.value > 0.5f) anim.SetTrigger("Ataque1");
            else anim.SetTrigger("Ataque2");
        }
    }

   
    public void ChangeState(AIState newState)
    {
        state = newState;

        switch (newState)
        {
            case AIState.Parado:
            case AIState.Procurando:
                agent.speed = walkSpeed;
                sprintBurstActive = false;
                break;

            case AIState.Perseguindo:
                if (!sprintBurstActive) agent.speed = runSpeed;
                break;

            case AIState.Atacando:
                agent.speed = 0f;
                break;
        }
    }

    
    public static Vector3 RandomNavSphere(Vector3 origin, float dist)
    {
        for (int i = 0; i < 20; i++)
        {
            Vector3 randomPos = origin + Random.insideUnitSphere * dist;
            if (NavMesh.SamplePosition(randomPos, out NavMeshHit navHit, dist, NavMesh.AllAreas))
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

       
        Vector3 left = Quaternion.Euler(0, -viewAngle / 2, 0) * transform.forward;
        Vector3 right = Quaternion.Euler(0, viewAngle / 2, 0) * transform.forward;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, transform.position + left * detectionRange);
        Gizmos.DrawLine(transform.position, transform.position + right * detectionRange);
    }
}