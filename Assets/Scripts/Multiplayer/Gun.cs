using UnityEngine;
using Photon.Pun;

// Pistola laser del jugador (multijugador). Vive en RightHand del prefab PlayerVR.
// Solo el dueno (photonView.IsMine) lee los botones, apunta y reparte dano; el resto de clientes
// reciben por RPC la linea laser (punto inicial y final ya calculados por el tirador, para que todos
// vean exactamente lo mismo), el sonido y el porcentaje de bateria (con el que cada cliente calcula la
// luz del avatar: asi todos ven que tan iluminado esta cada jugador).
//
// GDD Laser Project: bateria en % (100 -> -5 por disparo = 20 disparos), reserva de baterias (inicial 1, max 3),
// recarga con B gastando 1 de la reserva. Niveles de luz: 100% normal, 50% debil, 10% casi apagada (parpadea),
// 0% apagada (oscuridad total).
public class Gun : MonoBehaviourPun
{
    // El jugador local (null en los remotos). Lo usan el HUD, la reaparicion y las baterias recogibles.
    public static Gun Local { get; private set; }

    public OVRInput.RawButton shootButton;

    [Header("Light Settings (niveles segun el % de bateria)")]
    public Light gunLight;
    public float intensidad100 = 40f;
    public float intensidad50 = 14f;
    public float intensidad10 = 3f;
    public float rango100 = 15f;
    public float rango50 = 10f;
    public float rango10 = 5f;
    public int umbralParpadeo = 10;       // por debajo (y > 0) la luz parpadea
    public float segundosParpadeo = 0.08f;

    [Header("Gun Settings")]
    public float range = 100f;
    public int damage = 20;
    public int costePorDisparo = 5;       // % de bateria por disparo
    public int reservaInicial = 1;
    public int maxReserva = 3;
    public int umbralBateriaBaja = 20;    // % para la alerta sonora

    [Header("References")]
    public Transform firePoint;
    public LineRenderer linePrefab;

    [Header("Audio 3D (opcional: si falta el clip no pasa nada)")]
    public AudioSource sonidoDisparo;     // AudioSource 3D de la pistola; su clip es el sonido de disparo
    public AudioClip clipRecarga;
    public AudioClip clipRecoger;         // al recoger una bateria
    public AudioClip clipParpadeo;        // la luz empieza a parpadear (<= 10%)
    public AudioClip clipBateriaBaja;     // alerta al bajar de 20%

    [Header("Visuals")]
    public float lineDuration = 0.05f;

    [Header("Vibracion")]
    public float vibracionDisparo = 0.05f;

    // Estado (valido en el dueno; en los remotos solo se usa para la luz)
    public int BateriaPct { get; private set; }
    public int Reserva { get; private set; }

    int pctVisual = 100;       // ultimo % recibido (para la luz y para detectar cruces de umbral)
    float proximoParpadeo;
    float factorParpadeo = 1f;

    void Awake()
    {
        if (gunLight == null) gunLight = GetComponent<Light>();
        if (sonidoDisparo == null) sonidoDisparo = GetComponent<AudioSource>();
        BateriaPct = 100;
        Reserva = Mathf.Clamp(reservaInicial, 0, maxReserva);
        pctVisual = 100;
        AplicarLuz();
    }

    void Start()
    {
        if (photonView.IsMine) Local = this;
    }

    void OnDestroy()
    {
        if (Local == this) Local = null;
    }

    void Update()
    {
        ActualizarParpadeo(); // todos los clientes ven parpadear la luz de los demas

        if (!photonView.IsMine) return;

        if (OVRInput.GetDown(shootButton))
        {
            if (PuedeDisparar() && BateriaPct >= costePorDisparo)
            {
                Disparar();
            }
        }

        if (OVRInput.GetDown(OVRInput.RawButton.B))
        {
            Recargar();
        }
    }

