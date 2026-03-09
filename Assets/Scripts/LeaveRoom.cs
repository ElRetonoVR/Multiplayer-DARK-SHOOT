using UnityEngine;
using Photon.Pun;
using UnityEngine.SceneManagement;

public class LeaveRoom : MonoBehaviourPunCallbacks
{
    public void Leave()
    {
        PhotonNetwork.LeaveRoom();
    }

    public override void OnLeftRoom()
    {
        // Load the main menu or respawn scene here
        SceneManager.LoadScene("Menu");
    }
}
