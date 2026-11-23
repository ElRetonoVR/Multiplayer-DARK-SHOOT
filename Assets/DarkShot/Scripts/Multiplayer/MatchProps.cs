using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

// Estados de la partida multijugador. El valor entero es el que viaja en la propiedad de sala "ms".
public enum EstadoPartida
{
    Esperando = 0,       // menos de 2 jugadores
    CuentaRegresiva = 1, // 10 s antes de empezar
    Jugando = 2,         // 180 s de combate
    Fin = 3              // marcador 10 s y luego todos al Lobby
}

// Modo de la sala (propiedad de sala "md"). Se fija al crear la sala en el Lobby.
public enum ModoPartida
{
    TodosContraTodos = 0,
    Equipos = 1,
    Cooperativo = 2
}

// Claves de propiedades de sala / jugador y utilidades para leerlas y escribirlas.
//   Sala (las escribe SOLO el MasterClient):  "ms" = estado, "t0" = ServerTimestamp del inicio del estado.
//   Jugador (cada uno escribe solo las suyas): "h" = impactos, "k" = kills, "d" = muertes, "s" = score
//   (impacto +10, kill +100, muerte 0). La victima avisa al atacante por RPC y el atacante escribe lo suyo.
//   v2: Sala "md" = modo (ModoPartida). Jugador "r" = listo (bool), "t" = equipo (0 sin equipo, 1 Rojo, 2 Azul),
//   "b" = Blooms eliminados (Cooperativo).
public static class MatchProps
{
    public const string Estado = "ms";
    public const string Inicio = "t0";
    public const string Kills = "k";
    public const string Muertes = "d";
    public const string Impactos = "h";
    public const string Score = "s";
    public const string Modo = "md";       // sala
    public const string Listo = "r";       // jugador (bool)
    public const string Equipo = "t";      // jugador (0, 1, 2)
    public const string Blooms = "b";      // jugador

    public const int SinEquipo = 0;
    public const int EquipoRojo = 1;
    public const int EquipoAzul = 2;
    public const int PuntosPorBloom = 100;

    public static readonly Color ColorRojo = new Color(1.00f, 0.19f, 0.19f); // #FF3030
    public static readonly Color ColorAzul = new Color(0.18f, 0.48f, 1.00f); // #2F7BFF
    public static readonly Color ColorSinEquipo = new Color(0.5f, 0.5f, 0.5f);

    public const int PuntosPorImpacto = 10;
    public const int PuntosPorKill = 100;

    // Lee un entero de las propiedades del jugador (0 si no existe)
    public static int GetInt(Player jugador, string clave)
    {
        if (jugador == null || jugador.CustomProperties == null) return 0;
        object valor;
        if (!jugador.CustomProperties.TryGetValue(clave, out valor) || valor == null) return 0;
        return ConvertirAInt(valor);
    }

    // Lee un entero de las propiedades de la sala actual (valorPorDefecto si no existe)
    public static int GetRoomInt(string clave, int valorPorDefecto)
    {
        Room sala = PhotonNetwork.CurrentRoom;
        if (sala == null || sala.CustomProperties == null) return valorPorDefecto;
        object valor;
        if (!sala.CustomProperties.TryGetValue(clave, out valor) || valor == null) return valorPorDefecto;
        return ConvertirAInt(valor);
    }

    public static bool RoomTiene(string clave)
    {
        Room sala = PhotonNetwork.CurrentRoom;
        return sala != null && sala.CustomProperties != null && sala.CustomProperties.ContainsKey(clave);
    }

    // Escribe un entero en las propiedades del jugador LOCAL (nunca de otro jugador)
    public static void SetLocalInt(string clave, int valor)
    {
        if (PhotonNetwork.LocalPlayer == null) return;
        Hashtable h = new Hashtable();
        h[clave] = valor;
        PhotonNetwork.LocalPlayer.SetCustomProperties(h);
    }

    // Suma 'delta' a un entero de las propiedades del jugador LOCAL
    public static void SumarLocalInt(string clave, int delta)
    {
        SetLocalInt(clave, GetInt(PhotonNetwork.LocalPlayer, clave) + delta);
    }

