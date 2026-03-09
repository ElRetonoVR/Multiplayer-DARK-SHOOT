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
            ui = FindObjectOfType<ChangeUI>();
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
            //FindObjectOfType<LeaveRoom>().Leave();
            this.transform.position = LoserLounge; // Teletransporta al jugador a la sala de espera
            Debug.Log("Player moved to Loser Lounge.");
            if (ui != null)
            {
                ui.CambiarImagen(currentHealth);
            }
            deaths++;
            currentHealth = maxHealth; // Resetea la salud en lugar de salir de la sala
            Debug.Log("Player died. Total deaths: " + deaths);
        }
    }
}
