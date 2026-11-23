using UnityEngine;
using Photon.Pun;

public class PlayerHealth : MonoBehaviourPunCallbacks
{
    public int maxHealth = 100;
    public int currentHealth;
    public int deaths = 0;
    public Vector3 LoserLounge = new Vector3(1, 13, 0); // Posicion de la sala de espera
    private ChangeUI ui;

    void Start()
    {
        currentHealth = maxHealth;

        if (photonView.IsMine)
        {
            ui = FindFirstObjectByType<ChangeUI>();
        }
    }

    [PunRPC]
    public void TakeDamage(int damage)
    {
        if (!photonView.IsMine) return;

        currentHealth -= damage;

        if (ui != null)
        {
            ui.CambiarImagen(currentHealth);
        }

        Debug.Log("Player took damage. Current health: " + currentHealth);

        if (currentHealth <= 0)
        {
            TeleportToLoserLounge();
            deaths++;
            currentHealth = maxHealth; // Resetea la salud en lugar de salir de la sala

            // Actualiza el HUD con la salud ya restablecida
            if (ui != null)
            {
                ui.CambiarImagen(currentHealth);
            }

            Debug.Log("Player died. Total deaths: " + deaths);
        }
    }

    // El cuerpo de red sigue al OVRCameraRig local (VRLocalToNetworkMapper),
    // por eso hay que mover el rig y no solo este objeto.
    void TeleportToLoserLounge()
    {
        OVRCameraRig rig = FindFirstObjectByType<OVRCameraRig>();

        if (rig != null)
        {
            rig.transform.position = LoserLounge;
        }
        else
        {
            transform.position = LoserLounge;
        }

        Debug.Log("Player moved to Loser Lounge.");
    }
}
