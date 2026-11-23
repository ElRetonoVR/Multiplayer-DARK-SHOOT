using UnityEngine;
using UnityEngine.AI;

// Conecta el NavMeshAgent del Bloom con su Animator.
//  - Cada frame manda la velocidad del agente al parametro float "Speed" (Idle <-> Walk).
//  - PlayAttack() dispara el trigger "Attack".
// Va en la raiz del prefab (junto al NavMeshAgent); el Animator esta en el hijo "Model".
// Si falta algo (Animator, controller o parametros) no lanza errores: simplemente no anima.
[DisallowMultipleComponent]
public class BloomAnimator : MonoBehaviour
{
    public const string ParametroVelocidad = "Speed";
    public const string ParametroAtaque = "Attack";

    [Tooltip("Si queda vacio se busca en los hijos")]
    public Animator animator;
    [Tooltip("Si queda vacio se busca en este objeto")]
    public NavMeshAgent agent;
    [Tooltip("Suavizado del parametro Speed (segundos)")]
    public float suavizado = 0.1f;

    private static readonly int speedHash = Animator.StringToHash(ParametroVelocidad);
    private static readonly int attackHash = Animator.StringToHash(ParametroAtaque);

    private bool tieneSpeed = false;
    private bool tieneAttack = false;
    private bool inicializado = false;

    void Awake()
    {
        Inicializar();
    }

    void Inicializar()
    {
        if (inicializado) return;
        inicializado = true;

        if (animator == null) animator = GetComponentInChildren<Animator>(true);
        if (agent == null) agent = GetComponent<NavMeshAgent>();

        tieneSpeed = false;
        tieneAttack = false;
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            foreach (AnimatorControllerParameter p in animator.parameters)
            {
                if (p.nameHash == speedHash && p.type == AnimatorControllerParameterType.Float) tieneSpeed = true;
                if (p.nameHash == attackHash && p.type == AnimatorControllerParameterType.Trigger) tieneAttack = true;
            }
        }
    }

    void Update()
    {
        if (!tieneSpeed || animator == null || agent == null) return;
        if (!animator.isActiveAndEnabled) return;

        float velocidad = agent.velocity.magnitude;
        animator.SetFloat(speedHash, velocidad, suavizado, Time.deltaTime);
    }

    // Lo llama enemyNav cuando el Bloom golpea / alcanza al jugador
    public void PlayAttack()
    {
        Inicializar();
        if (!tieneAttack || animator == null || !animator.isActiveAndEnabled) return;
        animator.SetTrigger(attackHash);
    }
}
