using UnityEngine;
using UnityEngine.AI;

public class EnemyFollow : MonoBehaviour
{
    public float intervaloRuta = 0.25f; // Cada cuantos segundos se recalcula la ruta hacia el jugador

    [Header("Animacion de ataque (DarkShot)")]
    public float distanciaAtaque = 1.8f;   // Distancia al jugador a la que se reproduce la animacion de ataque
    public float cooldownAtaque = 1.2f;    // Segundos minimos entre animaciones de ataque

    private Transform player;
    private NavMeshAgent agent;
    private float tiempoRuta = 0f;
    private BloomAnimator bloomAnimator;
    private bool bloomAnimatorBuscado = false;
    private float proximoAtaque = 0f;

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
        proximoAtaque = 0f;
    }

    // Reproduce la animacion de ataque si el modelo tiene BloomAnimator (si no, no hace nada)
    void IntentarAtaque()
    {
        if (Time.time < proximoAtaque) return;
        if (!bloomAnimatorBuscado)
        {
            bloomAnimator = GetComponentInChildren<BloomAnimator>(true);
            bloomAnimatorBuscado = true;
        }
        if (bloomAnimator != null)
        {
            bloomAnimator.PlayAttack();
        }
        proximoAtaque = Time.time + cooldownAtaque;
    }

    void OnCollisionEnter(Collision collision)
    {
        // El dano lo aplica PlayerHealthSolo; aqui solo se dispara la animacion
        if (collision.gameObject.CompareTag("Player"))
        {
            IntentarAtaque();
        }
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

        // Ataque cuerpo a cuerpo: animacion cuando esta cerca del jugador
        Vector3 haciaJugador = player.position - transform.position;
        haciaJugador.y = 0f;
        if (haciaJugador.sqrMagnitude <= distanciaAtaque * distanciaAtaque)
        {
            IntentarAtaque();
        }
    }
}
