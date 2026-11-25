using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Hashtable = ExitGames.Client.Photon.Hashtable;

// Lista de salas del Lobby. Lo crea y conecta el menu 7 (DarkShot/7. Configurar Lobby).
//   - El boton Multiplayer llama a AbrirPanel(): se conecta (si hace falta), entra al lobby de Photon
//     y rellena hasta 6 botones con las salas abiertas (nombre y jugadores/max).
//   - Crear: sala "Sala-####" de 4 jugadores con modo (propiedad de sala "md": 0 Todos vs todos, 1 Equipos, 2 Cooperativo).
//     Tres botones: CrearSalaFFA / CrearSalaEquipos / CrearSalaCoop. Unirse: boton de la sala (muestra el modo).
//   - Al entrar a la sala, el MasterClient hace PhotonNetwork.LoadLevel("SampleScene") y el resto lo sigue
//     (AutomaticallySyncScene = true).
//   - Volver: oculta el panel y desconecta.
// Si el jugador vuelve de una partida ya esta conectado al Master Server: se detecta y se entra al lobby igual.
public class RoomBrowser : MonoBehaviourPunCallbacks
{
    [Header("Referencias (las asigna el menu 7)")]
    public GameObject panel;              // RoomPanel (desactivado al inicio)
    public GameObject menuPrincipal;      // Buttons (SingleBtn / MultiBtn): se oculta mientras el panel esta abierto
    public TMP_Text textoEstado;
    public Button[] botonesSala = new Button[6];
    public Button botonCrear;             // compatibilidad: crea sala Todos vs todos
    public Button botonCrearFFA;
    public Button botonCrearEquipos;
    public Button botonCrearCoop;
    public Button botonVolver;
    public Button botonCaminando;         // alterna "Caminando: SI / NO" para las salas que se creen

    [Header("Opciones")]
    public string escenaPartida = "SampleScene";      // Todos vs todos y Equipos (laboratorio)
    public string escenaCooperativo = "ArenaCoop";    // Cooperativo: mapa del single player (menu 10)
    public byte maxJugadores = 4; // GDD Laser Project: 2 a 4 jugadores
    public float segundosMensajeError = 4f;

    readonly Dictionary<string, RoomInfo> salas = new Dictionary<string, RoomInfo>();
    readonly List<RoomInfo> ordenadas = new List<RoomInfo>();
    const string PrefCaminando = "DarkShot_Caminando";
    bool caminando;                // modo caminando para la proxima sala creada (se recuerda en el visor)
    bool panelAbierto;
    bool ocupado;                  // creando o uniendose a una sala
    float proximoChequeo;
    float errorHasta;

    void Awake()
    {
        PhotonNetwork.AutomaticallySyncScene = true;

        if (botonCrear != null) botonCrear.onClick.AddListener(CrearSalaFFA);
        if (botonCrearFFA != null) botonCrearFFA.onClick.AddListener(CrearSalaFFA);
        if (botonCrearEquipos != null) botonCrearEquipos.onClick.AddListener(CrearSalaEquipos);
        if (botonCrearCoop != null) botonCrearCoop.onClick.AddListener(CrearSalaCoop);
        if (botonVolver != null) botonVolver.onClick.AddListener(Volver);
        if (botonCaminando != null) botonCaminando.onClick.AddListener(AlternarCaminando);
        caminando = PlayerPrefs.GetInt(PrefCaminando, 0) != 0;
        ActualizarTextoCaminando();
        if (botonesSala != null)
        {
            for (int i = 0; i < botonesSala.Length; i++)
            {
                if (botonesSala[i] == null) continue;
                int indice = i; // copia para el closure
                botonesSala[i].onClick.AddListener(delegate { UnirseASala(indice); });
            }
        }

        if (panel != null) panel.SetActive(false);
        RefrescarBotones();
    }

    // ------------------------------------------------------------ Panel
    // Lo llama el boton Multiplayer (onClick persistente configurado por el menu 7)
    public void AbrirPanel()
    {
        panelAbierto = true;
        ocupado = false;
        salas.Clear();
        if (panel != null) panel.SetActive(true);
        if (menuPrincipal != null) menuPrincipal.SetActive(false);
        EstablecerEstado("Conectando...");
        RefrescarBotones();
        proximoChequeo = 0f; // comprobar la conexion ya en el siguiente Update
    }

