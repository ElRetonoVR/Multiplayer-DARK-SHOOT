using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;

// Crea al jugador local al entrar a la sala y vuelve al Lobby al salir o desconectarse.
//   - Flujo normal: el Lobby crea/une la sala y el master hace PhotonNetwork.LoadLevel("SampleScene").
//     Al cargar esta escena ya se esta en la sala: se instancia el jugador en Start.
//   - Pruebas: si se abre SampleScene directo en el editor (sin sala), conecta y hace JoinOrCreateRoom.
public class NetworkManager : MonoBehaviourPunCallbacks
{
    public Transform[] spawnPoints;

    [Header("Escenas y pruebas")]
    public string escenaLobby = "Lobby";
    public string salaDePruebas = "RoomVR";

    public static NetworkManager Instancia { get; private set; }

    bool jugadorCreado;

    // Estado estatico: sobrevive al cambio de escena y se reinicia cuando la nueva escena carga
    static bool cargandoEscena;
    static bool saliendoDeSala;
    static bool suscritoAEscenas;

    void Awake()
    {
        Instancia = this;
        // El master carga la escena y todos los demas la siguen
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    void OnDestroy()
    {
        if (Instancia == this) Instancia = null;
    }

    void Start()
    {
        if (PhotonNetwork.InRoom)
        {
            CrearJugador();
        }
        else if (!PhotonNetwork.IsConnected)
        {
            // Prueba directa en el editor
            PhotonNetwork.ConnectUsingSettings();
        }
        else if (PhotonNetwork.IsConnectedAndReady)
        {
            EntrarASalaDePruebas();
        }
        // Si esta conectandose, OnConnectedToMaster hace el resto
    }

    void EntrarASalaDePruebas()
    {
        // Abrir ArenaCoop directo prueba el Cooperativo (sala de pruebas aparte para no mezclarse con la de SampleScene)
        bool coop = SceneManager.GetActiveScene().name.Contains("Coop");
        ModoPartida modo = coop ? ModoPartida.Cooperativo : ModoPartida.TodosContraTodos;
        PhotonNetwork.JoinOrCreateRoom(
            coop ? salaDePruebas + "-Coop" : salaDePruebas,
            new RoomOptions { MaxPlayers = 4, CustomRoomProperties = new ExitGames.Client.Photon.Hashtable { { MatchProps.Modo, (int)modo } } },
            TypedLobby.Default
        );
    }

    // ------------------------------------------------------------ Callbacks
    public override void OnConnectedToMaster()
    {
        if (cargandoEscena || saliendoDeSala) return; // estamos volviendo al Lobby
        if (!PhotonNetwork.InRoom) EntrarASalaDePruebas();
    }

    public override void OnJoinedRoom()
    {
        CrearJugador();
    }

    public override void OnLeftRoom()
    {
        CargarEscena(escenaLobby);
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        // Al cerrar la app (o detener Play en el editor) no se carga ninguna escena
        if (cause == DisconnectCause.ApplicationQuit) return;
        Debug.LogWarning("[NetworkManager] Desconectado: " + cause + ". Volviendo al Lobby.");
        CargarEscena(escenaLobby);
    }

    // ------------------------------------------------------------ Jugador
    void CrearJugador()
    {
        if (jugadorCreado) return;
        jugadorCreado = true;

        Transform sp = ObtenerMejorSpawn(null);
        Vector3 pos = sp != null ? sp.position : Vector3.zero;
        Quaternion rot = sp != null ? sp.rotation : Quaternion.identity;

        PhotonNetwork.Instantiate("PlayerVR", pos, rot);
        if (sp != null) TeletransportarRig(sp);
    }

    // Spawns que le tocan a un equipo: Rojo = los de x menor al centro de los spawns, Azul = x mayor.
    // Sin equipo (o fuera del modo Equipos) devuelve todos.
    List<Transform> SpawnsDelEquipo(int equipo)
    {
        List<Transform> todos = new List<Transform>();
        for (int i = 0; i < spawnPoints.Length; i++) if (spawnPoints[i] != null) todos.Add(spawnPoints[i]);
        if (todos.Count == 0 || MatchProps.ModoActual != ModoPartida.Equipos) return todos;
        if (equipo != MatchProps.EquipoRojo && equipo != MatchProps.EquipoAzul) return todos;

        float centro = 0f;
        for (int i = 0; i < todos.Count; i++) centro += todos[i].position.x;
        centro /= todos.Count;

        List<Transform> mitad = new List<Transform>();
        for (int i = 0; i < todos.Count; i++)
        {
            float x = todos[i].position.x;
            bool rojo = x < centro;
            if (equipo == MatchProps.EquipoRojo ? rojo : !rojo) mitad.Add(todos[i]);
        }
        return mitad.Count > 0 ? mitad : todos; // si todos los spawns estan alineados en x, usar todos
    }

    // Spawn al empezar el combate (Equipos y Cooperativo): reparte por orden. En Equipos solo usa la mitad del equipo;
    // el orden es el lugar del jugador entre los de su equipo (por ActorNumber), asi dos companeros no coinciden.
    public Transform ObtenerSpawnInicialPartida()
    {
        if (spawnPoints == null || spawnPoints.Length == 0 || PhotonNetwork.LocalPlayer == null) return null;
        int equipo = MatchProps.GetEquipo(PhotonNetwork.LocalPlayer);
        List<Transform> candidatos = SpawnsDelEquipo(equipo);
        if (candidatos.Count == 0) return null;

        int orden = 0;
        Player[] jugadores = PhotonNetwork.PlayerList;
        for (int i = 0; i < jugadores.Length; i++)
        {
            Player p = jugadores[i];
            if (p.ActorNumber >= PhotonNetwork.LocalPlayer.ActorNumber) continue;
            if (MatchProps.ModoActual == ModoPartida.Equipos && MatchProps.GetEquipo(p) != equipo) continue;
            orden++;
        }
        return candidatos[orden % candidatos.Count];
    }

    // Devuelve el spawn mas lejano de los demas jugadores (y de 'posicionPropia' si se indica,
    // para no reaparecer donde se acaba de morir). Sin otros jugadores reparte por numero de actor.
    // v2: en Equipos solo se usan los spawns de la mitad del equipo y se huye de los ENEMIGOS (no de los companeros);
    // en Cooperativo tambien se huye de los Blooms.
    public Transform ObtenerMejorSpawn(Vector3? posicionPropia)
    {
        if (spawnPoints == null || spawnPoints.Length == 0) return null;

        ModoPartida modo = MatchProps.ModoActual;
        int miEquipo = PhotonNetwork.LocalPlayer != null ? MatchProps.GetEquipo(PhotonNetwork.LocalPlayer) : MatchProps.SinEquipo;
        List<Transform> candidatos = SpawnsDelEquipo(miEquipo);

        List<Vector3> otros = new List<Vector3>();
        PlayerHealth[] jugadores = FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None);
        for (int i = 0; i < jugadores.Length; i++)
        {
            PlayerHealth ph = jugadores[i];
            if (ph == null || ph.photonView == null || ph.photonView.IsMine) continue;
            if (modo == ModoPartida.Equipos && MatchProps.SonAliados(PhotonNetwork.LocalPlayer, ph.photonView.Owner)) continue;
            otros.Add(ph.PosicionCabeza);
        }
        if (modo == ModoPartida.Cooperativo)
        {
            for (int i = 0; i < BloomNet.Activos.Count; i++)
            {
                if (BloomNet.Activos[i] != null) otros.Add(BloomNet.Activos[i].transform.position);
            }
        }
        if (posicionPropia.HasValue) otros.Add(posicionPropia.Value);

        if (otros.Count == 0)
        {
            int actor = PhotonNetwork.LocalPlayer != null ? PhotonNetwork.LocalPlayer.ActorNumber : 1;
            Transform porActor = candidatos.Count > 0 ? candidatos[Mathf.Abs(actor - 1) % candidatos.Count] : null;
            if (porActor != null) return porActor;
        }

        Transform mejor = null;
        float mejorDistancia = -1f;
        for (int i = 0; i < candidatos.Count; i++)
        {
            Transform sp = candidatos[i];
            if (sp == null) continue;

            float minima = float.MaxValue;
            for (int j = 0; j < otros.Count; j++)
            {
                float dx = sp.position.x - otros[j].x;
                float dz = sp.position.z - otros[j].z;
                float d = Mathf.Sqrt(dx * dx + dz * dz);
                if (d < minima) minima = d;
            }
            if (otros.Count == 0) minima = Random.value;

            if (minima > mejorDistancia)
            {
                mejorDistancia = minima;
                mejor = sp;
            }
        }
        return mejor;
    }