    // Pone a 0 las 5 estadisticas (h, k, d, s, b) del jugador LOCAL en una sola escritura
    public static void ReiniciarLocal()
    {
        if (PhotonNetwork.LocalPlayer == null) return;
        Hashtable h = new Hashtable();
        h[Impactos] = 0;
        h[Kills] = 0;
        h[Muertes] = 0;
        h[Score] = 0;
        h[Blooms] = 0;
        PhotonNetwork.LocalPlayer.SetCustomProperties(h);
    }

    // ------------------------------------------------------------ v2: modo, listo y equipos
    // Modo de la sala actual (FFA si no existe la propiedad, p. ej. la sala de pruebas)
    public static ModoPartida ModoActual
    {
        get
        {
            int v = GetRoomInt(Modo, 0);
            if (v < 0 || v > 2) v = 0;
            return (ModoPartida)v;
        }
    }

    public static string NombreModo(int modo)
    {
        switch (modo)
        {
            case 1: return "Equipos";
            case 2: return "Cooperativo";
            default: return "Todos vs todos";
        }
    }

    public static bool GetBool(Player jugador, string clave)
    {
        if (jugador == null || jugador.CustomProperties == null) return false;
        object valor;
        if (!jugador.CustomProperties.TryGetValue(clave, out valor) || valor == null) return false;
        if (valor is bool) return (bool)valor;
        return ConvertirAInt(valor) != 0;
    }

    public static bool EstaListo(Player jugador) { return GetBool(jugador, Listo); }
    public static int GetEquipo(Player jugador) { return GetInt(jugador, Equipo); }

    public static string NombreEquipo(int equipo)
    {
        if (equipo == EquipoRojo) return "Rojo";
        if (equipo == EquipoAzul) return "Azul";
        return "Sin equipo";
    }

    public static Color ColorDeEquipo(int equipo)
    {
        if (equipo == EquipoRojo) return ColorRojo;
        if (equipo == EquipoAzul) return ColorAzul;
        return ColorSinEquipo;
    }

    // Texto TMP "#RRGGBB" del color del equipo
    public static string HexEquipo(int equipo)
    {
        if (equipo == EquipoRojo) return "#FF3030";
        if (equipo == EquipoAzul) return "#2F7BFF";
        return "#AAAAAA";
    }

    // true si en la sala actual (modo Equipos) los dos jugadores son del mismo equipo (y tienen equipo)
    public static bool SonAliados(Player a, Player b)
    {
        if (a == null || b == null) return false;
        if (ModoActual != ModoPartida.Equipos) return false;
        int ea = GetEquipo(a);
        return ea != SinEquipo && ea == GetEquipo(b);
    }

    // Escribe un bool en las propiedades del jugador LOCAL
    public static void SetLocalBool(string clave, bool valor)
    {
        if (PhotonNetwork.LocalPlayer == null) return;
        Hashtable h = new Hashtable();
        h[clave] = valor;
        PhotonNetwork.LocalPlayer.SetCustomProperties(h);
    }

    // Quita "listo" y el equipo del jugador LOCAL en una sola escritura
    public static void LimpiarListoYEquipoLocal()
    {
        if (PhotonNetwork.LocalPlayer == null) return;
        Hashtable h = new Hashtable();
        h[Listo] = false;
        h[Equipo] = SinEquipo;
        PhotonNetwork.LocalPlayer.SetCustomProperties(h);
    }

    // Puntos de un equipo (suma de "s" de sus jugadores)
    public static int PuntosEquipo(int equipo)
    {
        int total = 0;
        Player[] lista = PhotonNetwork.PlayerList;
        for (int i = 0; i < lista.Length; i++)
        {
            if (GetEquipo(lista[i]) == equipo) total += GetInt(lista[i], Score);
        }
        return total;
    }

    // Blooms eliminados entre todos (suma de "b")
    public static int BloomsTotales()
    {
        int total = 0;
        Player[] lista = PhotonNetwork.PlayerList;
        for (int i = 0; i < lista.Length; i++) total += GetInt(lista[i], Blooms);
        return total;
    }

    static int ConvertirAInt(object valor)
    {
        try { return System.Convert.ToInt32(valor); }
        catch { return 0; }
    }
}
