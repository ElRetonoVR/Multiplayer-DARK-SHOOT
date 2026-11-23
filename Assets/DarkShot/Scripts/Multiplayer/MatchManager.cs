using System;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

// Controla la partida: Esperando -> Cuenta regresiva (10 s) -> Jugando (180 s) -> Fin (10 s) -> Lobby.
// v2: la cuenta regresiva empieza solo cuando TODOS marcaron "listo" (boton A) y hay jugadores suficientes segun el
// modo de la sala (FFA 2+, Equipos 1+ por equipo con todos con equipo, Cooperativo 1+). X = Rojo, Y = Azul (solo Esperando).
//
// TODO el estado sale de propiedades de sala escritas SOLO por el MasterClient:
//   "ms" = estado (EstadoPartida), "t0" = PhotonNetwork.ServerTimestamp del inicio de ese estado.
// Asi quien entra tarde ve lo mismo, y si el master se va, el nuevo master continua donde quedo.
// Los clientes no dependen de callbacks: leen las propiedades cada frame y detectan los cambios.
public class MatchManager : MonoBehaviourPunCallbacks
{
    public static MatchManager Instancia { get; private set; }

    [Header("Reglas")]
    public int jugadoresMinimos = 2;      // Todos vs todos (en Equipos tambien se pide 1 por equipo; Cooperativo: 1)
    public bool entradaDeMando = true;    // A = listo, X = Rojo, Y = Azul (el modo escritorio llama a los metodos publicos)
    public float duracionCuenta = 10f;
    public float duracionPartida = 180f; // editable: el GDD dice 5-10 min, el usuario eligio 3
    [Header("Baterias al morir")]
    public string recursoBateria = "BatteryPickup";
    public int maxBateriasEnMapa = 12;   // tope total de baterias vivas (spawner + drops)
    public float duracionFin = 10f;

    // (estadoAnterior, estadoNuevo)
    public event Action<EstadoPartida, EstadoPartida> EstadoCambio;

    EstadoPartida estadoObservado;
    bool hayObservacion;
    float proximaEscritura;
    bool saliendo;

    void Awake()
    {
        Instancia = this;
    }

    void OnDestroy()
    {
        if (Instancia == this) Instancia = null;
    }

    // ------------------------------------------------------------ Estado
    public EstadoPartida Estado
    {
        get
        {
            if (!PhotonNetwork.InRoom) return EstadoPartida.Esperando;
            int v = MatchProps.GetRoomInt(MatchProps.Estado, 0);
            if (v < 0 || v > 3) v = 0;
            return (EstadoPartida)v;
        }
    }

    // Segundos transcurridos desde que empezo el estado actual (segun el reloj del servidor)
    public float SegundosEnEstado
    {
        get
        {
            if (!PhotonNetwork.InRoom || !MatchProps.RoomTiene(MatchProps.Inicio)) return 0f;
            int t0 = MatchProps.GetRoomInt(MatchProps.Inicio, 0);
            // ServerTimestamp es un int que da la vuelta (wrap-around): la resta debe ser unchecked
            int ms = unchecked(PhotonNetwork.ServerTimestamp - t0);
            if (ms < 0) ms = 0;
            return ms / 1000f;
        }
    }

    // Segundos que faltan para terminar el estado actual (0 en Esperando)
    public float SegundosRestantes
    {
        get
        {
            float e = SegundosEnEstado;
            switch (Estado)
            {
                case EstadoPartida.CuentaRegresiva: return Mathf.Max(0f, duracionCuenta - e);
                case EstadoPartida.Jugando: return Mathf.Max(0f, duracionPartida - e);
                case EstadoPartida.Fin: return Mathf.Max(0f, duracionFin - e);
                default: return 0f;
            }
        }
    }

    public int JugadoresEnSala
    {
        get { return PhotonNetwork.InRoom ? PhotonNetwork.CurrentRoom.PlayerCount : 0; }
    }

    public ModoPartida Modo { get { return MatchProps.ModoActual; } }

    // Jugadores minimos segun el modo
    public int MinimoJugadores
    {
        get
        {
            switch (Modo)
            {
                case ModoPartida.Cooperativo: return 1;
                case ModoPartida.Equipos: return 2;
                default: return Mathf.Max(1, jugadoresMinimos);
            }
        }
    }

    public int JugadoresListos
    {
        get
        {
            if (!PhotonNetwork.InRoom) return 0;
            int n = 0;
            Player[] lista = PhotonNetwork.PlayerList;
            for (int i = 0; i < lista.Length; i++) if (MatchProps.EstaListo(lista[i])) n++;
            return n;
        }
    }

