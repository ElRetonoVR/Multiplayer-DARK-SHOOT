using UnityEngine;
using UnityEngine.AI;

public class EnemyFollow : MonoBehaviour
{
    public float intervaloRuta = 0.25f; // Cada cuantos segundos se recalcula la ruta hacia el jugador

    private Transform player;
    private NavMeshAgent agent;
    private float tiempoRuta = 0f;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
    }

    void OnEnable()
    {
        tiempoRuta = 0f; // Recalcula de inmediato al reaparecer desde el pool
    }

    void Update()
    {
        if (player == null || agent == null || !agent.isOnNavMesh) return;

        tiempoRuta -= Time.deltaTime;
        if (tiempoRuta <= 0f)
        {
            agent.SetDestination(player.position);
            tiempoRuta = intervaloRuta;
        }
    }
}
