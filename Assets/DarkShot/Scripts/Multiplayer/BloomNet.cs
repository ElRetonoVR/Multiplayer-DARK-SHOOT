using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.AI;

// Bloom de red del modo Cooperativo. Los prefabs BloomNet_Base / BloomNet_LabCoat / BloomNet_Engineer (carpeta Resources)
// los crea el menu 8 a partir de Assets/DarkShot/Prefabs/Bloom_*.prefab, sin EnemyFollow ni Health, y con PhotonView +
// PhotonTransformView + este script. Los instancia SOLO el Master con PhotonNetwork.InstantiateRoomObject (BloomSpawner).
//
//  - Master: el NavMeshAgent persigue al jugador mas cercano (cabeza proyectada al piso). A <= 1.6 m ataca cada 1.2 s:
//    TakeDamage(20, -2) al dueno de ese jugador (-2 = "atacante no es un jugador") y un RPC a todos para la animacion.
//  - Resto de clientes: el agente se desactiva y la posicion llega por PhotonTransformView.
//  - Animacion: BloomAnimator (existente) se deja DESACTIVADO (lee el agente); aqui se calcula "Speed" con el
//    desplazamiento real en todos los clientes y se usa BloomAnimator.PlayAttack() para el ataque.
//  - Muere de un disparo: Gun llama PedirDano -> RPC al Master -> avisa al tirador (MatchManager.RPC_BloomEliminado)
//    y PhotonNetwork.Destroy. Si el Master se va, PUN traspasa el objeto al nuevo Master y este retoma el agente.
[DisallowMultipleComponent]
public class BloomNet : MonoBehaviourPun
{
    // Blooms vivos en este cliente (los cuenta BloomSpawner y los usa NetworkManager para elegir spawn)
    public static readonly List<BloomNet> Activos = new List<BloomNet>();

    [Header("IA (solo el Master)")]
    public float intervaloRuta = 0.25f;
    public float distanciaAtaque = 1.6f;
    public float cooldownAtaque = 1.2f;
    public int danoAtaque = 20;
    public float distanciaParada = 1.2f;   // stoppingDistance del agente (el prefab trae 1.5, muy cerca del alcance de ataque)
    public float giroHaciaObjetivo = 360f; // grados por segundo al estar quieto frente al jugador

    [Header("Animacion")]
    public float suavizadoVelocidad = 0.1f;

    const string ParametroVelocidad = "Speed";

    NavMeshAgent agente;
    Animator animador;
    BloomAnimator bloomAnimator;
    bool tieneVelocidad;
    int hashVelocidad;

    bool vivo = true;
    bool destruyendo;
    bool controlMasterAplicado;
    bool eraMaster;
    float proximaRuta;
    float proximoAtaque;
    Vector3 ultimaPos;
    bool hayUltimaPos;

    // Cache compartida de jugadores (evita un FindObjects por Bloom y por frame)
    static PlayerHealth[] cacheJugadores = new PlayerHealth[0];
    static float proximaBusquedaJugadores;

    void OnEnable() { Activos.Add(this); }
    void OnDisable() { Activos.Remove(this); }

    void Awake()
    {
        agente = GetComponent<NavMeshAgent>();
        animador = GetComponentInChildren<Animator>(true);
        bloomAnimator = GetComponentInChildren<BloomAnimator>(true);

        // BloomAnimator lee agent.velocity (inutil en los clientes sin agente): se apaga y Speed se calcula aqui.
        // PlayAttack() sigue funcionando con el componente desactivado.
        if (bloomAnimator != null) bloomAnimator.enabled = false;

        hashVelocidad = Animator.StringToHash(ParametroVelocidad);
        tieneVelocidad = false;
        if (animador != null && animador.runtimeAnimatorController != null)
        {
            AnimatorControllerParameter[] ps = animador.parameters;
            for (int i = 0; i < ps.Length; i++)
            {
                if (ps[i].nameHash == hashVelocidad && ps[i].type == AnimatorControllerParameterType.Float) tieneVelocidad = true;
            }
        }
    }

    void Start()
    {
        if (agente != null) agente.stoppingDistance = distanciaParada;

        // Respiracion humeda en 3D (GDD: "enemy breath"): cada Bloom empieza en un punto distinto del clip
        AudioSource respiracion = GetComponent<AudioSource>();
        if (respiracion != null && respiracion.clip != null)
        {
            respiracion.time = Random.Range(0f, respiracion.clip.length * 0.9f);
            respiracion.pitch = Random.Range(0.85f, 1.15f);
            respiracion.Play();
        }
        AplicarControl(PhotonNetwork.IsMasterClient);
    }

    // ------------------------------------------------------------ Control (Master o cliente)
    void AplicarControl(bool soyMaster)
    {
        controlMasterAplicado = true;
        eraMaster = soyMaster;
        if (agente == null) return;

        if (soyMaster)
        {
            // Colocar el agente en el NavMesh cercano antes de activarlo
            NavMeshHit hit;
            if (NavMesh.SamplePosition(transform.position, out hit, 3f, NavMesh.AllAreas))
            {
                transform.position = hit.position;
                agente.enabled = true;
                if (agente.isOnNavMesh) agente.Warp(hit.position);
            }
            else
            {
                Debug.LogWarning("[BloomNet] No hay NavMesh cerca de " + transform.position + ": hornea el NavMesh con el menu 8.");
                agente.enabled = false;
            }
        }
        else
        {
            agente.enabled = false; // la posicion llega por PhotonTransformView
        }
    }