    public int JugadoresEnEquipo(int equipo)
    {
        if (!PhotonNetwork.InRoom) return 0;
        int n = 0;
        Player[] lista = PhotonNetwork.PlayerList;
        for (int i = 0; i < lista.Length; i++) if (MatchProps.GetEquipo(lista[i]) == equipo) n++;
        return n;
    }

    // Motivo por el que aun no empieza la cuenta ("" si se cumplen las condiciones). Lo usa el HUD.
    public string MotivoEspera()
    {
        if (!PhotonNetwork.InRoom) return "";
        int jugadores = PhotonNetwork.CurrentRoom.PlayerCount;
        ModoPartida modo = Modo;

        if (jugadores < MinimoJugadores) return "Esperando jugadores " + jugadores + "/" + MinimoJugadores;

        if (modo == ModoPartida.Equipos)
        {
            Player[] lista = PhotonNetwork.PlayerList;
            for (int i = 0; i < lista.Length; i++)
            {
                if (MatchProps.GetEquipo(lista[i]) == MatchProps.SinEquipo) return "Todos deben elegir equipo";
            }
            if (JugadoresEnEquipo(MatchProps.EquipoRojo) < 1 || JugadoresEnEquipo(MatchProps.EquipoAzul) < 1)
                return "Hace falta al menos 1 jugador en cada equipo";
        }

        if (JugadoresListos < jugadores) return "Listos " + JugadoresListos + "/" + jugadores;
        return "";
    }

    // true si se cumplen todas las condiciones de inicio (todos listos, jugadores suficientes, equipos)
    public bool CondicionesDeInicio()
    {
        return PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom != null && MotivoEspera().Length == 0;
    }

    // Se puede disparar mientras se espera jugadores (para probar) y durante el combate
    public bool PuedeDisparar
    {
        get { EstadoPartida e = Estado; return e == EstadoPartida.Esperando || e == EstadoPartida.Jugando; }
    }

    // El dano solo cuenta en Esperando/Jugando (no en la cuenta regresiva ni en el marcador final)
    public bool DanoPermitido
    {
        get { return PuedeDisparar; }
    }

    // Jugadores ordenados por score (mas primero), luego kills y luego menos muertes
    public static List<Player> ObtenerMarcador()
    {
        List<Player> lista = new List<Player>(PhotonNetwork.PlayerList);
        lista.Sort(delegate (Player a, Player b)
        {
            int sa = MatchProps.GetInt(a, MatchProps.Score);
            int sb = MatchProps.GetInt(b, MatchProps.Score);
            if (sa != sb) return sb.CompareTo(sa);
            int ka = MatchProps.GetInt(a, MatchProps.Kills);
            int kb = MatchProps.GetInt(b, MatchProps.Kills);
            if (ka != kb) return kb.CompareTo(ka);
            int da = MatchProps.GetInt(a, MatchProps.Muertes);
            int db = MatchProps.GetInt(b, MatchProps.Muertes);
            if (da != db) return da.CompareTo(db);
            return a.ActorNumber.CompareTo(b.ActorNumber);
        });
        return lista;
    }

    // ------------------------------------------------------------ Bucle
    void Update()
    {
        if (!PhotonNetwork.InRoom) return;

        // 1. Detectar cambios de estado (en todos los clientes)
        EstadoPartida actual = Estado;
        if (!hayObservacion)
        {
            hayObservacion = true;
            estadoObservado = actual;
            AlCambiarEstado(actual, actual, true);
        }
        else if (actual != estadoObservado)
        {
            EstadoPartida previo = estadoObservado;
            estadoObservado = actual;
            AlCambiarEstado(previo, actual, false);
        }

        // 1b. Entrada del mando (A = listo, X = Rojo, Y = Azul)
        if (entradaDeMando) LeerEntradaDeMando();

        // 2. El master avanza la maquina de estados
        EvaluarComoMaster();

        // 3. Al terminar el marcador, todos salen al Lobby
        if (actual == EstadoPartida.Fin && !saliendo && SegundosEnEstado >= duracionFin)
        {
            saliendo = true;
            MatchProps.LimpiarListoYEquipoLocal(); // listo y equipo no pasan a la siguiente sala
            NetworkManager.SalirDeLaSala();
        }
    }

