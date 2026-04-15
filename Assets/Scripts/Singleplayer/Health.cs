using UnityEngine;

public class Health : MonoBehaviour
{
    public int health = 20;

    public void TakeDamage(int damage)
    {
        health -= damage;

        if (health <= 0)
        {
            gameObject.SetActive(false);
            ScoreSingle scoreScript = FindObjectOfType<ScoreSingle>();
            if (scoreScript != null)
            {
                scoreScript.AddScore();
            }
        }
    }
}
