using Photon.Pun;
using UnityEngine;

// Generador de baterias del centro del mapa (va junto al modelo BatteryStation, sobre la plataforma).
// Solo el MasterClient genera: una bateria cada 20 s, maximo 3 vivas, solo en estado Jugando.
// Si cambia el Master, el nuevo continua (cada cliente lleva su propio temporizador; el del nuevo master
// arranca de cero, asi que la primera bateria tras el cambio tarda un intervalo completo).
public class BatterySpawner : MonoBehaviour
{
    public string recursoPrefab = "BatteryPickup"; // nombre del prefab en una carpeta Resources
    public float intervalo = 20f;
    public int maxVivas = 3;
    public float alturaAparicion = 1.1f;           // sobre este objeto

    float proximo;

    void Update()
    {
        MatchManager mm = MatchManager.Instancia;
        bool puedeGenerar = mm != null && PhotonNetwork.InRoom && PhotonNetwork.IsMasterClient && mm.Estado == EstadoPartida.Jugando;
        if (!puedeGenerar)
        {
            proximo = Time.time + intervalo; // el reloj arranca cuando empieza el combate
            return;
        }

        if (Time.time < proximo) return;
        proximo = Time.time + intervalo;

        int vivas = 0;
        for (int i = 0; i < BatteryPickup.Activas.Count; i++)
        {
            BatteryPickup b = BatteryPickup.Activas[i];
            if (b != null && b.EsDelSpawner) vivas++;
        }
        if (vivas >= maxVivas) return;

        // data[0] = 1 marca "generada por el spawner" (las de muerte llevan 0)
        PhotonNetwork.InstantiateRoomObject(recursoPrefab, transform.position + Vector3.up * alturaAparicion, Quaternion.identity, 0, new object[] { 1 });
    }
}
