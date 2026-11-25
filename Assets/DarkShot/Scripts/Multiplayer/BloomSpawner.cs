using Photon.Pun;
using UnityEngine;
using UnityEngine.AI;

// Generador de Blooms del modo Cooperativo (objeto de escena en SampleScene, lo agrega el menu 8).
// Solo trabaja el MasterClient, solo en estado Jugando y solo si la sala es Cooperativa.
//  - Igual que el single player (SpawnEnemy): por tercio de la partida, oleadas cada 6 s / 5 s / 3 s
//    de 1 / 2 / 3 Blooms, y se van abriendo los spawns de enemigos (los 2 primeros, luego 3, luego 4).
//  - Maximo 30 Blooms vivos (el pool del single player).
//  - Variante al azar entre los prefabs BloomNet_* de Resources.
//  - Aparece en un spawn del NetworkManager a 6 m o mas de cualquier jugador, colocado con NavMesh.SamplePosition.
// Si cambia el Master, el nuevo continua (su temporizador arranca de cero).
public class BloomSpawner : MonoBehaviour
{
    public string[] recursosBloom = { "BloomNet_Base", "BloomNet_LabCoat", "BloomNet_Engineer" };
    public float[] intervalosPorTercio = { 6f, 5f, 3f };
    public int[] enemigosPorTercio = { 1, 2, 3 };       // Blooms por oleada en cada tercio (como SpawnEnemy)
    public int[] spawnsAbiertosPorTercio = { 2, 3, 4 };  // cuantos de los puntosDeSpawn se usan en cada tercio
    public int maxVivos = 30;
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
        if (recursosBloom == null || recursosBloom.Length == 0) return;

        int tercio = TercioActual(mm);
        int cantidad = enemigosPorTercio != null && enemigosPorTercio.Length > 0
            ? enemigosPorTercio[Mathf.Min(tercio, enemigosPorTercio.Length - 1)] : 1;
        for (int i = 0; i < cantidad; i++)
        {
            if (BloomNet.Activos.Count >= maxVivos) return; // en el master cada Bloom se suma a Activos al crearse
            Vector3 posicion;
            if (!BuscarPosicion(tercio, out posicion)) return;

            string recurso = recursosBloom[Random.Range(0, recursosBloom.Length)];
            Quaternion giro = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            PhotonNetwork.InstantiateRoomObject(recurso, posicion, giro);
        }
    }

    // Segundos entre apariciones segun el tercio de la partida
    float IntervaloActual(MatchManager mm)
    {
        if (intervalosPorTercio == null || intervalosPorTercio.Length == 0) return 5f;
        return intervalosPorTercio[Mathf.Min(TercioActual(mm), intervalosPorTercio.Length - 1)];
    }

    // 0, 1 o 2 segun el tercio de la partida transcurrido
    int TercioActual(MatchManager mm)
    {
        if (mm == null || mm.Estado != EstadoPartida.Jugando || mm.duracionPartida <= 0f) return 0;
        return Mathf.Clamp(Mathf.FloorToInt(mm.SegundosEnEstado / (mm.duracionPartida / 3f)), 0, 2);
    }

    bool BuscarPosicion(int tercio, out Vector3 resultado)
    {
        resultado = Vector3.zero;

        Transform[] spawns = puntosDeSpawn;
        if (spawns != null && spawns.Length > 0 && spawnsAbiertosPorTercio != null && spawnsAbiertosPorTercio.Length > 0)
        {
            // Como el single player: al principio solo los primeros spawns de enemigos
            int abiertos = Mathf.Clamp(spawnsAbiertosPorTercio[Mathf.Min(tercio, spawnsAbiertosPorTercio.Length - 1)], 1, spawns.Length);
            if (abiertos < spawns.Length) System.Array.Resize(ref spawns, abiertos); // copia: no toca el campo
        }
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
        if (Physics.Raycast(punto + Vector3.up * 0.1f, Vector3.down, out golpe, 10f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            punto = golpe.point;
        }

        NavMeshHit hit;
        if (!NavMesh.SamplePosition(punto, out hit, radioNavMesh, NavMesh.AllAreas)) return false;
        resultado = hit.position;
        return true;
    }
}
