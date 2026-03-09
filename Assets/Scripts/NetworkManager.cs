using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class NetworkManager : MonoBehaviourPunCallbacks
{    
    public Transform[] spawnPoints;

    void Start()
    {
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinOrCreateRoom(
            "RoomVR",
            new RoomOptions { MaxPlayers = 8 },
            TypedLobby.Default
        );
    }


    public override void OnJoinedRoom()
    {
        SpawnPlayer();
    }

    void SpawnPlayer()
    {
        int actorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
        int spawnIndex = (actorNumber - 1) % spawnPoints.Length;
        Transform spawnPoint = spawnPoints[spawnIndex];
        PhotonNetwork.Instantiate(
            "PlayerVR",
            spawnPoint.position,
            spawnPoint.rotation
        );
        AlignToSpawn(spawnPoint);
    }

    void AlignToSpawn(Transform spawnPoint)
    {
        OVRCameraRig rig = FindObjectOfType<OVRCameraRig>();

        if (rig == null) return;

        rig.transform.position = spawnPoint.position;
        rig.transform.rotation = spawnPoint.rotation;
    }
}
