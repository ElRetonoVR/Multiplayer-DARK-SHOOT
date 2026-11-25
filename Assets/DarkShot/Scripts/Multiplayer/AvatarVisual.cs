using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

// Aspecto del avatar de red (va en la raiz del prefab PlayerVR).
//  - Jugador propio: oculta cabeza y tronco (la camara queda dentro del casco); manos y pistola quedan visibles.
//  - Todos: tintan el material "M_AvatarAccent" con un color por jugador (paleta de 8 por ActorNumber)
//    usando MaterialPropertyBlock (no se crean materiales nuevos). En modo Equipos el color es el del equipo
//    (Rojo #FF3030, Azul #2F7BFF, sin equipo gris); se revisa cada 0.25 s porque el equipo cambia en la espera.
//  - Todos: el tronco sigue la posicion de la cabeza (bajo ella) y solo gira en Y. Se calcula aqui, en
//    LateUpdate, en cada cliente; por eso el Trunk NO se sincroniza por red.
// Se ejecuta DESPUES de VRLocalToNetworkMapper (que coloca la cabeza del jugador propio en su LateUpdate).
[DefaultExecutionOrder(100)]
public class AvatarVisual : MonoBehaviourPun
{
    [Header("Partes (las asigna el menu 5; si faltan se buscan por nombre)")]
    public Transform cabeza;
    public Transform tronco;

    [Header("Tronco")]
    public Vector3 offsetCuello = new Vector3(0f, -0.19f, -0.07f); // de los ojos a la base del cuello, en el marco del tronco (contrato: 0.19 abajo, 0.07 atras)
    public float suavizadoGiro = 12f;

    [Header("Color de acento")]
    public string nombreMaterialAcento = "M_AvatarAccent";
    public float intensidadEmision = 2.5f;
    public float frecuenciaParpadeo = 6f;
    public float brilloApagado = 0.12f;

    static readonly Color[] Paleta =
    {
        new Color(0.10f, 0.85f, 1.00f), // cian
        new Color(1.00f, 0.20f, 0.75f), // magenta
        new Color(0.55f, 1.00f, 0.10f), // lima
        new Color(1.00f, 0.55f, 0.05f), // naranja
        new Color(1.00f, 0.15f, 0.15f), // rojo
        new Color(1.00f, 0.95f, 0.20f), // amarillo
        new Color(0.60f, 0.35f, 1.00f), // violeta
        new Color(0.20f, 1.00f, 0.55f)  // verde agua
    };

    static readonly int IdBaseColor = Shader.PropertyToID("_BaseColor");
    static readonly int IdEmisionColor = Shader.PropertyToID("_EmissionColor");

    struct RanuraAcento
    {
        public Renderer renderer;
        public int indiceMaterial;
    }

    readonly List<RanuraAcento> acentos = new List<RanuraAcento>();
    readonly List<Renderer> ocultosSiEsMio = new List<Renderer>();
    MaterialPropertyBlock bloque;
    Color colorJugador = Color.white;
    bool parpadeando;
    bool hayGiroPrevio;
    float proximaRevisionColor;
    int ultimoCodigoColor = -1;
    Quaternion giroTronco = Quaternion.identity;

