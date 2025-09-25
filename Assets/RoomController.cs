using UnityEngine;
using System;

public class RoomController : MonoBehaviour
{
    [SerializeField] DoorController[] doors;
    [SerializeField] EnemySpawner[] spawners;
    public event Action OnAllEnemiesDead;

    int alive = 0;
    bool activated = false;

    public void Init(DoorController[] ds, EnemySpawner[] es)
    {
        doors = ds;
        spawners = es;
        LockDoors(true);
    }

    // 방활성화 -> 적다죽으면 ClearRoom()
    public void Activate(Transform player)
    {
        if (activated) return;
        activated = true;

        LockDoors(true);
        alive = 0;

        foreach (var sp in spawners)
        {
            sp.Spawn(this, player);
        }

        // 적이 나오지 않는 방
        if (alive <= 0) ClearRoom();
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
