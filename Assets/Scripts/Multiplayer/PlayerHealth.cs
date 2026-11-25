using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

// Vida del jugador. La decide el DUENO de cada avatar: el tirador envia un RPC "TakeDamage" al dueno
// del objetivo, y el dueno aplica invencibilidad, curacion, muerte y reaparicion.
//
// Reglas (GDD): 5 s de invencibilidad tras reaparecer, tras 8 s sin dano cura 20 por segundo,
// al morir suma 1 muerte propia, avisa al atacante para que sume su kill y reaparece en el spawn
// mas lejano de los demas con vida y bateria al 100%.
// Marcador (propiedades de jugador de Photon, cada jugador escribe SOLO las suyas):
//   "h" impactos (+10 puntos), "k" kills (+100 puntos), "d" muertes (0), "s" score.
// La victima avisa al atacante por RPC y el atacante suma lo suyo.
// Al morir tambien pide al Master soltar 1-3 baterias recogibles donde murio.
public class PlayerHealth : MonoBehaviourPunCallbacks
{
    // El jugador local (null en los remotos)
    public static PlayerHealth Local { get; private set; }

    public int maxHealth = 100;
    public int currentHealth;
    public int deaths = 0;
    public int kills = 0;
    public Vector3 LoserLounge = new Vector3(1, 13, 0); // HEREDADO: ya no se usa (se mantiene para no romper el prefab)

    [Header("Reglas")]
    public float segundosInvencible = 5f;
    public float segundosSinDanoParaCurar = 8f;
    public float curacionPorSegundo = 20f;

    [Header("Feedback")]
    public float vibracionImpacto = 0.2f;
    public AudioClip clipImpacto;   // opcional: se oye en 3D en la cabeza de quien recibe el impacto
    public AudioClip clipMuerte;    // opcional

    private ChangeUI ui;
    private JumpscareBloom susto; // susto del Bloom en Cooperativo (objeto de escena que crea el menu 8)
    private AvatarVisual avatar;
    private float ultimoDano = -999f;
    private float curacionAcumulada = 0f;
    private float invencibleHasta = 0f;

    public bool EstaMuerto { get { return currentHealth <= 0; } }
    public bool Invencible { get { return Time.time < invencibleHasta; } }
    public float SegundosInvencibleRestantes { get { return Mathf.Max(0f, invencibleHasta - Time.time); } }

    // Posicion de la cabeza del avatar (la usa el NetworkManager para elegir spawn y la estacion de baterias)
    public Vector3 PosicionCabeza
    {
        get
        {
            if (avatar != null && avatar.cabeza != null) return avatar.cabeza.position;
            return transform.position;
        }
    }

    void Start()
    {
        currentHealth = maxHealth;
        avatar = GetComponent<AvatarVisual>();

        if (photonView.IsMine)
        {
            Local = this;
            ui = FindFirstObjectByType<ChangeUI>();
            ActualizarUI();
        }
    }

    void OnDestroy()
    {
        if (Local == this) Local = null;
    }

    void Update()
    {
        // Todos los clientes: parpadeo del acento del avatar mientras es invencible
        if (avatar != null) avatar.SetBlink(Invencible);

        if (!photonView.IsMine) return;

        // Curacion: 20/s tras 8 s sin recibir dano
        if (currentHealth > 0 && currentHealth < maxHealth && Time.time - ultimoDano >= segundosSinDanoParaCurar)
        {
            curacionAcumulada += curacionPorSegundo * Time.deltaTime;
            int puntos = Mathf.FloorToInt(curacionAcumulada);
            if (puntos > 0)
            {
                currentHealth = Mathf.Min(currentHealth + puntos, maxHealth);
                curacionAcumulada -= puntos;
                ActualizarUI();
            }
        }
    }

