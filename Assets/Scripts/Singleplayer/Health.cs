using UnityEngine;

public class Health : MonoBehaviour
{
    public int health = 20; // Vida con la que aparece el enemigo
    private int currentHealth;

    // Se llama cada vez que el pool de SpawnEnemy reactiva al enemigo
    void OnEnable()
    {
        currentHealth = health;
    }

    public void TakeDamage(int damage)
    {
        if (currentHealth <= 0) return; // Ya esta muerto, evita sumar puntaje doble

        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            gameObject.SetActive(false);
            ScoreSingle scoreScript = FindFirstObjectByType<ScoreSingle>();
            if (scoreScript != null)
            {
                scoreScript.AddScore();
            }
        }
    }
}
