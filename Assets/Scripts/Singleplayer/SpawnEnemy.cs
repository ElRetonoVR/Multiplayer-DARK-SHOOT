using System.Collections.Generic;
using UnityEngine;

public class SpawnEnemy : MonoBehaviour
{
    public GameObject enemyPrefab;
    public Transform[] puntosDeSpawn;
    public int cantidadTotalEnemigos = 30;
    private List<GameObject> poolDeEnemigos = new List<GameObject>();
    private float timer = 0f, temporizadorSpawn = 0f;

    void Start()
    {
        // Llenamos el pool de una manera rápida
        for (int i = 0; i < cantidadTotalEnemigos; i++)
        {
            GameObject obj = Instantiate(enemyPrefab);
            obj.SetActive(false);
            poolDeEnemigos.Add(obj);
        }
    }

    void Update()
    {
        timer += Time.deltaTime;
        temporizadorSpawn += Time.deltaTime;
        // 1. TERNARIO para el intervalo (Se calcula al vuelo, eliminando la variable global)
        float spawnInterval = timer >= 120f ? 2f : (timer >= 60f ? 3f : 4f);
        if (temporizadorSpawn >= spawnInterval)
        {
            int enemigosSpawn = timer >= 120f ? 3 : (timer >= 60f ? 2 : 1);
            for (int i = 0; i < enemigosSpawn; i++)
            {
                AparecerEnemigoSegunTiempo(timer);
            }
            temporizadorSpawn = 0f;
        }
    }

    public void AparecerEnemigoSegunTiempo(float tiempoActual)
    {
        // 2. TERNARIO para el límite de puntos de aparicion 
        int limiteSpawners = tiempoActual >= 120f ? 4 : (tiempoActual >= 60f ? 3 : 2);
        Vector3 posicionAparicion = puntosDeSpawn[Random.Range(0, limiteSpawners)].position;

        // 3. MINIMA EXPRESION: Usamos .Find() en lugar del foreach gigante
        GameObject enemigoLibre = poolDeEnemigos.Find(e => !e.activeInHierarchy);

        if (enemigoLibre) // Si encontró uno libre (no es nulo)
        {
            enemigoLibre.transform.position = posicionAparicion;
            enemigoLibre.SetActive(true);
        }
    }
}