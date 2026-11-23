using Photon.Pun;
using UnityEngine;
using UnityEngine.AI;

// Generador de Blooms del modo Cooperativo (objeto de escena en SampleScene, lo agrega el menu 8).
// Solo trabaja el MasterClient, solo en estado Jugando y solo si la sala es Cooperativa.
//  - Ritmo por tercio de la partida (GDD): cada 6 s, 5 s y 3 s.
//  - Maximo 15 Blooms vivos (rendimiento en Quest).
//  - Variante al azar entre los prefabs BloomNet_* de Resources.
//  - Aparece en un spawn del NetworkManager a 6 m o mas de cualquier jugador, colocado con NavMesh.SamplePosition.
// Si cambia el Master, el nuevo continua (su temporizador arranca de cero).
public class BloomSpawner : MonoBehaviour
{
    public string[] recursosBloom = { "BloomNet_Base", "BloomNet_LabCoat", "BloomNet_Engineer" };
    public float[] intervalosPorTercio = { 6f, 5f, 3f };
    public int maxVivos = 15;
    public float distanciaMinimaJugador = 6f;
    public float radioNavMesh = 2.5f;
    [Tooltip("Opcional: si se asignan se usan en lugar de los spawns del NetworkManager")]
    public Transform[] puntosDeSpawn;

    float proximo;

    void Update()
    {
        MatchManager mm = MatchManager.Instancia;
        bool puedeGenerar = mm != null && PhotonNetwork.InRoom && PhotonNetwork.IsMasterClient
            && mm.Estado == EstadoPartida.Jugando && MatchProps.ModoActual == ModoPartida.Cooperativo;
        if (!puedeGenerar)
        {
            proximo = Time.time + IntervaloActual(mm);
            return;
        }

        if (Time.time < proximo) return;
        proximo = Time.time + IntervaloActual(mm);

        if (BloomNet.Activos.Count >= maxVivos) return;
        if (recursosBloom == null || recursosBloom.Length == 0) return;

        Vector3 posicion;
        if (!BuscarPosicion(out posicion)) return;

        string recurso = recursosBloom[Random.Range(0, recursosBloom.Length)];
        Quaternion giro = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        PhotonNetwork.InstantiateRoomObject(recurso, posicion, giro);
    }

    // Segundos entre apariciones segun el tercio de la partida
    float IntervaloActual(MatchManager mm)
    {
        if (intervalosPorTercio == null || intervalosPorTercio.Length == 0) return 5f;
        int tercio = 0;
        if (mm != null && mm.Estado == EstadoPartida.Jugando && mm.duracionPartida > 0f)
        {
            tercio = Mathf.Clamp(Mathf.FloorToInt(mm.SegundosEnEstado / (mm.duracionPartida / 3f)), 0, 2);
        }
        return intervalosPorTercio[Mathf.Min(tercio, intervalosPorTercio.Length - 1)];
    }

    bool BuscarPosicion(out Vector3 resultado)
    {
        resultado = Vector3.zero;

        Transform[] spawns = puntosDeSpawn;
        if ((spawns == null || spawns.Length == 0) && NetworkManager.Instancia != null) spawns = NetworkManager.Instancia.spawnPoints;
        if (spawns == null || spawns.Length == 0) return false;

        PlayerHealth[] jugadores = FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None);

        // Spawns a >= distanciaMinimaJugador de todos los jugadores; si ninguno cumple, el mas lejano
        Transform mejor = null;
        float mejorDist = -1f;
        int candidatos = 0;
        Transform[] validos = new Transform[spawns.Length];
        for (int i = 0; i < spawns.Length; i++)
        {
            Transform sp = spawns[i];
            if (sp == null) continue;
            float minima = float.MaxValue;
            for (int j = 0; j < jugadores.Length; j++)
            {
                if (jugadores[j] == null) continue;
                Vector3 d = jugadores[j].PosicionCabeza - sp.position;
                d.y = 0f;
                if (d.magnitude < minima) minima = d.magnitude;
            }
            if (minima >= distanciaMinimaJugador) validos[candidatos++] = sp;
            if (minima > mejorDist)
            {
                mejorDist = minima;
                mejor = sp;
            }
        }

        Transform elegido = candidatos > 0 ? validos[Random.Range(0, candidatos)] : mejor;
        if (elegido == null) return false;

        // Bajar al piso (los spawns pueden estar en el aire) y luego al NavMesh
        Vector3 punto = elegido.position;
        RaycastHit golpe;
        if (Physics.Raycast(punto + Vector3.up * 0.5f, Vector3.down, out golpe, 10f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            punto = golpe.point;
        }

        NavMeshHit hit;
        if (!NavMesh.SamplePosition(punto, out hit, radioNavMesh, NavMesh.AllAreas)) return false;
        resultado = hit.position;
        return true;
    }
}
