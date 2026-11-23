using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class SpawnEnemy : MonoBehaviour
{
    public GameObject enemyPrefab;
    [Header("Variantes del Bloom (DarkShot)")]
    [Tooltip("Si tiene al menos un prefab, cada enemigo del pool elige una variante al azar. Si esta vacio se usa enemyPrefab.")]
    public GameObject[] variantesEnemigo;

    [Header("Colocacion en el piso (DarkShot)")]
    [Tooltip("Hasta cuantos metros debajo del punto de spawn se busca el piso")]
    public float distanciaBusquedaPiso = 10f;
    [Tooltip("Radio para buscar el NavMesh alrededor del piso encontrado")]
    public float radioBusquedaNavMesh = 1.0f;
    public Transform[] puntosDeSpawn;
    public int cantidadTotalEnemigos = 30;

    [Header("Aparicion por tercio de la partida (GDD: cada 6 / 5 / 3 s)")]
    public float[] intervalosPorTercio = { 6f, 5f, 3f }; // Segundos entre oleadas en cada tercio
    public int[] enemigosPorTercio = { 1, 2, 3 };       // Enemigos que aparecen en cada oleada
    private List<GameObject> poolDeEnemigos = new List<GameObject>();
    private float timer = 0f, temporizadorSpawn = 0f;

    // New: reference to the countdown timer script and its initial value
    private TimerScript timerScript;
    private float initialTimerFromScript = -1f;

    void Start()
    {
        // Try to find the TimerScript in scene
        timerScript = FindFirstObjectByType<TimerScript>();
        if (timerScript != null)
        {
            initialTimerFromScript = timerScript.timer;
        }

        // Llenamos el pool de una manera rápida
        for (int i = 0; i < cantidadTotalEnemigos; i++)
        {
            GameObject prefabElegido = ElegirPrefab();
            if (prefabElegido == null) continue;
            GameObject obj = Instantiate(prefabElegido);
            if (obj == null) continue;
            obj.SetActive(false);
            poolDeEnemigos.Add(obj);
        }
    }

    // Raycast hacia abajo desde el punto de spawn: devuelve el piso que tiene debajo (o el mismo punto si no hay nada)
    Vector3 BuscarPiso(Vector3 desde)
    {
        RaycastHit[] golpes = Physics.RaycastAll(desde + Vector3.up * 0.1f, Vector3.down, distanciaBusquedaPiso, ~0, QueryTriggerInteraction.Ignore);
        float mejor = float.MaxValue;
        Vector3 piso = desde;
        foreach (RaycastHit g in golpes)
        {
            // Ignora a otros enemigos y al jugador
            if (g.collider.GetComponentInParent<EnemyFollow>() != null) continue;
            if (g.collider.CompareTag("Player")) continue;
            if (g.distance < mejor)
            {
                mejor = g.distance;
                piso = g.point;
            }
        }
        return piso;
    }

    // Elige al azar una variante (si hay) o devuelve el prefab clasico
    GameObject ElegirPrefab()
    {
        if (variantesEnemigo != null && variantesEnemigo.Length > 0)
        {
            // Intenta unas cuantas veces por si hay huecos vacios en el arreglo
            for (int intento = 0; intento < variantesEnemigo.Length * 2; intento++)
            {
                GameObject v = variantesEnemigo[Random.Range(0, variantesEnemigo.Length)];
                if (v != null) return v;
            }
        }
        return enemyPrefab;
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

        int tercio = elapsed >= twoThirds ? 2 : (elapsed >= oneThird ? 1 : 0);
        float spawnInterval = intervalosPorTercio[Mathf.Min(tercio, intervalosPorTercio.Length - 1)];
        if (temporizadorSpawn >= spawnInterval)
        {
            int enemigosSpawn = enemigosPorTercio[Mathf.Min(tercio, enemigosPorTercio.Length - 1)];
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
            // Los puntos de spawn estan en el aire (~2.6 m). Se baja el punto al piso que tienen debajo
            // y se coloca el agente en el NavMesh cercano a ese piso, para que no se pegue al techo
            // (el techo tambien esta horneado en el NavMesh).
            Vector3 posicionPiso = BuscarPiso(posicionAparicion);
            enemigoLibre.transform.position = posicionPiso;
            enemigoLibre.SetActive(true);

            NavMeshAgent agente = enemigoLibre.GetComponent<NavMeshAgent>();
            if (agente != null && agente.enabled)
            {
                NavMeshHit hitNav;
                if (NavMesh.SamplePosition(posicionPiso, out hitNav, radioBusquedaNavMesh, NavMesh.AllAreas))
                {
                    agente.Warp(hitNav.position);
                }
            }
        }
        else
        {
            Debug.LogWarning("SpawnEnemy: no hay enemigos libres en el pool. Considera aumentar cantidadTotalEnemigos.");
        }
    }
}