    void AlCambiarEstado(EstadoPartida previo, EstadoPartida nuevo, bool primeraObservacion)
    {
        // Al entrar a una sala nueva (y mientras se espera) nadie arrastra "listo" ni equipo de la partida anterior
        // (las propiedades del jugador local sobreviven al cambio de sala)
        if (primeraObservacion && nuevo == EstadoPartida.Esperando) MatchProps.LimpiarListoYEquipoLocal();

        // Antes de combatir se reinician kills/muertes (cada cliente escribe solo las suyas)
        if (nuevo == EstadoPartida.Esperando || nuevo == EstadoPartida.CuentaRegresiva)
        {
            ReiniciarMarcadorLocal();
        }

        // Al empezar el combate todos reaparecen con vida y bateria al 100%.
        // Quien entra tarde (primeraObservacion) ya fue colocado por el NetworkManager.
        if (nuevo == EstadoPartida.Jugando && !primeraObservacion)
        {
            // Sin invencibilidad: en el GDD los 5 s son solo tras morir
            if (PlayerHealth.Local != null) PlayerHealth.Local.ReaparecerAlInicioDePartida();
        }

        // Partida en marcha o terminada: que nadie nuevo entre (en Equipos entrar tarde dejaria a alguien sin equipo)
        if (nuevo == EstadoPartida.Jugando || nuevo == EstadoPartida.Fin) CerrarSalaSiSoyMaster();

        // Al terminar se limpia "listo" (el equipo se conserva hasta salir: el marcador final se agrupa por equipo)
        if (nuevo == EstadoPartida.Fin) MatchProps.SetLocalBool(MatchProps.Listo, false);

        if (EstadoCambio != null) EstadoCambio(previo, nuevo);
    }

    void ReiniciarMarcadorLocal()
    {
        if (PhotonNetwork.LocalPlayer == null) return;
        MatchProps.ReiniciarLocal();
        if (PlayerHealth.Local != null)
        {
            PlayerHealth.Local.kills = 0;
            PlayerHealth.Local.deaths = 0;
        }
    }

    // ------------------------------------------------------------ Listo y equipos (cada jugador escribe solo lo suyo)
    // Entrada del mando. Solo en Esperando; en la cuenta regresiva A solo sirve para DESMARCAR (cancelar el inicio).
    void LeerEntradaDeMando()
    {
        if (!PhotonNetwork.InRoom) return;
        EstadoPartida e = Estado;
        if (e != EstadoPartida.Esperando && e != EstadoPartida.CuentaRegresiva) return;

        if (OVRInput.GetDown(OVRInput.RawButton.A)) AlternarListoLocal();
        if (e != EstadoPartida.Esperando) return;
        if (OVRInput.GetDown(OVRInput.RawButton.X)) ElegirEquipoLocal(MatchProps.EquipoRojo);
        if (OVRInput.GetDown(OVRInput.RawButton.Y)) ElegirEquipoLocal(MatchProps.EquipoAzul);
    }

    // A (o K en escritorio): alterna listo. En Equipos hace falta tener equipo para marcarse listo.
    public void AlternarListoLocal()
    {
        if (!PhotonNetwork.InRoom || PhotonNetwork.LocalPlayer == null) return;
        EstadoPartida e = Estado;
        if (e != EstadoPartida.Esperando && e != EstadoPartida.CuentaRegresiva) return;

        bool listo = MatchProps.EstaListo(PhotonNetwork.LocalPlayer);
        if (!listo)
        {
            if (e != EstadoPartida.Esperando) return; // en la cuenta solo se puede desmarcar
            if (Modo == ModoPartida.Equipos && MatchProps.GetEquipo(PhotonNetwork.LocalPlayer) == MatchProps.SinEquipo) return;
        }
        MatchProps.SetLocalBool(MatchProps.Listo, !listo);
    }

    // X = Rojo, Y = Azul (Z / X en escritorio). Solo en Esperando y solo en modo Equipos. Al cambiar se desmarca listo.
    public void ElegirEquipoLocal(int equipo)
    {
        if (!PhotonNetwork.InRoom || PhotonNetwork.LocalPlayer == null) return;
        if (Estado != EstadoPartida.Esperando || Modo != ModoPartida.Equipos) return;
        if (equipo != MatchProps.EquipoRojo && equipo != MatchProps.EquipoAzul) return;
        if (MatchProps.GetEquipo(PhotonNetwork.LocalPlayer) == equipo) return;

        Hashtable h = new Hashtable();
        h[MatchProps.Equipo] = equipo;
        h[MatchProps.Listo] = false;
        PhotonNetwork.LocalPlayer.SetCustomProperties(h);
    }

