using UnityEngine;
using System;

[RequireComponent(typeof(ObstaclePlacer))]
public class RoomController : MonoBehaviour
{
    public Room data;
    [SerializeField] DoorController[] doors;
    [SerializeField] EnemySpawner[] spawners;

    public bool HasActivated { get; private set; }
    public bool IsCleared { get; private set; }

    public int RemainingEnemies => alive;

    public event Action OnAllEnemiesDead;
    int alive = 0;

    public DoorController GetDoor(Doordir dir)
    {
        if (doors == null) return null;
        for (int i = 0; i < doors.Length; i++)
        {
            if (doors[i] != null && doors[i].Direction == dir) return doors[i];
        }
        return null;
    }

    public void Init(Room room, DoorController[] ds, EnemySpawner[] es)
    {
        data = room;
        doors = ds;
        spawners = es;
        LockDoors(true);
        HasActivated = false;
        IsCleared = false;
        alive = 0;
    }

    public void UnlockLobby()
    {
        LockDoors(false);
        HasActivated = false;
        IsCleared = true;
    }

    // 방활성화 -> 적다죽으면 ClearRoom()
    // 방 활성화: 전투방이면 스폰 + 문잠금, 아니면 문 잠그지 않음
    public void Activate(Transform player)
    {
        if (HasActivated || IsCleared) return;
        HasActivated = true;
        
        if (data.kind == RoomKind.Combat)
        {
            Debug.Log("전투");
            var placer = GetComponent<ObstaclePlacer>();
            if (placer)
            {
                Debug.Log("장애물");
                placer.Rebuild();
            }

            // 전투방: 문 잠그고, 장애물 생성(옵션), 적 스폰
            LockDoors(true);

            alive = 0;
            foreach (var sp in spawners)
                sp.Spawn(this, player);

            // 적이 아예 안 나오면 즉시 클리어(문 열림)
            if (alive <= 0) ClearRoom();
        }
        else
        {
            Debug.Log("전투x");
            ClearRoom();
        }
    }


    public void Register(EnemyHealth eh)
    {
        alive++;
        eh.onDied -= OnEnemyDied;
        eh.onDied += OnEnemyDied;
    }

    void OnEnemyDied(EnemyHealth eh)
    {
        alive = Mathf.Max(0, alive - 1);
        if (alive <= 0) ClearRoom();
    }

    // 방 클리어
    void ClearRoom()
    {
        IsCleared = true;
        LockDoors(false);
        OnAllEnemiesDead?.Invoke();
    }

    public void LockDoors(bool locked)
    {
        if (doors == null) return;
        foreach (var d in doors)
        {
            d.SetLocked(locked);
        }
    }
}