    void Update()
    {
        bool soyMaster = PhotonNetwork.IsMasterClient;
        if (!controlMasterAplicado || soyMaster != eraMaster) AplicarControl(soyMaster);

        if (soyMaster && !destruyendo)
        {
            MatchManager mm = MatchManager.Instancia;
            // Fuera del combate (o si la sala no es Cooperativa) no debe quedar ningun Bloom
            if (mm != null && (mm.Estado != EstadoPartida.Jugando || MatchProps.ModoActual != ModoPartida.Cooperativo))
            {
                Eliminar();
                return;
            }
            if (vivo) ActualizarIA();
        }

        ActualizarAnimacion();
    }

    // ------------------------------------------------------------ IA (Master)
    void ActualizarIA()
    {
        if (agente == null || !agente.enabled || !agente.isOnNavMesh) return;

        PlayerHealth objetivo = BuscarObjetivo();
        if (objetivo == null) return;

        Vector3 cabeza = objetivo.PosicionCabeza;
        Vector3 haciaJugador = cabeza - transform.position;
        haciaJugador.y = 0f;

        // Ruta: la cabeza proyectada al piso (al nivel del Bloom) y luego al NavMesh mas cercano
        proximaRuta -= Time.deltaTime;
        if (proximaRuta <= 0f)
        {
            proximaRuta = intervaloRuta;
            Vector3 destino = new Vector3(cabeza.x, transform.position.y, cabeza.z);
            NavMeshHit hit;
            if (NavMesh.SamplePosition(destino, out hit, 2.5f, NavMesh.AllAreas)) destino = hit.position;
            agente.SetDestination(destino);
        }

        // Ataque cuerpo a cuerpo
        if (haciaJugador.sqrMagnitude <= distanciaAtaque * distanciaAtaque)
        {
            // Mirar al jugador aunque el agente ya se haya detenido
            if (haciaJugador.sqrMagnitude > 0.0001f)
            {
                Quaternion meta = Quaternion.LookRotation(haciaJugador.normalized, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, meta, giroHaciaObjetivo * Time.deltaTime);
            }

            if (Time.time >= proximoAtaque)
            {
                proximoAtaque = Time.time + cooldownAtaque;
                photonView.RPC("RPC_Atacar", RpcTarget.All);
                if (objetivo.photonView != null && objetivo.photonView.Owner != null)
                {
                    // -2 = atacante que no es un jugador: PlayerHealth no lo filtra por modo y no suma kills
                    objetivo.photonView.RPC("TakeDamage", objetivo.photonView.Owner, danoAtaque, -2);
                }
            }
        }
    }

    // Jugador mas cercano (en horizontal). Todos se consideran vivos: al morir se reaparece al instante.
    PlayerHealth BuscarObjetivo()
    {
        if (Time.time >= proximaBusquedaJugadores)
        {
            proximaBusquedaJugadores = Time.time + 0.5f;
            cacheJugadores = FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None);
        }

        PlayerHealth mejor = null;
        float mejorD = float.MaxValue;
        for (int i = 0; i < cacheJugadores.Length; i++)
        {
            PlayerHealth ph = cacheJugadores[i];
            if (ph == null || ph.photonView == null) continue;
            Vector3 d = ph.PosicionCabeza - transform.position;
            d.y = 0f;
            float m = d.sqrMagnitude;
            if (m < mejorD)
            {
                mejorD = m;
                mejor = ph;
            }
        }
        return mejor;
    }

    [PunRPC]
    void RPC_Atacar()
    {
        if (bloomAnimator != null) bloomAnimator.PlayAttack();
    }

    // ------------------------------------------------------------ Animacion (todos los clientes)
    void ActualizarAnimacion()
    {
        Vector3 actual = transform.position;
        if (!hayUltimaPos)
        {
            ultimaPos = actual;
            hayUltimaPos = true;
            return;
        }
        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        Vector3 delta = actual - ultimaPos;
        delta.y = 0f;
        float velocidad = delta.magnitude / dt;
        ultimaPos = actual;

        if (tieneVelocidad && animador != null && animador.isActiveAndEnabled)
        {
            animador.SetFloat(hashVelocidad, velocidad, suavizadoVelocidad, dt);
        }
    }

    // ------------------------------------------------------------ Dano (lo pide el tirador)
    // Lo llama Gun en el cliente del tirador: el Master decide.
    public void PedirDano(int actorTirador)
    {
        if (!vivo || destruyendo) return;
        photonView.RPC("RPC_Danar", RpcTarget.MasterClient, actorTirador);
    }

    // Se ejecuta en el MASTER: un disparo elimina al Bloom, se avisa al tirador (kill) y se destruye
    [PunRPC]
    void RPC_Danar(int actorTirador)
    {
        if (!PhotonNetwork.IsMasterClient || !vivo || destruyendo) return;
        vivo = false; // el primer disparo gana: no se premia dos veces

        Player tirador = PhotonNetwork.CurrentRoom != null ? PhotonNetwork.CurrentRoom.GetPlayer(actorTirador) : null;
        MatchManager mm = MatchManager.Instancia;
        if (tirador != null && mm != null && mm.photonView != null)
        {
            mm.photonView.RPC("RPC_BloomEliminado", tirador);
        }
        Eliminar();
    }

    void Eliminar()
    {
        if (destruyendo) return;
        destruyendo = true;
        vivo = false;
        if (photonView.IsMine || PhotonNetwork.IsMasterClient) PhotonNetwork.Destroy(gameObject);
    }
}