    void CerrarSalaSiSoyMaster()
    {
        if (!PhotonNetwork.IsMasterClient || PhotonNetwork.CurrentRoom == null) return;
        if (PhotonNetwork.CurrentRoom.IsOpen) PhotonNetwork.CurrentRoom.IsOpen = false;
        if (PhotonNetwork.CurrentRoom.IsVisible) PhotonNetwork.CurrentRoom.IsVisible = false; // sale de la lista del Lobby
    }

    // ------------------------------------------------------------ Master
    void EvaluarComoMaster()
    {
        if (!PhotonNetwork.IsMasterClient || PhotonNetwork.CurrentRoom == null) return;
        if (Time.unscaledTime < proximaEscritura) return; // dar tiempo a que la escritura anterior se aplique

        // Sala recien creada (o master nuevo sin datos): fijar el estado inicial
        if (!MatchProps.RoomTiene(MatchProps.Estado) || !MatchProps.RoomTiene(MatchProps.Inicio))
        {
            EscribirEstado(EstadoPartida.Esperando);
            return;
        }

        EstadoPartida e = Estado;

        switch (e)
        {
            case EstadoPartida.Esperando:
                // Todos listos + jugadores suficientes segun el modo (+ equipos completos en Equipos)
                if (CondicionesDeInicio()) EscribirEstado(EstadoPartida.CuentaRegresiva);
                break;

            case EstadoPartida.CuentaRegresiva:
                // Alguien se desmarco, entro alguien nuevo o se fue alguien: volver a Esperando
                if (!CondicionesDeInicio()) EscribirEstado(EstadoPartida.Esperando);
                else if (SegundosEnEstado >= duracionCuenta) EscribirEstado(EstadoPartida.Jugando);
                break;

            case EstadoPartida.Jugando:
                CerrarSalaSiSoyMaster(); // por si el master anterior se fue antes de cerrarla
                if (SegundosEnEstado >= duracionPartida) EscribirEstado(EstadoPartida.Fin);
                break;

            case EstadoPartida.Fin:
                CerrarSalaSiSoyMaster(); // por si el master anterior se fue antes de cerrarla
                break;
        }
    }

    void EscribirEstado(EstadoPartida nuevo)
    {
        Hashtable h = new Hashtable();
        h[MatchProps.Estado] = (int)nuevo;
        h[MatchProps.Inicio] = PhotonNetwork.ServerTimestamp;
        PhotonNetwork.CurrentRoom.SetCustomProperties(h);
        proximaEscritura = Time.unscaledTime + 0.3f;
    }

    // ------------------------------------------------------------ RPC de baterias (este PhotonView es de escena)
    // Lo pide la victima al morir; solo el Master lo ejecuta: suelta 1-3 baterias alrededor del punto.
    [PunRPC]
    void RPC_SoltarBaterias(Vector3 posicion)
    {
        if (!PhotonNetwork.IsMasterClient || Estado != EstadoPartida.Jugando) return;

        int cuantas = UnityEngine.Random.Range(1, 4); // 1 a 3
        for (int i = 0; i < cuantas; i++)
        {
            if (BatteryPickup.Activas.Count >= maxBateriasEnMapa) break;
            Vector2 c = UnityEngine.Random.insideUnitCircle * 0.4f;
            Vector3 p = posicion + new Vector3(c.x, 0f, c.y);
            // data[0] = 0: no cuenta para el maximo de 3 del spawner
            PhotonNetwork.InstantiateRoomObject(recursoBateria, p, Quaternion.identity, 0, new object[] { 0 });
        }
    }

    // El Master avisa al jugador que recogio: +1 a la reserva (maximo 3, lo valida Gun)
    [PunRPC]
    void RPC_SumarBateria()
    {
        if (Gun.Local != null) Gun.Local.AgregarReserva(1);
    }

    // Cooperativo: el Master avisa al tirador que su disparo elimino un Bloom (+1 Bloom, +100 puntos)
    [PunRPC]
    void RPC_BloomEliminado()
    {
        MatchProps.SumarLocalInt(MatchProps.Blooms, 1);
        MatchProps.SumarLocalInt(MatchProps.Score, MatchProps.PuntosPorBloom);
        VibracionVR.Pulso(OVRInput.Controller.RTouch, 0.08f);
    }

    // ------------------------------------------------------------ Callbacks (solo para reaccionar rapido)
    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        proximaEscritura = 0f;
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        proximaEscritura = 0f;
        // Alguien nuevo aun no esta listo: si habia cuenta regresiva, EvaluarComoMaster vuelve a Esperando
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        proximaEscritura = 0f; // reaccionar rapido a listo / equipo (el estado igual se evalua cada frame)
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        proximaEscritura = 0f;
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        // Nada: el estado se lee cada frame en Update
    }
}