    // El cuerpo de red sigue al OVRCameraRig local (VRLocalToNetworkMapper), asi que para mover al
    // jugador hay que mover el rig. Se coloca la CABEZA sobre el spawn y se gira para mirar como el spawn.
    public void TeletransportarRig(Transform spawn)
    {
        if (spawn == null) return;
        OVRCameraRig rig = FindFirstObjectByType<OVRCameraRig>();
        if (rig == null) return;

        Transform rt = rig.transform;
        Transform ojo = rig.centerEyeAnchor;
        if (ojo == null)
        {
            rt.SetPositionAndRotation(spawn.position, spawn.rotation);
            return;
        }

        Vector3 ojoLocal = rt.InverseTransformPoint(ojo.position);
        Vector3 f = rt.InverseTransformDirection(ojo.forward);
        f.y = 0f;
        float giroLocal = f.sqrMagnitude > 0.0001f ? Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg : 0f;

        rt.rotation = Quaternion.Euler(0f, spawn.eulerAngles.y - giroLocal, 0f);

        Vector3 ojoMundo = rt.TransformPoint(ojoLocal);
        Vector3 desplazamiento = spawn.position - ojoMundo;
        rt.position = new Vector3(rt.position.x + desplazamiento.x, spawn.position.y, rt.position.z + desplazamiento.z);
    }

    // ------------------------------------------------------------ Salir / cambiar de escena
    // Punto unico para salir de la partida (boton, Start del mando, fin del marcador)
    public static void SalirDeLaSala()
    {
        saliendoDeSala = true;
        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom(); // OnLeftRoom carga el Lobby
        }
        else
        {
            CargarEscena(Instancia != null ? Instancia.escenaLobby : "Lobby");
        }
    }

    // Carga una escena una sola vez aunque varios scripts lo pidan en el mismo frame
    public static void CargarEscena(string nombre)
    {
        if (cargandoEscena) return;
        cargandoEscena = true;
        if (!suscritoAEscenas)
        {
            suscritoAEscenas = true;
            SceneManager.sceneLoaded += AlCargarEscena;
        }
        SceneManager.LoadScene(nombre);
    }

    static void AlCargarEscena(Scene escena, LoadSceneMode modo)
    {
        cargandoEscena = false;
        saliendoDeSala = false;
    }
}