    void Awake()
    {
        bloque = new MaterialPropertyBlock();

        if (cabeza == null) cabeza = BuscarPorNombre(transform, "Head");
        if (tronco == null) tronco = BuscarPorNombre(transform, "Trunk");

        // Renderers de cabeza y tronco (se ocultan en el jugador propio)
        if (cabeza != null) ocultosSiEsMio.AddRange(cabeza.GetComponentsInChildren<Renderer>(true));
        if (tronco != null) ocultosSiEsMio.AddRange(tronco.GetComponentsInChildren<Renderer>(true));

        // Ranuras de material "acento" en todo el avatar
        Renderer[] todos = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < todos.Length; i++)
        {
            Renderer r = todos[i];
            if (r is LineRenderer || r is ParticleSystemRenderer) continue;
            Material[] mats = r.sharedMaterials;
            for (int m = 0; m < mats.Length; m++)
            {
                if (mats[m] != null && mats[m].name.Contains(nombreMaterialAcento))
                {
                    RanuraAcento ranura = new RanuraAcento();
                    ranura.renderer = r;
                    ranura.indiceMaterial = m;
                    acentos.Add(ranura);
                }
            }
        }
    }

    void Start()
    {
        RevisarColor(true);

        if (photonView.IsMine)
        {
            for (int i = 0; i < ocultosSiEsMio.Count; i++)
            {
                if (ocultosSiEsMio[i] != null) ocultosSiEsMio[i].enabled = false;
            }

            // Colliders del avatar propio apagados: siguen a la cabeza y las manos, y la capsula de locomocion
            // del joystick (Meta Interaction SDK) chocaba contra ellos y frenaba al jugador. Solo hacen falta
            // en las copias remotas (ahi es donde le pegan los otros); el arma ya saltaba los propios y las
            // baterias y los Blooms usan distancia.
            Collider[] propios = GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < propios.Length; i++)
            {
                if (propios[i] != null && !propios[i].isTrigger) propios[i].enabled = false;
            }
        }
    }

    // Color por jugador (paleta de 8 segun el ActorNumber del dueno) o por equipo en modo Equipos
    void RevisarColor(bool forzar)
    {
        int actor = photonView.Owner != null ? photonView.Owner.ActorNumber : 1;
        int equipo = MatchProps.SinEquipo;
        bool porEquipo = PhotonNetwork.InRoom && MatchProps.ModoActual == ModoPartida.Equipos;
        if (porEquipo) equipo = MatchProps.GetEquipo(photonView.Owner);

        // Codigo unico de la combinacion: cambia solo si cambia el modo/equipo o el jugador
        int codigo = porEquipo ? 1000 + equipo : actor;
        if (!forzar && codigo == ultimoCodigoColor) return;
        ultimoCodigoColor = codigo;

        colorJugador = porEquipo ? MatchProps.ColorDeEquipo(equipo) : Paleta[Mathf.Abs(actor - 1) % Paleta.Length];
        AplicarColor(1f);
    }

    void Update()
    {
        if (Time.time >= proximaRevisionColor)
        {
            proximaRevisionColor = Time.time + 0.25f;
            RevisarColor(false);
        }

        if (!parpadeando) return;
        float fase = Mathf.Repeat(Time.time * frecuenciaParpadeo, 1f);
        AplicarColor(fase < 0.5f ? 1f : brilloApagado);
    }

    void LateUpdate()
    {
        if (cabeza == null || tronco == null) return;

        // Direccion horizontal hacia la que mira la cabeza (estable aunque mire arriba o abajo)
        Vector3 f = cabeza.forward;
        f.y = 0f;
        if (f.sqrMagnitude < 0.01f)
        {
            // Mirando casi vertical: usar el "arriba" de la cabeza (hacia delante si mira abajo, hacia atras si mira arriba)
            Vector3 u = cabeza.up;
            u.y = 0f;
            f = cabeza.forward.y > 0f ? -u : u;
        }

        if (f.sqrMagnitude > 0.0001f)
        {
            Quaternion objetivo = Quaternion.LookRotation(f.normalized, Vector3.up);
            if (!hayGiroPrevio)
            {
                giroTronco = objetivo;
                hayGiroPrevio = true;
            }
            else
            {
                float k = 1f - Mathf.Exp(-suavizadoGiro * Time.deltaTime);
                giroTronco = Quaternion.Slerp(giroTronco, objetivo, k);
            }
        }

        tronco.SetPositionAndRotation(cabeza.position + giroTronco * offsetCuello, giroTronco);
    }

    // Parpadeo del acento mientras el jugador es invencible (lo llama PlayerHealth en todos los clientes)
    public void SetBlink(bool activo)
    {
        if (activo == parpadeando) return;
        parpadeando = activo;
        if (!activo) AplicarColor(1f);
    }

    void AplicarColor(float brillo)
    {
        if (acentos.Count == 0) return;

        Color baseColor = colorJugador * brillo;
        baseColor.a = 1f;
        Color emision = colorJugador * (intensidadEmision * brillo);
        emision.a = 1f;

        bloque.Clear();
        bloque.SetColor(IdBaseColor, baseColor);
        bloque.SetColor(IdEmisionColor, emision);

        for (int i = 0; i < acentos.Count; i++)
        {
            RanuraAcento a = acentos[i];
            if (a.renderer != null) a.renderer.SetPropertyBlock(bloque, a.indiceMaterial);
        }
    }

    static Transform BuscarPorNombre(Transform raiz, string nombre)
    {
        Transform[] todos = raiz.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < todos.Length; i++)
        {
            if (todos[i].name == nombre) return todos[i];
        }
        return null;
    }
}
