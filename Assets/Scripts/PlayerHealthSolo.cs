using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerHealthSolo : MonoBehaviour
{
    public int maxHealth = 100;
    [SerializeField] int currentHealth;
    public float noDamageTimer = 0f;
    public float deathTimer = 0f;
    public bool isDead = false;
    public int countingDeath = 0;
    public Text textDeath;


    void Start()
    {
        currentHealth = maxHealth;
    }
    void Update()
    {
        noDamageTimer += Time.deltaTime;
        if (noDamageTimer > 8 && currentHealth < maxHealth)
        {
            currentHealth++;
        }
        if (isDead)
        {
            deathTimer += Time.deltaTime;
            if (deathTimer >= 5f)
            {
                isDead = false;
                deathTimer = 0f;
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            TakeDamage(20); // Assuming each enemy deals 20 damage
        }
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        noDamageTimer = 0f;
        if (currentHealth <= 0)
        {
            death();
        }
    }

    IEnumerator FlashContadorRed(float duration)
    {
        TimerScript timerScript = FindAnyObjectByType<TimerScript>();
        Color originalColor = timerScript.Contador.color;
        countingDeath++;
        textDeath.text = "Deaths: " + countingDeath;
        timerScript.Contador.color = Color.red;
        yield return new WaitForSeconds(duration);
        timerScript.Contador.color = originalColor;
    }
    public void death()
    {
        currentHealth = maxHealth;
        GunSingle gun = FindAnyObjectByType<GunSingle>();
        gun.ammoCapacity = 20;
        TimerScript timerScript = FindAnyObjectByType<TimerScript>();
        timerScript.timer -= 8f;
        isDead = true;
        StartCoroutine(FlashContadorRed(0.5f));
    }
}
    