    // ------------------------------------------------------------ Dano
    // Un solo RPC "TakeDamage" (sin sobrecargas, para que PUN no tenga dos metodos con el mismo nombre)
    [PunRPC]
    public void TakeDamage(int damage, int attackerActor)
    {
        if (!photonView.IsMine) return; // solo el dueno decide
        if (damage <= 0 || EstaMuerto) return;
        if (Invencible) return;
        if (MatchManager.Instancia != null && !MatchManager.Instancia.DanoPermitido) return;
        if (PhotonNetwork.LocalPlayer != null && attackerActor == PhotonNetwork.LocalPlayer.ActorNumber) return; // no se puede danar a si mismo

        // v2 (por seguridad; Gun ya no manda estos RPC): Cooperativo sin dano entre jugadores, Equipos sin fuego amigo.
        // Los Blooms atacan con attackerActor = -2 (no es un jugador) y siempre danan.
        if (attackerActor > 0 && PhotonNetwork.CurrentRoom != null)
        {
            ModoPartida modo = MatchProps.ModoActual;
            if (modo == ModoPartida.Cooperativo) return;
            if (modo == ModoPartida.Equipos && MatchProps.SonAliados(PhotonNetwork.LocalPlayer, PhotonNetwork.CurrentRoom.GetPlayer(attackerActor))) return;
        }

        currentHealth -= damage;
        ultimoDano = Time.time;
        curacionAcumulada = 0f;
        ActualizarUI();

        // Feedback local: vibracion en ambas manos y sonido
        VibracionVR.Pulso(OVRInput.Controller.LTouch, vibracionImpacto, 0.8f);
        VibracionVR.Pulso(OVRInput.Controller.RTouch, vibracionImpacto, 0.8f);
        if (clipImpacto != null) AudioSource.PlayClipAtPoint(clipImpacto, PosicionCabeza);

        // Golpe de un Bloom (Cooperativo): el susto del GDD (pantalla negra, sonido cortado y su cara muy cerca).
        // Solo en este cliente: TakeDamage corre en el dueno del avatar.
        if (attackerActor == -2)
        {
            if (susto == null) susto = FindFirstObjectByType<JumpscareBloom>(FindObjectsInactive.Include);
            if (susto != null && susto.isActiveAndEnabled) susto.Activar();
        }

        // El atacante suma su impacto (+10)
        Player atacante = BuscarJugador(attackerActor);
        if (atacante != null) photonView.RPC("RPC_RegistrarImpacto", atacante);

        if (currentHealth <= 0)
        {
            Morir(attackerActor);
        }
    }

    void Morir(int atacanteActor)
    {
        // Muertes propias (se parte de la propiedad por si el marcador se reinicio)
        deaths = MatchProps.GetInt(PhotonNetwork.LocalPlayer, MatchProps.Muertes) + 1;
        MatchProps.SetLocalInt(MatchProps.Muertes, deaths);

        if (clipMuerte != null) AudioSource.PlayClipAtPoint(clipMuerte, PosicionCabeza);

        // Avisar al atacante para que sume SU kill (cada jugador escribe solo sus propiedades).
        // El RPC se ejecuta en el cliente del atacante sobre la copia de este avatar.
        Player atacante = BuscarJugador(atacanteActor);
        if (atacante != null) photonView.RPC("RPC_RegistrarKill", atacante);

        // Pedir al Master que suelte 1-3 baterias recogibles donde murio (a la altura de la cintura)
        SoltarBaterias();

        ReaparecerLocal(true);
    }

    Player BuscarJugador(int actor)
    {
        if (actor <= 0 || PhotonNetwork.CurrentRoom == null) return null;
        if (PhotonNetwork.LocalPlayer != null && actor == PhotonNetwork.LocalPlayer.ActorNumber) return null;
        return PhotonNetwork.CurrentRoom.GetPlayer(actor);
    }

