using UnityEngine;

public class EnemySpawner : MonoBehaviour
{

    public GameObject enemyPrefab;
    public int enemyCount = 10;
    public float spanwRange = 20f;

    public Transform player;


    public void Spawn(RoomController owner, Transform playerRef)
    {
        if (playerRef) player = playerRef;

        var ai = enemyPrefab.GetComponent<EnemyAI>();
        if (ai != null) ai.player = player;


        for (int i = 0; i < enemyCount; i++)
        {
            Vector3 randomPos = transform.position + new Vector3(
                Random.Range(-spanwRange, spanwRange),
                0f,
                Random.Range(-spanwRange, spanwRange)
            );

            var go = Instantiate(enemyPrefab, randomPos, Quaternion.identity);
            var eh = go.GetComponent<EnemyHealth>();
            if (eh != null) owner.Register(eh);
        }
    }
}
