using System.Collections.Generic;
using UnityEngine;

public class SpawnEnemy : MonoBehaviour
{
    public GameObject enemyPrefab;
    public Transform[] puntosDeSpawn;
    public int cantidadTotalEnemigos = 30;
    private List<GameObject> poolDeEnemigos = new List<GameObject>();
    private float timer = 0f, temporizadorSpawn = 0f;

    // New: reference to the countdown timer script and its initial value
    private TimerScript timerScript;
    private float initialTimerFromScript = -1f;

    void Start()
    {
        // Try to find the TimerScript in scene
        timerScript = FindObjectOfType<TimerScript>();
        if (timerScript != null)
        {
            initialTimerFromScript = timerScript.timer;
        }

        // Llenamos el pool de una manera rápida
        for (int i = 0; i < cantidadTotalEnemigos; i++)
        {
            GameObject obj = Instantiate(enemyPrefab);
            if (obj == null) continue;
            obj.SetActive(false);
            poolDeEnemigos.Add(obj);
        }
    }

    void Update()
    {
        // If we have a TimerScript, compute elapsed from its countdown; otherwise use local timer
        float elapsed;
        if (timerScript != null && initialTimerFromScript > 0f)
        {
            float remaining = timerScript.timer;
            elapsed = Mathf.Clamp(initialTimerFromScript - remaining, 0f, initialTimerFromScript);
        }
        else
        {
            timer += Time.deltaTime;
            elapsed = timer;
        }

        temporizadorSpawn += Time.deltaTime;

        // Use fractions of the initial timer (1/3 and 2/3) to determine spawn behavior.
        // If TimerScript wasn't found, default initial to 180f to preserve previous behavior.
        float initial = (initialTimerFromScript > 0f) ? initialTimerFromScript : 180f;
        float oneThird = initial / 3f;
        float twoThirds = oneThird * 2f;

        float spawnInterval = elapsed >= twoThirds ? 2f : (elapsed >= oneThird ? 3f : 4f);
        if (temporizadorSpawn >= spawnInterval)
        {
            int enemigosSpawn = elapsed >= twoThirds ? 3 : (elapsed >= oneThird ? 2 : 1);
            for (int i = 0; i < enemigosSpawn; i++)
            {
                AparecerEnemigoSegunTiempo(elapsed);
            }
            temporizadorSpawn = 0f;
        }
    }

    public void AparecerEnemigoSegunTiempo(float tiempoActual)
    {
        // Determine limit based on thirds of the initial timer (maps original 180->120/60 behavior)
        float initial = (initialTimerFromScript > 0f) ? initialTimerFromScript : 180f;
        float oneThird = initial / 3f;
        float twoThirds = oneThird * 2f;

        int limiteSpawners = tiempoActual >= twoThirds ? 4 : (tiempoActual >= oneThird ? 3 : 2);

        // Ensure we don't index outside puntosDeSpawn
        limiteSpawners = Mathf.Clamp(limiteSpawners, 1, Mathf.Max(1, puntosDeSpawn != null ? puntosDeSpawn.Length : 1));
        int spawnIndex = Random.Range(0, limiteSpawners);
        Vector3 posicionAparicion = puntosDeSpawn[spawnIndex].position;

        // Find inactive enemy safely
        GameObject enemigoLibre = poolDeEnemigos.Find(e => e != null && !e.activeInHierarchy);

        if (enemigoLibre != null)
        {
            enemigoLibre.transform.position = posicionAparicion;
            enemigoLibre.SetActive(true);
        }
        else
        {
            Debug.LogWarning("SpawnEnemy: no hay enemigos libres en el pool. Considera aumentar cantidadTotalEnemigos.");
        }
    }
}