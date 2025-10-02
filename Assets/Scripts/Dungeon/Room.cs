using UnityEngine;

public enum RoomKind { Default, Combat, Shop, Puzzle }

[System.Serializable]
public class Room
{
    public Vector3Int grid;     // 정수 격자 좌표
    public int doorMask;        // 6비트 (열림 = 1, 닫힘 =0)
    public GameObject instance;
    public RoomKind kind;
}
