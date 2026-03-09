using Photon.Pun;
using UnityEngine;

public class VRLocalToNetworkMapper : MonoBehaviour
{
    [Header("Local (Scene)")]
    public Transform localHead;
    public Transform localLeftHand;
    public Transform localRightHand;

    [Header("Network (Prefab)")]
    public Transform netHead;
    public Transform netLeftHand;
    public Transform netRightHand;

    PhotonView pv;

    void Awake()
    {
        pv = GetComponent<PhotonView>();

        if (!pv.IsMine) return;

        // Buscar el LocalPlayerRig en la escena
        var rig = FindObjectOfType<OVRCameraRig>();

        localHead = rig.centerEyeAnchor;
        localLeftHand = rig.leftHandAnchor;
        localRightHand = rig.rightHandAnchor;
    }

    void LateUpdate()
    {
        if (!pv.IsMine) return;

        if (!localHead) return; // seguridad extra

        netHead.SetPositionAndRotation(localHead.position, localHead.rotation);
        netLeftHand.SetPositionAndRotation(localLeftHand.position, localLeftHand.rotation);
        netRightHand.SetPositionAndRotation(localRightHand.position, localRightHand.rotation);
    }
}
