using UnityEngine;
using Photon.Pun;

public class PlayerCombat : MonoBehaviourPun
{
    [Header("Gun Settings")]
    public float range = 100f;
    public int damage = 20;
    public int ammoCapacity = 20;
    public int maxAmmo = 20;
    public int batteries = 1;

    [Header("References")]
    public Transform firePoint;
    public LineRenderer linePrefab;

    [Header("Visuals")]
    public float lineDuration = 0.05f;

    public void TryShoot()
    {
        if (!photonView.IsMine) return;

        if (ammoCapacity <= 0) return;

        ammoCapacity--;

        photonView.RPC("RPC_Shoot", RpcTarget.All);
    }

    public void TryReload()
    {
        if (!photonView.IsMine) return;

        if (batteries <= 0) return;

        batteries--;
        ammoCapacity = maxAmmo;

        photonView.RPC("RPC_Reload", RpcTarget.All);
    }

    [PunRPC]
    void RPC_Shoot()
    {
        RaycastHit hit;

        if (Physics.Raycast(firePoint.position, firePoint.forward, out hit, range))
        {
            PlayerHealth target = hit.collider.GetComponentInParent<PlayerHealth>();

            if (target != null)
            {
                target.TakeDamage(damage);
            }
        }

        // Dibujar línea para todos
        if (linePrefab != null)
        {
            LineRenderer line = Instantiate(linePrefab);
            line.SetPosition(0, firePoint.position);
            line.SetPosition(1, firePoint.position + firePoint.forward * range);
            Destroy(line.gameObject, lineDuration);
        }
    }

    [PunRPC]
    void RPC_Reload()
    {
        Debug.Log("Reloaded!");
    }
}
