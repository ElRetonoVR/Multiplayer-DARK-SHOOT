using UnityEngine;
using Photon.Pun;

public class LeaveRoom : MonoBehaviourPunCallbacks
{
    // Escena a la que se regresa al salir de la sala (debe estar en Build Settings)
    public string escenaAlSalir = "Lobby";

    void Update()
    {
        // Boton Menu/Start del control izquierdo: salir al Lobby
        if (OVRInput.GetDown(OVRInput.Button.Start))
        {
            Leave();
        }
    }

    public void Leave()
    {
        NetworkManager.SalirDeLaSala();
    }

    public override void OnLeftRoom()
    {
        // Si NetworkManager ya pidio la carga, CargarEscena ignora esta segunda llamada
        NetworkManager.CargarEscena(escenaAlSalir);
    }
}
