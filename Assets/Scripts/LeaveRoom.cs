using UnityEngine;
using Photon.Pun;
using UnityEngine.SceneManagement;

public class LeaveRoom : MonoBehaviourPunCallbacks
{
    // Escena a la que se regresa al salir de la sala (debe estar en Build Settings)
    public string escenaAlSalir = "Lobby";

    public void Leave()
    {
        PhotonNetwork.LeaveRoom();
    }

    public override void OnLeftRoom()
    {
        SceneManager.LoadScene(escenaAlSalir);
    }
}
