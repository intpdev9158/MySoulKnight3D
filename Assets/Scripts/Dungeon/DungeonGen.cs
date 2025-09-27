using System;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using Unity.VisualScripting;

#if UNITY_EDITOR
using UnityEditor;
#endif

public enum Dir { XPlus, XMinus, YPlus, YMinus, ZPlus, ZMinus }

public static class DirUtil
{
    public static Vector3Int step(Dir d) => d switch
    {
        Dir.XPlus => new(1, 0, 0),
        Dir.XMinus => new(-1, 0, 0),
        Dir.YPlus => new(0, 1, 0),
        Dir.YMinus => new(0, -1, 0),
        Dir.ZPlus => new(0, 0, 1),
        Dir.ZMinus => new(0, 0, -1),
        _ => throw new ArgumentOutOfRangeException(nameof(d))
    };

    public static Dir Opp(Dir d) => d switch
    {
        Dir.XPlus => Dir.XMinus,
        Dir.XMinus => Dir.XPlus,
        Dir.YPlus => Dir.YMinus,
        Dir.YMinus => Dir.YPlus,
        Dir.ZPlus => Dir.ZMinus,
        Dir.ZMinus => Dir.ZPlus,
        _ => throw new ArgumentOutOfRangeException(nameof(d))
    };

    public static int Bit(Dir d) => d switch
    {
        Dir.XPlus => 0,
        Dir.XMinus => 1,
        Dir.YPlus => 2,
        Dir.YMinus => 3,
        Dir.ZPlus => 4,
        Dir.ZMinus => 5,
        _ => throw new ArgumentOutOfRangeException(nameof(d))
    };
}

public class Room
{
    public Vector3Int grid;     // 정수 격자 좌표
    public int doorMask;        // 6비트 (열림 = 1, 닫힘 =0)
    public GameObject instance;
}


public class DungeonGen : MonoBehaviour
{
    [Header("Prefabs & Root")]
    public GameObject roomShellPrefab;
    public GameObject capPrefapX;
    public GameObject capPrefapZ;
    public GameObject capPrefapY;
    public GameObject doorPrefabX;
    public GameObject doorPrefabY;
    public GameObject doorPrefabZ;
    public Transform root;

    [Header("Layout")]
    public Vector3 cellSize = new(16f, 16f, 16f);
    public int maxRooms = 25;
    public int maxChildrenPerRoom = 3;
    public int seed = 0;

    // 내부상태
    private readonly Dictionary<Vector3Int, Room> map = new();  // 방 좌표, 방
    private readonly List<Vector3Int> frontier = new();         // 방 좌표
    private readonly List<Room> order = new();                  // 생성 순서
    private readonly List<RoomController> roomControllers = new(); // 방 컨트롤러 캐시

    // 소켓 이름 테이블
    private static readonly (Dir dir, string name, bool vertical)[] SocketSpec = new[] {
        (Dir.XPlus,     "Socket_X+", false),
        (Dir.XMinus,    "Socket_X-", false),
        (Dir.ZPlus,     "Socket_Z+", false),
        (Dir.ZMinus,    "Socket_Z-", false),
        (Dir.YPlus,     "Socket_Y+", true),
        (Dir.YMinus,    "Socket_Y-", true),
    };

