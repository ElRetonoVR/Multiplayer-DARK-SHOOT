using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

// Bateria recogible en red. Prefab "BatteryPickup" en Resources (lo crea el menu 4), instanciado SOLO por el
// MasterClient con PhotonNetwork.InstantiateRoomObject (BatterySpawner cada 20 s, o al morir un jugador).
//
// Recoger: el cliente local acerca su mano IZQUIERDA (<= 0.25 m) y aprieta el grip izquierdo. Entonces envia un
// RPC al Master con su ActorNumber. El Master valida que no este tomada, avisa al jugador (+1 reserva, via
// MatchManager) y hace PhotonNetwork.Destroy. Si el jugador ya tiene la reserva llena (3) ni siquiera lo pide.
public class BatteryPickup : MonoBehaviourPun
{
    // Todas las baterias vivas en este cliente (las cuenta el BatterySpawner)
    public static readonly List<BatteryPickup> Activas = new List<BatteryPickup>();

    [Header("Recoger")]
    public float distanciaRecoger = 0.25f;
    public float segundosEntrePedidos = 1f;

    [Header("Visual")]
    public Transform modelo;               // hijo que gira y flota (si es null se usa el primer hijo)
    public float giroGradosPorSegundo = 90f;
    public float amplitudFlote = 0.05f;
    public float velocidadFlote = 2f;

    [Header("Audio 3D (opcional)")]
    public AudioSource sonido;             // se reproduce al aparecer (el clip lo asignas tu)

    // true si la genero el BatterySpawner (las que suelta un jugador al morir no cuentan para el maximo de 3)
    public bool EsDelSpawner { get; private set; }

    bool tomada;
    float proximoPedido;
    Vector3 modeloBase;
    float fase;
    static OVRCameraRig rig;

    void OnEnable() { Activas.Add(this); }
    void OnDisable() { Activas.Remove(this); }

    void Start()
    {
        object[] datos = photonView.InstantiationData;
        EsDelSpawner = datos != null && datos.Length > 0 && datos[0] is int && (int)datos[0] == 1;

        if (modelo == null && transform.childCount > 0) modelo = transform.GetChild(0);
        if (modelo != null) modeloBase = modelo.localPosition;
        fase = Random.value * 6.28f;
        if (sonido != null && sonido.clip != null) sonido.Play();
    }

    void Update()
    {
        if (modelo != null)
        {
            modelo.Rotate(0f, giroGradosPorSegundo * Time.deltaTime, 0f, Space.Self);
            modelo.localPosition = modeloBase + Vector3.up * (Mathf.Sin(Time.time * velocidadFlote + fase) * amplitudFlote);
        }

        IntentarRecoger();
    }

    void IntentarRecoger()
    {
        if (tomada || Time.time < proximoPedido) return;

        Gun gun = Gun.Local;
        if (gun == null || gun.Reserva >= gun.maxReserva) return; // reserva llena: no se recoge

        if (rig == null) rig = FindFirstObjectByType<OVRCameraRig>();
        if (rig == null || rig.leftHandAnchor == null) return;

        Vector3 delta = rig.leftHandAnchor.position - transform.position;
        if (delta.sqrMagnitude > distanciaRecoger * distanciaRecoger) return;

        if (!OVRInput.Get(OVRInput.RawButton.LHandTrigger)) return; // grip izquierdo

        PedirRecoger();
    }

    // Pide al Master recoger esta bateria (tambien lo usa el modo escritorio)
    public void PedirRecoger()
    {
        if (tomada || Time.time < proximoPedido) return;
        Gun gun = Gun.Local;
        if (gun == null || gun.Reserva >= gun.maxReserva) return;
        proximoPedido = Time.time + segundosEntrePedidos;
        photonView.RPC("RPC_Pedir", RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber);
    }

    // Se ejecuta en el MASTER
    [PunRPC]
    void RPC_Pedir(int actorNumber)
    {
        if (!PhotonNetwork.IsMasterClient || tomada) return;
        if (PhotonNetwork.CurrentRoom == null) return;

        Player jugador = PhotonNetwork.CurrentRoom.GetPlayer(actorNumber);
        if (jugador == null) return;

        tomada = true; // la primera peticion gana

        MatchManager mm = MatchManager.Instancia;
        if (mm != null && mm.photonView != null)
        {
            mm.photonView.RPC("RPC_SumarBateria", jugador);
        }
        PhotonNetwork.Destroy(gameObject);
    }
}
