using System.Collections;
using UnityEngine;

public class SpawnEnemy : MonoBehaviour
{
    [SerializeField]
    public GameObject enemyPrefab;
    public GameObject spawner1;
    public GameObject spawner2;
    public GameObject spawner3;
    public GameObject spawner4;
    [SerializeField]
    private float spawnInterval = 5f;
    private float timer = 0f;
    private int selectSpawn = 0;


    void Start()
    {
        StartCoroutine(SpawnEnemies());
    }
    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= 60)
        {
            spawnInterval = 4f;
        }
        if (timer >= 120)
        {
            spawnInterval = 3f;
        }
    }
    private IEnumerator SpawnEnemies()
    {
        while (true)
        {
            selectSpawn = Random.Range(1, 5);
            if (selectSpawn == 1)
            {
                transform.position = spawner1.transform.position;
            }
            else if (selectSpawn == 2)
            {
                transform.position = spawner2.transform.position;
            }
            else if (selectSpawn == 3)
            {
                transform.position = spawner3.transform.position;
            }
            else if (selectSpawn == 4)
            {
                transform.position = spawner4.transform.position;
            }
            Instantiate(enemyPrefab, transform.position, Quaternion.identity);
            yield return new WaitForSeconds(spawnInterval);
        }
    }
}
