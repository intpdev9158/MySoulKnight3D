using UnityEngine;

public class EnemySpawner : MonoBehaviour
{

    public GameObject enemyPrefab;
    public int enemyCount = 10;
    public float spanwRange = 20f;

    public Transform player;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        var ai = enemyPrefab.GetComponent<EnemyAI>();
        if (ai != null) ai.player = player;

        SpawnEnemies();
    }

    // Update is called once per frame
    void Update()
    {

    }

    void SpawnEnemies()
    {
        for (int i = 0; i < enemyCount; i++)
        {
            Vector3 randomPos = player.position + new Vector3(
                Random.Range(-spanwRange, spanwRange),
                0f,
                Random.Range(-spanwRange, spanwRange)
            );

            Instantiate(enemyPrefab, randomPos, Quaternion.identity);
        }
    }
}
