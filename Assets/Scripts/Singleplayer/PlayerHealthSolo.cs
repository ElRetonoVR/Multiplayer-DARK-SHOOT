using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerHealthSolo : MonoBehaviour
{
    public int maxHealth = 100; // Salud máxima del jugador
    [SerializeField] int currentHealth; // Salud actual del jugador que puede variar
    public float noDamageTimer = 0f; //Tiempo sin haber recibido daño para la regeneración de salud
    public float deathTimer = 0f; //Tiempo desde que el jugador "murió" para permitirle escapar o recuperarse antes de volver a la normalidad
    public bool isDead = false; // Muerto o vivo
    public Text textScore; // Puntaje que se muestra en pantalla
    ScoreSingle scoreSingle; // Referencia al componente ScoreSingle para actualizar el puntaje al morir

    void Start()
    {
        currentHealth = maxHealth;
        scoreSingle = FindAnyObjectByType<ScoreSingle>(); //Busca el componente ScoreSingle en la escena para actualizar el puntaje y no tener que arrastrarlo manualmente al inspector
    }
    void Update()
    {
        noDamageTimer += Time.deltaTime;
        textScore.text = "Score: " + scoreSingle.GetScore();
        if (noDamageTimer > 8 && currentHealth < maxHealth) //Si han pasado más de 8 segundos sin recibir daño y la salud actual es menor que la salud máxima, regenera salud hasta el tope
        {
            currentHealth++;
        }
        if (isDead) //Registra la muerte del jugador, le quita puntaje y le da la oportunidad de escapar o pelear por 5 segundos
        {
            deathTimer += Time.deltaTime; // Si han pasado más de X segundos desde la muerte, el jugador vuelve a la normalidad
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
            TakeDamage(20); // Cada enemigo causa 20 puntos de daño
        }
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return; // Evita recibir daño mientras está "muerto" para permitir al jugador escapar o recuperarse

        currentHealth -= damage;
        noDamageTimer = 0f;

        if (currentHealth <= 0)
        {
            death();
        }
    }

    public void death()
    {
        currentHealth = maxHealth;
        GunSingle gun = FindAnyObjectByType<GunSingle>();
        gun.gunLight.intensity = 40f;
        gun.gunLight.range = 15f;
        gun.ammoCapacity = 20;
        TimerScript timerScript = FindAnyObjectByType<TimerScript>();
        isDead = true;
        scoreSingle.downScore();
        
    }
}
    