using UnityEngine;
using Photon.Pun;

public class Gun : MonoBehaviourPun
{
    public OVRInput.RawButton shootButton;

    [Header("Light Settings")]
    public Light gunLight;

    [Header("Gun Settings")]
    public float range = 100f;
    public int damage = 20;
    public int ammoCapacity = 20;
    public int batteries = 1;

    [Header("References")]
    public Transform firePoint;
    public LineRenderer linePrefab;

    [Header("Visuals")]
    public float lineDuration = 0.05f;

    private void Awake()
    {
        if (gunLight == null)
        {
            gunLight = GetComponent<Light>();
        }
    }

    private void Start()
    {
        gunLight = GetComponent<Light>();

    }

    void Update()
    {
        if (!photonView.IsMine) return;

    if (OVRInput.GetDown(shootButton))
    {
        if (ammoCapacity > 0)
        {
            photonView.RPC("RPC_Shoot", RpcTarget.All);
        }
    }

    if (OVRInput.GetDown(OVRInput.RawButton.B))
    {
        if (batteries > 0)
        {
                photonView.RPC("RPC_Reload", RpcTarget.All);
            }
    }
    }

    [PunRPC]
    void RPC_Shoot()
    {
        Ray ray = new Ray(firePoint.position, firePoint.forward);
        RaycastHit hit;

        Vector3 endPoint = firePoint.position + firePoint.forward * range;

        if (photonView.IsMine)
        {
            if (Physics.Raycast(ray, out hit, range))
            {
                endPoint = hit.point;
                if (hit.collider.CompareTag("Player"))
                {
                    Debug.Log("Hit player: " + hit.collider.name);
                    PhotonView targetPhotonView = hit.collider.GetComponentInParent<PhotonView>();
                    if (targetPhotonView != null)
                    {
                        targetPhotonView.RPC("TakeDamage", targetPhotonView.Owner, damage);
                    }
                }
            }
            ammoCapacity--;
        }

        LineRenderer line = Instantiate(linePrefab);
        line.SetPosition(0, firePoint.position);
        line.SetPosition(1, endPoint);

        Destroy(line.gameObject, lineDuration);
        gunLight.intensity -= 1.9f;
        gunLight.range -= 0.5f;
        // Solo el dueño modifica munición

    }

    [PunRPC]
    void RPC_Reload()
    {
        gunLight.intensity = 40;
        gunLight.range = 15;

        if (photonView.IsMine)
        {
            ammoCapacity = 20;
            batteries--;
        }
    }
}