    public void Volver()
    {
        panelAbierto = false;
        ocupado = false;
        salas.Clear();
        if (panel != null) panel.SetActive(false);
        if (menuPrincipal != null) menuPrincipal.SetActive(true);
        if (PhotonNetwork.IsConnected) PhotonNetwork.Disconnect();
    }

    // Mantiene la conexion en marcha mientras el panel esta abierto (cubre el caso de volver de una partida)
    void Update()
    {
        if (!panelAbierto || ocupado) return;
        if (Time.unscaledTime < proximoChequeo) return;
        proximoChequeo = Time.unscaledTime + 0.5f;

        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom(); // seguimos en una sala de una partida anterior: salir y volver al master
            EstablecerEstado("Conectando...");
            return;
        }

        switch (PhotonNetwork.NetworkClientState)
        {
            case ClientState.PeerCreated:
            case ClientState.Disconnected:
                EstablecerEstado("Conectando...");
                if (!PhotonNetwork.ConnectUsingSettings()) EstablecerError("No se pudo iniciar la conexion");
                break;

            case ClientState.ConnectedToMasterServer:
                EstablecerEstado("Entrando al lobby...");
                PhotonNetwork.JoinLobby();
                break;
        }
    }

    // ------------------------------------------------------------ Acciones
    // Envoltorios sin parametros (para botones de UI y modo escritorio)
    public void CrearSala() { CrearSala((int)ModoPartida.TodosContraTodos); }
    public void CrearSalaFFA() { CrearSala((int)ModoPartida.TodosContraTodos); }
    public void CrearSalaEquipos() { CrearSala((int)ModoPartida.Equipos); }
    public void CrearSalaCoop() { CrearSala((int)ModoPartida.Cooperativo); }

    // Boton "Caminando": sin teletransporte al morir, cada jugador empieza en su punto de origen
    public void AlternarCaminando()
    {
        caminando = !caminando;
        PlayerPrefs.SetInt(PrefCaminando, caminando ? 1 : 0);
        PlayerPrefs.Save();
        ActualizarTextoCaminando();
    }

    void ActualizarTextoCaminando()
    {
        if (botonCaminando == null) return;
        TMP_Text t = botonCaminando.GetComponentInChildren<TMP_Text>(true);
        if (t != null) t.text = caminando ? "Caminando: SI" : "Caminando: NO";
    }

    public void CrearSala(int modo)
    {
        if (modo < 0 || modo > 2) modo = 0;
        if (ocupado || !PhotonNetwork.IsConnectedAndReady || PhotonNetwork.InRoom)
        {
            EstablecerError("Espera a estar conectado");
            return;
        }

        string nombre = "Sala-" + Random.Range(0, 10000).ToString("D4");
        RoomOptions opciones = new RoomOptions();
        opciones.MaxPlayers = maxJugadores;
        opciones.IsVisible = true;
        opciones.IsOpen = true;
        opciones.CleanupCacheOnLeave = true;
        // Modo de la sala: propiedad de sala "md", publicada en el lobby para mostrarla en la lista
        Hashtable props = new Hashtable();
        props[MatchProps.Modo] = modo;
        props[MatchProps.Caminando] = caminando ? 1 : 0;
        opciones.CustomRoomProperties = props;
        opciones.CustomRoomPropertiesForLobby = new string[] { MatchProps.Modo, MatchProps.Caminando };

        ocupado = true;
        EstablecerEstado("Creando " + nombre + " (" + MatchProps.NombreModo(modo) + (caminando ? ", caminando" : "") + ")...");
        if (!PhotonNetwork.CreateRoom(nombre, opciones))
        {
            ocupado = false;
            EstablecerError("No se pudo crear la sala");
        }
    }

    // Modo escritorio: unirse a la primera sala con lugar
    public void UnirsePrimeraSala()
    {
        for (int i = 0; i < ordenadas.Count; i++)
        {
            if (ordenadas[i].IsOpen && ordenadas[i].PlayerCount < ordenadas[i].MaxPlayers)
            {
                UnirseASala(i);
                return;
            }
        }
        EstablecerError("No hay salas con lugar");
    }

    void UnirseASala(int indice)
    {
        if (ocupado || indice < 0 || indice >= ordenadas.Count) return;
        RoomInfo info = ordenadas[indice];
        if (info.PlayerCount >= info.MaxPlayers) return;

        ocupado = true;
        EstablecerEstado("Uniendose a " + info.Name + "...");
        if (!PhotonNetwork.JoinRoom(info.Name))
        {
            ocupado = false;
            EstablecerError("No se pudo entrar a la sala");
        }
    }

    // ------------------------------------------------------------ Callbacks de Photon
    public override void OnConnectedToMaster()
    {
        if (panelAbierto && !PhotonNetwork.InLobby)
        {
            EstablecerEstado("Entrando al lobby...");
            PhotonNetwork.JoinLobby();
        }
    }

    public override void OnJoinedLobby()
    {
        salas.Clear(); // el servidor reenvia la lista completa
        RefrescarBotones();
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        for (int i = 0; i < roomList.Count; i++)
        {
            RoomInfo info = roomList[i];
            if (info.RemovedFromList || !info.IsOpen || !info.IsVisible)
            {
                salas.Remove(info.Name);
            }
            else
            {
                salas[info.Name] = info;
            }
        }
        RefrescarBotones();
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        ocupado = false;
        EstablecerError("No se pudo crear la sala: " + message);
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        ocupado = false;
        EstablecerError("No se pudo entrar: " + message);
    }

    public override void OnJoinedRoom()
    {
        // El master carga la escena de la partida; los demas la siguen por AutomaticallySyncScene
        if (PhotonNetwork.IsMasterClient)
        {
            EstablecerEstado("Cargando partida...");
            bool coop = MatchProps.ModoActual == ModoPartida.Cooperativo && !string.IsNullOrEmpty(escenaCooperativo)
                && Application.CanStreamedLevelBeLoaded(escenaCooperativo);
            PhotonNetwork.LoadLevel(coop ? escenaCooperativo : escenaPartida);
        }
        else
        {
            EstablecerEstado("Entrando a la partida...");
        }
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        salas.Clear();
        ocupado = false;
        RefrescarBotones();
        if (panelAbierto) EstablecerError("Desconectado: " + cause);
    }

    // ------------------------------------------------------------ UI
    void RefrescarBotones()
    {
        ordenadas.Clear();
        foreach (KeyValuePair<string, RoomInfo> kv in salas) ordenadas.Add(kv.Value);
        ordenadas.Sort(delegate (RoomInfo a, RoomInfo b) { return string.CompareOrdinal(a.Name, b.Name); });

        if (botonesSala != null)
        {
            for (int i = 0; i < botonesSala.Length; i++)
            {
                Button boton = botonesSala[i];
                if (boton == null) continue;

                if (i < ordenadas.Count)
                {
                    RoomInfo info = ordenadas[i];
                    boton.gameObject.SetActive(true);
                    boton.interactable = info.IsOpen && info.PlayerCount < info.MaxPlayers; // llenas: desactivadas
                    TMP_Text t = boton.GetComponentInChildren<TMP_Text>(true);
                    if (t != null) t.text = info.Name + " - " + NombreModoDe(info) + " - " + info.PlayerCount + "/" + info.MaxPlayers;
                }
                else
                {
                    boton.gameObject.SetActive(false);
                }
            }
        }

        if (!panelAbierto || Time.unscaledTime < errorHasta || ocupado) return;
        if (PhotonNetwork.InLobby)
        {
            if (ordenadas.Count == 0) EstablecerEstado("Sin salas, crea una");
            else EstablecerEstado("Salas disponibles: " + ordenadas.Count);
        }
    }

    // Modo publicado en el lobby ("md"); sin la propiedad se asume Todos vs todos
    static string NombreModoDe(RoomInfo info)
    {
        int modo = 0;
        object valor;
        if (info.CustomProperties != null && info.CustomProperties.TryGetValue(MatchProps.Modo, out valor) && valor != null)
        {
            try { modo = System.Convert.ToInt32(valor); } catch { modo = 0; }
        }
        string nombre = MatchProps.NombreModo(modo);
        if (info.CustomProperties != null && info.CustomProperties.TryGetValue(MatchProps.Caminando, out valor) && valor != null)
        {
            int c = 0;
            try { c = System.Convert.ToInt32(valor); } catch { c = 0; }
            if (c != 0) nombre += " (caminando)";
        }
        return nombre;
    }

    void EstablecerEstado(string mensaje)
    {
        if (textoEstado != null) textoEstado.text = mensaje;
    }

    void EstablecerError(string mensaje)
    {
        errorHasta = Time.unscaledTime + segundosMensajeError;
        EstablecerEstado(mensaje);
    }
}