    void SoltarBaterias()
    {
        MatchManager mm = MatchManager.Instancia;
        if (mm == null || mm.photonView == null) return;
        if (MatchProps.ModoActual == ModoPartida.Cooperativo) return; // en Cooperativo no se sueltan pilas al morir

        Vector3 pos = PosicionCabeza;
        OVRCameraRig rig = FindFirstObjectByType<OVRCameraRig>();
        if (rig != null) pos.y = rig.transform.position.y + 0.9f; // el rig esta a nivel del piso
        mm.photonView.RPC("RPC_SoltarBaterias", RpcTarget.MasterClient, pos);
    }

    // Se ejecutan en el cliente del ATACANTE
    [PunRPC]
    void RPC_RegistrarImpacto()
    {
        MatchProps.SumarLocalInt(MatchProps.Impactos, 1);
        MatchProps.SumarLocalInt(MatchProps.Score, MatchProps.PuntosPorImpacto);
    }

    [PunRPC]
    void RPC_RegistrarKill()
    {
        int k = MatchProps.GetInt(PhotonNetwork.LocalPlayer, MatchProps.Kills) + 1;
        MatchProps.SetLocalInt(MatchProps.Kills, k);
        MatchProps.SumarLocalInt(MatchProps.Score, MatchProps.PuntosPorKill);
        if (Local != null) Local.kills = k;
    }

    // ------------------------------------------------------------ Reaparicion
    // Vida y bateria al 100%, mueve el OVRCameraRig al spawn mas lejano de los demas y (opcional) 5 s de invencibilidad.
    public void ReaparecerLocal(bool conInvencibilidad)
    {
        if (!photonView.IsMine) return;

        // Elegir el spawn ANTES de moverse (se evita el sitio donde se esta ahora).
        // Modo caminando: no se mueve al jugador (reaparece donde esta parado en la sala real).
        NetworkManager nm = NetworkManager.Instancia;
        if (nm != null && !MatchProps.ModoCaminando)
        {
            Transform spawn = nm.ObtenerMejorSpawn(PosicionCabeza);
            if (spawn != null) nm.TeletransportarRig(spawn);
        }

        currentHealth = maxHealth;
        curacionAcumulada = 0f;
        ultimoDano = Time.time;
        ActualizarUI();

        Gun gun = Gun.Local;
        if (gun != null) gun.RellenarBateria();

        if (conInvencibilidad) IniciarInvencibilidad(segundosInvencible);
    }

    // Al empezar el combate (lo llama MatchManager): sin invencibilidad. En Equipos usa la mitad del mapa del equipo;
    // en Equipos y Cooperativo reparte los spawns por orden dentro del equipo/sala para que nadie coincida.
    public void ReaparecerAlInicioDePartida()
    {
        if (!photonView.IsMine) return;
        NetworkManager nm = NetworkManager.Instancia;
        bool caminando = MatchProps.ModoCaminando;
        if (nm == null || (!caminando && MatchProps.ModoActual == ModoPartida.TodosContraTodos))
        {
            ReaparecerLocal(false);
            return;
        }

        // Modo caminando: NO se mueve al jugador. Ya se coloco en su punto de origen al entrar a la escena
        // (NetworkManager.CrearJugador); moverlo otra vez descuadraria su posicion con la de la sala real.
        if (!caminando)
        {
            Transform spawn = nm.ObtenerSpawnInicialPartida();
            if (spawn != null) nm.TeletransportarRig(spawn);
        }

        currentHealth = maxHealth;
        curacionAcumulada = 0f;
        ultimoDano = Time.time;
        ActualizarUI();
        if (Gun.Local != null) Gun.Local.RellenarBateria();
    }

    void IniciarInvencibilidad(float segundos)
    {
        invencibleHasta = Time.time + segundos;
        photonView.RPC("RPC_Invencible", RpcTarget.All, segundos); // para que todos vean el parpadeo
    }

    [PunRPC]
    void RPC_Invencible(float segundos)
    {
        invencibleHasta = Time.time + segundos;
    }

    void ActualizarUI()
    {
        if (ui != null) ui.CambiarImagen(currentHealth);
    }
}
