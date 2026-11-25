using System.Collections.Generic;
using System.Text;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;

// HUD del jugador LOCAL (textos TMP en un canvas hijo del CenterEyeAnchor). Lo crea el menu 6.
// Muestra: estado/tiempo, bateria % y reserva, puntos/kills/muertes/impactos, "INVENCIBLE" y, al terminar, el marcador.
// v2: en Esperando/Cuenta regresiva el cuadro central lista los jugadores (listo / equipo) con las instrucciones del modo;
// el marcador final cambia segun el modo (FFA por jugador, Equipos por equipo, Cooperativo con Blooms eliminados).
// No usa simbolos Unicode (la fuente TMP del proyecto puede no tenerlos): el listo se muestra como texto de color.
public class MatchHUD : MonoBehaviour
{
    [Header("Textos (los asigna el menu 6)")]
    public TMP_Text textoEstado;      // "Esperando jugadores 1/2", "Empieza en 7", "2:41"
    public TMP_Text textoBateria;     // "BATERIA 85%  RESERVA 1/3"
    public TMP_Text textoKills;       // "KILLS 3   MUERTES 1"
    public TMP_Text textoInvencible;  // "INVENCIBLE 3"
    public TMP_Text textoMarcador;    // tabla final y, antes de empezar, la lista de espera

    [Header("Opciones")]
    public float segundosEntreActualizaciones = 0.1f;
    public int bateriaBaja = 20;

    float proximaActualizacion;
    readonly StringBuilder sb = new StringBuilder(256);

    void Update()
    {
        if (Time.unscaledTime < proximaActualizacion) return;
        proximaActualizacion = Time.unscaledTime + segundosEntreActualizaciones;

        MatchManager mm = MatchManager.Instancia;
        PlayerHealth ph = PlayerHealth.Local;
        Gun gun = Gun.Local;

        // ---- Estado / tiempo
        Poner(textoEstado, TextoEstado(mm));

        // ---- Bateria y reserva
        if (gun != null)
        {
            string color = gun.BateriaPct <= bateriaBaja ? "#F80000" : "#FFFFFF";
            Poner(textoBateria, "BATERIA <color=" + color + ">" + gun.BateriaPct + "%</color>   RESERVA " + gun.Reserva + "/" + gun.maxReserva);
        }
        else
        {
            Poner(textoBateria, "");
        }

        // ---- Kills y muertes (salen de las propiedades de jugador)
        if (PhotonNetwork.InRoom && PhotonNetwork.LocalPlayer != null)
        {
            Player yo = PhotonNetwork.LocalPlayer;
            string extra = (mm != null && mm.Modo == ModoPartida.Cooperativo)
                ? "  BLOOMS " + MatchProps.GetInt(yo, MatchProps.Blooms)
                : "  H " + MatchProps.GetInt(yo, MatchProps.Impactos);
            string equipo = "";
            if (mm != null && mm.Modo == ModoPartida.Equipos)
            {
                int t = MatchProps.GetEquipo(yo);
                if (t != MatchProps.SinEquipo) equipo = "<color=" + MatchProps.HexEquipo(t) + ">" + MatchProps.NombreEquipo(t).ToUpper() + "</color>   ";
            }
            Poner(textoKills, equipo + "PUNTOS " + MatchProps.GetInt(yo, MatchProps.Score)
                + "   K " + MatchProps.GetInt(yo, MatchProps.Kills)
                + "  D " + MatchProps.GetInt(yo, MatchProps.Muertes)
                + extra);
        }
        else
        {
            Poner(textoKills, "");
        }

        // ---- Invencible
        if (ph != null && ph.Invencible)
        {
            Poner(textoInvencible, "INVENCIBLE " + Mathf.CeilToInt(ph.SegundosInvencibleRestantes));
        }
        else
        {
            Poner(textoInvencible, "");
        }

        // ---- Marcador final
        if (mm != null && mm.Estado == EstadoPartida.Fin)
        {
            Poner(textoMarcador, ConstruirMarcador(mm));
        }
        else if (mm != null && PhotonNetwork.InRoom
            && (mm.Estado == EstadoPartida.Esperando || mm.Estado == EstadoPartida.CuentaRegresiva))
        {
            Poner(textoMarcador, ConstruirListaEspera(mm));
        }
        else
        {
            Poner(textoMarcador, "");
        }
    }

    string TextoEstado(MatchManager mm)
    {
        if (mm == null || !PhotonNetwork.InRoom) return "";
        switch (mm.Estado)
        {
            case EstadoPartida.Esperando:
                string motivo = mm.MotivoEspera();
                return motivo.Length > 0 ? motivo : "Todos listos";
            case EstadoPartida.CuentaRegresiva:
                return "Empieza en " + Mathf.CeilToInt(mm.SegundosRestantes);
            case EstadoPartida.Jugando:
                int total = Mathf.CeilToInt(mm.SegundosRestantes);
                return (total / 60) + ":" + (total % 60).ToString("00");
            case EstadoPartida.Fin:
                return "FIN DE LA PARTIDA";
        }
        return "";
    }

    static string NombreDe(Player p)
    {
        return string.IsNullOrEmpty(p.NickName) ? "Jugador " + p.ActorNumber : p.NickName;
    }

