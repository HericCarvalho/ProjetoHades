using UnityEngine;
using UnityEngine.AI;

public class IA_Movimento : MonoBehaviour
{
    public enum AIState { Parado, Procurando, Perseguindo, Atacando }
    public AIState state = AIState.Parado;

    public NavMeshAgent agent;
    public Transform player;
    public float detectionRange = 15f;
    public float attackRange = 2f;

    public float waitTimeIdle = 3f;
    Vector3 lastSeenPlayerPos;
    float idleTimer;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        state = AIState.Parado;
        idleTimer = waitTimeIdle;
    }

    void Update()
    {
        float distance = Vector3.Distance(transform.position, player.position);

        // Máquinas de estados (FSM)
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

        // Se chegou ao último local visto → procura outro ponto aleatório
        if (!agent.pathPending && agent.remainingDistance <= 0.5f)
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

        // Aqui você pode colocar animação e dano ao player
        Debug.Log("Atacando o Player!");

        // Exemplo de ataque automático com cooldown → opcional
        // StartCoroutine(GiveDamage());
    }

    // ---------------- UTILS ---------------- //

    public void ChangeState(AIState newState)
    {
        state = newState;
    }

    public static Vector3 RandomNavSphere(Vector3 origin, float dist)
    {
        Vector3 random = Random.insideUnitSphere * dist;
        random += origin;
        NavMesh.SamplePosition(random, out NavMeshHit navHit, dist, NavMesh.AllAreas);
        return navHit.position;
    }

    // Gizmos para visualizar os raios
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}