/*using System;
using Unity.VisualScripting;
using UnityEngine;

public class EnemyAttack : MonoBehaviour
{
    [SerializeField] int attackDamage = 20;
    [SerializeField] GameObject colider;


    void OnCollisionEnter()
    {
        colider = GameObject.FindWithTag("Player");
        if (colider != null)
        {
            PlayerHealth playerHealth = colider.GetComponent<PlayerHealth>();
            if (playerHealth  != null )
            {
                playerHealth.TakeDamage(attackDamage);
                Debug.Log("Player hit! Damage dealt: ");

            }
        }
    }
}
*/