    [ContextMenu("Generate")]
    public void Generate()
    {

        if (root)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                foreach (Transform child in root) DestroyImmediate(child.gameObject);
            }
            else
#endif
            {
                foreach (Transform child in root) Destroy(child.gameObject);
            }
        }

        map.Clear();
        frontier.Clear();
        order.Clear();
        roomControllers.Clear();

        // rng = System.Random()
        var rng = (seed == 0) ? new System.Random() : new System.Random(seed);

        // 시작 방
        var start = PlaceRoom(Vector3Int.zero);
        frontier.Add(start.grid);

        // 프런티어 확장
        while (map.Count < maxRooms && frontier.Count > 0)
        {
            int idx = rng.Next(frontier.Count); // 최대값안에서의 무작위 값 뽑음
            var cur = frontier[idx]; // 그 인덱스 의 좌표 사용
            frontier.RemoveAt(idx); // 뽑았으니 리스트에서 제거 (같은 씨앗 사용x)

            // 6방향 셔플
            var dirs = new List<Dir> { Dir.XPlus, Dir.XMinus, Dir.YPlus, Dir.YMinus, Dir.ZPlus, Dir.ZMinus };
            for (int i = 0; i < dirs.Count; i++)
            {
                int j = rng.Next(i, dirs.Count);
                // 튜플 형식으로 교환 temp(x) , swap기능
                (dirs[i], dirs[j]) = (dirs[j], dirs[i]);
            }

            int children = 0;
            foreach (var d in dirs)
            {
                if (map.Count >= maxRooms) break;
                if (children >= maxChildrenPerRoom) break;

                // 각 좌표 성분의 합
                var next = cur + DirUtil.step(d);
                if (map.ContainsKey(next)) continue; // 겹침 건너뛰기

                var rNext = PlaceRoom(next);

                // 양방향 "열림" 설정
                // doorMask값을 설정 
                // << : bit_shift ( ex 1 << n : 2의 n제곱임)
                // 1 << 0 : 000001
                // 1 << 1 : 000010
                map[cur].doorMask |= (1 << DirUtil.Bit(d));
                // 다음 생성될 방의 반대쪽문을 열어주는 거지
                map[next].doorMask |= (1 << DirUtil.Bit(DirUtil.Opp(d)));

                children++;
                frontier.Add(next);
            }
        }

        // 각 방에 Cap배치 
        foreach (var kv in map) BuildCoversAndDoors(kv.Value);
    }

    private Room PlaceRoom(Vector3Int grid)
    {

        // Vector3.Scale = 벡터의 성분별 곱셈
        var worldPos = Vector3.Scale((Vector3)grid, cellSize);
        var go = Instantiate(roomShellPrefab, worldPos, Quaternion.identity, root);
        var r = new Room { grid = grid, doorMask = 0, instance = go };
        map[grid] = r;
        order.Add(r); // 생성 순서 기록
        return r;
    }

    private void BuildCoversAndDoors(Room r)
    {
        var parent = r.instance.transform;

        // 방에 RoomController보장
        var roomCtrl = r.instance.GetComponent<RoomController>();
        if (!roomCtrl) roomCtrl = r.instance.AddComponent<RoomController>();

        var spawnedDoors = new List<DoorController>();
        int sourceIdx = order.IndexOf(r); // 현재 방 인덱스

        foreach (var (dir, name, vertical) in SocketSpec)
        {
            // 1) doorMask 기반
            bool open = (r.doorMask & (1 << DirUtil.Bit(dir))) != 0;
            // t = 소켓 , name = Socket_X+ 이런거임
            var t = parent.Find(name);

            GameObject doorPrefab = null;
            doorPrefab = (dir == Dir.YPlus || dir == Dir.YMinus) ? doorPrefabY :
                         (dir == Dir.XPlus || dir == Dir.XMinus) ? doorPrefabX : doorPrefabZ;

            if (open && doorPrefab)
            {
                // 열린 소켓 + 문 배치
                var dgo = Instantiate(doorPrefab, t.position, t.rotation, parent);
                var dc = dgo.GetComponent<DoorController>();
                if (!dc) dc = dgo.AddComponent<DoorController>();

                // DoorDir 매핑
                dc.Direction = dir switch
                {
                    Dir.XPlus => Doordir.XPlus,
                    Dir.XMinus => Doordir.XMinus,
                    Dir.YPlus => Doordir.YPlus,
                    Dir.YMinus => Doordir.YMinus,
                    Dir.ZPlus => Doordir.ZPlus,
                    Dir.ZMinus => Doordir.ZMinus,
                    _ => Doordir.XPlus
                };

                dc.OwnerRoom = roomCtrl;

                // 타깃 방 인덱스 계산 ( 이웃 좌표 -> order 인덱스)
                var nextGrid = r.grid + DirUtil.step(dir);
                int targetIdx = -1;
                for (int i = 0; i < order.Count; i++)
                {
                    if (order[i].grid == nextGrid)
                    {
                        targetIdx = i; break;
                    }
                }

                dc.sourceRoomIndex = sourceIdx;
                dc.targetRoomIndex = targetIdx;

                // 기본 잠금상태 ( 방 클리어 하면 RoomController가 해제)
                dc.SetLocked(true);
                spawnedDoors.Add(dc);
            }
            else if (!open)
            {
                GameObject capPrefab = (dir == Dir.YPlus || dir == Dir.YMinus) ? capPrefapY :
                                        (dir == Dir.XPlus || dir == Dir.XMinus) ? capPrefapX : capPrefapZ;

                if (capPrefab) Instantiate(capPrefab, t.position, t.rotation, parent);
            }
        }

        // 방 내 스포너 수집 & 초기화 연결
        var spawners = r.instance.GetComponentsInChildren<EnemySpawner>(true);
        roomCtrl.Init(spawnedDoors.ToArray(), spawners);
        roomControllers.Add(roomCtrl);

        var enterObj = new GameObject("EnterTrigger");
        enterObj.transform.SetParent(r.instance.transform, false);
        enterObj.transform.localPosition = new Vector3(0, cellSize.y / 2f, 0);

        var box = enterObj.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(
            Mathf.Max(0.1f, cellSize.x - 2f),
            Mathf.Max(0.1f, cellSize.y - 2f),
            Mathf.Max(0.1f, cellSize.z - 2f)
        );

        var enter = enterObj.AddComponent<RoomEnterTrigger>();
        enter.roomIndex = order.IndexOf(r);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0, 1, 1, 0.2f);
        foreach (var kv in map)
        {
            var p = Vector3.Scale((Vector3)kv.Key, cellSize);
            Gizmos.DrawWireCube(p + new Vector3(0, cellSize.y * 0.5f, 0), cellSize);
        }
    }