    // Lista de espera: modo, jugadores con listo / equipo e instrucciones
    string ConstruirListaEspera(MatchManager mm)
    {
        ModoPartida modo = mm.Modo;
        int miActor = PhotonNetwork.LocalPlayer != null ? PhotonNetwork.LocalPlayer.ActorNumber : -1;
        Player[] lista = PhotonNetwork.PlayerList;

        sb.Length = 0;
        sb.Append("<b>").Append(MatchProps.NombreModo((int)modo).ToUpper()).Append("</b>\n");
        for (int i = 0; i < lista.Length; i++)
        {
            Player p = lista[i];
            bool soyYo = p.ActorNumber == miActor;
            bool listo = MatchProps.EstaListo(p);
            if (soyYo) sb.Append("<color=#19C8E0>");
            sb.Append(NombreDe(p));
            if (soyYo) sb.Append(" (tu)");
            if (soyYo) sb.Append("</color>");
            if (modo == ModoPartida.Equipos)
            {
                int t = MatchProps.GetEquipo(p);
                sb.Append("<pos=40%><color=").Append(MatchProps.HexEquipo(t)).Append(">").Append(MatchProps.NombreEquipo(t)).Append("</color>");
            }
            sb.Append("<pos=72%>");
            if (listo) sb.Append("<color=#00F838>LISTO</color>");
            else sb.Append("<color=#F80000>no listo</color>");
            sb.Append('\n');
        }

        sb.Append('\n');
        sb.Append("<color=#9A9A9A>");
        if (mm.Estado == EstadoPartida.CuentaRegresiva) sb.Append("A: cancelar (desmarcar listo)");
        else if (modo == ModoPartida.Equipos) sb.Append("A: listo   X: Rojo   Y: Azul");
        else sb.Append("A: listo");
        sb.Append("</color>");
        return sb.ToString();
    }

    string ConstruirMarcador(MatchManager mm)
    {
        ModoPartida modo = mm.Modo;
        List<Player> lista = MatchManager.ObtenerMarcador();
        int miActor = PhotonNetwork.LocalPlayer != null ? PhotonNetwork.LocalPlayer.ActorNumber : -1;

        sb.Length = 0;
        sb.Append("<color=#D00000><b>MARCADOR</b></color>  <color=#9A9A9A>").Append(MatchProps.NombreModo((int)modo)).Append("</color>\n");

        if (modo == ModoPartida.Equipos)
        {
            int rojo = MatchProps.PuntosEquipo(MatchProps.EquipoRojo);
            int azul = MatchProps.PuntosEquipo(MatchProps.EquipoAzul);
            sb.Append("<color=").Append(MatchProps.HexEquipo(MatchProps.EquipoRojo)).Append(">ROJO ").Append(rojo).Append("</color>   ");
            sb.Append("<color=").Append(MatchProps.HexEquipo(MatchProps.EquipoAzul)).Append(">AZUL ").Append(azul).Append("</color>   ");
            if (rojo > azul) sb.Append("<color=").Append(MatchProps.HexEquipo(MatchProps.EquipoRojo)).Append("><b>GANA ROJO</b></color>");
            else if (azul > rojo) sb.Append("<color=").Append(MatchProps.HexEquipo(MatchProps.EquipoAzul)).Append("><b>GANA AZUL</b></color>");
            else sb.Append("<b>EMPATE</b>");
            sb.Append('\n');
        }
        else if (modo == ModoPartida.Cooperativo)
        {
            sb.Append("BLOOMS ELIMINADOS: <b>").Append(MatchProps.BloomsTotales()).Append("</b>\n");
        }

        string tercera = modo == ModoPartida.Cooperativo ? "B" : "K";
        sb.Append("<color=#9A9A9A>#   JUGADOR<pos=46%>SCORE<pos=62%>").Append(tercera).Append("<pos=74%>D<pos=86%>H</color>\n");

        // En Equipos se agrupa por equipo (Rojo y luego Azul) conservando el orden por score dentro de cada uno
        List<Player> orden = new List<Player>();
        if (modo == ModoPartida.Equipos)
        {
            for (int e = MatchProps.EquipoRojo; e <= MatchProps.EquipoAzul; e++)
                for (int i = 0; i < lista.Count; i++) if (MatchProps.GetEquipo(lista[i]) == e) orden.Add(lista[i]);
            for (int i = 0; i < lista.Count; i++) if (MatchProps.GetEquipo(lista[i]) == MatchProps.SinEquipo) orden.Add(lista[i]);
        }
        else
        {
            orden = lista;
        }

        for (int i = 0; i < orden.Count; i++)
        {
            Player p = orden[i];
            bool soyYo = p.ActorNumber == miActor;
            if (soyYo) sb.Append("<color=#19C8E0>");
            else if (modo == ModoPartida.Equipos) sb.Append("<color=").Append(MatchProps.HexEquipo(MatchProps.GetEquipo(p))).Append(">");
            sb.Append(i + 1).Append(".  ").Append(NombreDe(p));
            sb.Append("<pos=46%>").Append(MatchProps.GetInt(p, MatchProps.Score));
            sb.Append("<pos=62%>").Append(MatchProps.GetInt(p, modo == ModoPartida.Cooperativo ? MatchProps.Blooms : MatchProps.Kills));
            sb.Append("<pos=74%>").Append(MatchProps.GetInt(p, MatchProps.Muertes));
            sb.Append("<pos=86%>").Append(MatchProps.GetInt(p, MatchProps.Impactos));
            if (soyYo || modo == ModoPartida.Equipos) sb.Append("</color>");
            sb.Append('\n');
        }
        sb.Append("\n<color=#9A9A9A>Volviendo al lobby en ").Append(Mathf.CeilToInt(mm.SegundosRestantes)).Append("</color>");
        return sb.ToString();
    }

    // Solo escribe si el texto cambio (evita regenerar la malla de TMP en cada actualizacion)
    static void Poner(TMP_Text t, string valor)
    {
        if (t == null) return;
        if (t.text != valor) t.text = valor;
    }
}
