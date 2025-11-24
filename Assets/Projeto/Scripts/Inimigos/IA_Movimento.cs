using UnityEngine;
using UnityEngine.AI;

public class IA_Movimento : MonoBehaviour
{
    public enum AIState { Parado, Procurando, Perseguindo, Atacando }
    public AIState state = AIState.Parado;

    Animator anim;
    public NavMeshAgent agent;
    public Transform player;

    public float detectionRange = 15f;
    public float attackRange = 2f;
    public float waitTimeIdle = 3f;

    Vector3 lastSeenPlayerPos;
    float idleTimer;

    void Start()
    {
        anim = GetComponentInChildren<Animator>();
        agent = GetComponent<NavMeshAgent>();
        state = AIState.Parado;
        idleTimer = waitTimeIdle;
    }

    void Update()
    {
        float distance = Vector3.Distance(transform.position, player.position);
        // Para quando o NavMeshAgent perde o caminho
        if (agent.pathStatus == NavMeshPathStatus.PathInvalid || agent.pathStatus == NavMeshPathStatus.PathPartial)
        {
            agent.ResetPath(); // limpa o destino
        }

        switch (state)
        {
            case AIState.Parado:
                IdleState();
                if (distance <= detectionRange) ChangeState(AIState.Perseguindo);
                break;

            case AIState.Procurando:
                SearchState();
                if (distance <= detectionRange) ChangeState(AIState.Perseguindo);
                break;

            case AIState.Perseguindo:
                ChaseState();
                if (distance > detectionRange)
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
                if (distance > attackRange) ChangeState(AIState.Perseguindo);
                break;
        }

        AtualizarAnimacoes();
    }

    // ---------------- ESTADOS ---------------- //

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

        // Se chegou ao último destino → pega novo ponto randômico
        if (!agent.pathPending && agent.remainingDistance <=1f)
        {
            Vector3 randomPoint = RandomNavSphere(transform.position, 12f);
            agent.SetDestination(randomPoint);
        }
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

    // ---------------- ANIMAÇÕES ---------------- //

    void AtualizarAnimacoes()
    {
        // limpa triggers de ataque (garante que não travem)
        anim.ResetTrigger("Ataque1");
        anim.ResetTrigger("Ataque2");

        switch (state)
        {
            case AIState.Parado:
                anim.SetBool("Idle", true);
                anim.SetBool("Andando", false);
                anim.SetBool("Correndo", false);
                break;

            case AIState.Procurando:
                anim.SetBool("Idle", false);
                anim.SetBool("Andando", true);
                anim.SetBool("Correndo", false);
                break;

            case AIState.Perseguindo:
                anim.SetBool("Idle", false);
                anim.SetBool("Andando", false);
                anim.SetBool("Correndo", true);
                break;

            case AIState.Atacando:
                anim.SetBool("Idle", false);
                anim.SetBool("Andando", false);
                anim.SetBool("Correndo", false);

                // alterna entre ataque1 e ataque2
                if (Random.value > 0.5f)
                    anim.SetTrigger("Ataque1");
                else
                    anim.SetTrigger("Ataque2");
                break;
        }
    }

    // ---------------- UTILS ---------------- //

    public void ChangeState(AIState newState)
    {
        state = newState;
    }

    public static Vector3 RandomNavSphere(Vector3 origin, float dist)
    {
        for (int i = 0; i < 20; i++)
        {
            Vector3 randomPos = origin + Random.insideUnitSphere * dist;
            NavMeshHit navHit;
            if (NavMesh.SamplePosition(randomPos, out navHit, dist, NavMesh.AllAreas))
                return navHit.position;
        }
        return origin; // se não achar nenhum ponto válido
    }


    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