    // Para el modo escritorio (DesktopDebugControls): mismas reglas que con el gatillo y el boton B
    public void DispararExterno()
    {
        if (!photonView.IsMine) return;
        if (PuedeDisparar() && BateriaPct >= costePorDisparo) Disparar();
    }

    public void RecargarExterno()
    {
        if (!photonView.IsMine) return;
        Recargar();
    }

    bool PuedeDisparar()
    {
        if (firePoint == null) return false;
        // Muerto: la reaparicion es inmediata, pero por seguridad no se dispara con vida 0
        if (PlayerHealth.Local != null && PlayerHealth.Local.EstaMuerto) return false;
        // Antes de empezar (cuenta regresiva) y en el marcador final no se dispara.
        // Durante la espera de jugadores si, para poder probar.
        if (MatchManager.Instancia != null && !MatchManager.Instancia.PuedeDisparar) return false;
        return true;
    }

    // ------------------------------------------------------------ Disparo
    void Disparar()
    {
        Vector3 origen = firePoint.position;
        Vector3 direccion = firePoint.forward;
        Vector3 fin = origen + direccion * range;

        // RaycastAll ordenado por distancia: asi se saltan los colliders del propio jugador
        // (cabeza, tronco, manos) y los triggers, y se queda con el primer golpe "de verdad".
        PlayerHealth propio = GetComponentInParent<PlayerHealth>();
        RaycastHit[] golpes = Physics.RaycastAll(origen, direccion, range, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        System.Array.Sort(golpes, (a, b) => a.distance.CompareTo(b.distance));

        PlayerHealth objetivo = null;
        BloomNet bloom = null;
        for (int i = 0; i < golpes.Length; i++)
        {
            Collider c = golpes[i].collider;
            if (c == null) continue;

            PlayerHealth ph = c.GetComponentInParent<PlayerHealth>();
            if (ph != null && ph == propio) continue; // collider propio: atravesar

            fin = golpes[i].point;
            objetivo = ph; // null si se golpeo el mapa
            if (ph == null) bloom = c.GetComponentInParent<BloomNet>(); // Cooperativo: un Bloom
            break;
        }

        // Dano a un jugador: RPC solo al dueno del objetivo (el decide si es invencible, si muere, etc.).
        // Cooperativo: no hay dano entre jugadores. Equipos: sin fuego amigo (el laser se ve, pero no se manda dano).
        if (objetivo != null && objetivo.photonView != null && objetivo.photonView.Owner != null && PuedeDanarJugador(objetivo))
        {
            objetivo.photonView.RPC("TakeDamage", objetivo.photonView.Owner, damage, PhotonNetwork.LocalPlayer.ActorNumber);
        }

        // Dano a un Bloom (solo Cooperativo): un disparo lo elimina. El Master lo destruye y avisa a este tirador.
        if (bloom != null && MatchProps.ModoActual == ModoPartida.Cooperativo)
        {
            bloom.PedirDano(PhotonNetwork.LocalPlayer.ActorNumber);
        }

        BateriaPct = Mathf.Max(0, BateriaPct - costePorDisparo);

        // Vibracion corta en la mano derecha
        VibracionVR.Pulso(OVRInput.Controller.RTouch, vibracionDisparo);

        // Linea, sonido y luz para todos (incluido el tirador)
        photonView.RPC("RPC_Shoot", RpcTarget.All, origen, fin, BateriaPct);
    }

    // Reglas de dano entre jugadores segun el modo de la sala
    static bool PuedeDanarJugador(PlayerHealth objetivo)
    {
        ModoPartida modo = MatchProps.ModoActual;
        if (modo == ModoPartida.Cooperativo) return false;
        if (modo == ModoPartida.Equipos && MatchProps.SonAliados(PhotonNetwork.LocalPlayer, objetivo.photonView.Owner)) return false;
        return true;
    }

    [PunRPC]
    void RPC_Shoot(Vector3 desde, Vector3 hasta, int bateriaPct)
    {
        if (linePrefab != null)
        {
            LineRenderer line = Instantiate(linePrefab);
            line.SetPosition(0, desde);
            line.SetPosition(1, hasta);
            Destroy(line.gameObject, lineDuration);
        }

        if (sonidoDisparo != null && sonidoDisparo.clip != null)
        {
            sonidoDisparo.pitch = Random.Range(0.7f, 1.5f);
            sonidoDisparo.PlayOneShot(sonidoDisparo.clip, 0.7f);
        }

        ActualizarNivelVisual(bateriaPct);
    }

    // ------------------------------------------------------------ Bateria
    void Recargar()
    {
        if (Reserva <= 0 || BateriaPct >= 100) return;
        Reserva--;
        BateriaPct = 100;
        photonView.RPC("RPC_SetBateria", RpcTarget.All, BateriaPct, true);
    }

    // Bateria al 100% sin sonido de recarga (reaparicion). Solo debe llamarla el dueno.
    public void RellenarBateria()
    {
        BateriaPct = 100;
        photonView.RPC("RPC_SetBateria", RpcTarget.All, BateriaPct, false);
    }

    // Suma baterias de reserva (recoger una bateria del mapa). Devuelve false si ya estaba llena.
    public bool AgregarReserva(int cantidad)
    {
        if (Reserva >= maxReserva) return false;
        Reserva = Mathf.Min(maxReserva, Reserva + cantidad);
        Sonar(clipRecoger);
        return true;
    }

    [PunRPC]
    void RPC_SetBateria(int bateriaPct, bool conSonido)
    {
        if (conSonido) Sonar(clipRecarga);
        ActualizarNivelVisual(bateriaPct);
    }

    // ------------------------------------------------------------ Luz por niveles
    // Guarda el % y dispara las alertas al cruzar umbrales (se oyen en 3D para todos)
    void ActualizarNivelVisual(int pct)
    {
        int previo = pctVisual;
        pctVisual = Mathf.Clamp(pct, 0, 100);

        if (previo > umbralBateriaBaja && pctVisual <= umbralBateriaBaja && pctVisual > 0) Sonar(clipBateriaBaja);
        if (previo > umbralParpadeo && pctVisual <= umbralParpadeo && pctVisual > 0) Sonar(clipParpadeo);

        factorParpadeo = 1f;
        AplicarLuz();
    }

    // Parpadeo irregular mientras 0 < % <= umbral
    void ActualizarParpadeo()
    {
        if (gunLight == null) return;
        if (pctVisual <= 0 || pctVisual > umbralParpadeo) return;
        if (Time.time < proximoParpadeo) return;

        proximoParpadeo = Time.time + segundosParpadeo * Random.Range(0.5f, 1.5f);
        factorParpadeo = Random.value < 0.4f ? Random.Range(0f, 0.25f) : 1f;
        AplicarLuz();
    }

    void AplicarLuz()
    {
        if (gunLight == null) return;

        if (pctVisual <= 0)
        {
            gunLight.enabled = false; // oscuridad total
            return;
        }
        gunLight.enabled = true;

        float intensidad = Nivel(pctVisual, 0f, intensidad10, intensidad50, intensidad100);
        float rango = Nivel(pctVisual, 0f, rango10, rango50, rango100);
        gunLight.intensity = intensidad * factorParpadeo;
        gunLight.range = rango;
    }

    // Interpolacion por tramos: 0% -> v0, 10% -> v10, 50% -> v50, 100% -> v100
    static float Nivel(float pct, float v0, float v10, float v50, float v100)
    {
        if (pct <= 10f) return Mathf.Lerp(v0, v10, pct / 10f);
        if (pct <= 50f) return Mathf.Lerp(v10, v50, (pct - 10f) / 40f);
        return Mathf.Lerp(v50, v100, (pct - 50f) / 50f);
    }

    void Sonar(AudioClip clip)
    {
        if (clip == null || sonidoDisparo == null) return;
        sonidoDisparo.PlayOneShot(clip);
    }
}