#endif

    [ContextMenu("Clear All")]
    public void ClearAll()   // 생성한 방/캡 전부 제거
    {
        if (!root) return;

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            // 에디터 모드: 즉시 삭제
            for (int i = root.childCount - 1; i >= 0; --i)
                DestroyImmediate(root.GetChild(i).gameObject);
        }
        else
#endif
        {
            // 플레이 모드: Destroy로 삭제
            for (int i = root.childCount - 1; i >= 0; --i)
                Destroy(root.GetChild(i).gameObject);
        }

        map.Clear();
        frontier.Clear();
        order.Clear();
        roomControllers.Clear();
    }

    public RoomController GetRoomController(int index)
    {
        if (index < 0 || index >= roomControllers.Count) return null;
        return roomControllers[index];
    }


    public bool IsLastRoom(int index) => (index == roomControllers.Count - 1);


    public Transform GetPlayerSpawn(int index)
    {
        var rc = GetRoomController(index);
        if (!rc) return null;

        var t = rc.transform.Find("PlayerSpawn");
        return t ? t : rc.transform; // 스폰 포인트 없으면 방 중심
    }

    public int GetNextIndex(int currentIndex, Doordir doorDir)
    {
        if (currentIndex < 0 || currentIndex >= order.Count) return -1;
        var cur = order[currentIndex];

        Dir stepDir = doorDir switch
        {
            Doordir.XPlus => Dir.XPlus,
            Doordir.XMinus => Dir.XMinus,
            Doordir.YPlus => Dir.YPlus,
            Doordir.YMinus => Dir.YMinus,
            Doordir.ZPlus => Dir.ZPlus,
            Doordir.ZMinus => Dir.ZMinus,
            _ => Dir.XPlus
        };

        var nextGrid = cur.grid + DirUtil.step(stepDir);

        for (int i = 0; i < order.Count; i++)
        {
            if (order[i].grid == nextGrid) return i;
        }

        return -1;
    }
}
