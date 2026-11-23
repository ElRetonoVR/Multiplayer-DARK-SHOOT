using Photon.Pun;
using UnityEngine;

public class PlayerVR : MonoBehaviour
{
    public GameObject localRig;
    public GameObject remoteRig;

    PhotonView pv;

    void Awake()
    {
        pv = GetComponent<PhotonView>();

        if (pv.IsMine)
        {
            localRig.SetActive(true);
            remoteRig.SetActive(false);
        }
        else
        {
            // localRig ya viene apagado, pero por seguridad
            localRig.SetActive(false);
            remoteRig.SetActive(true);
        }
    }
}
