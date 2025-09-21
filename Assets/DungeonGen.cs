using System;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEditor;


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
    // public GameObjecjt doorPrefab;
    public Transform root;

    [Header("Layout")]
    public Vector3 cellSize = new(16f, 16f, 16f);
    public int maxRooms = 25;
    public int maxChildrenPerRoom = 3;
    public int seed = 0;

    // 내부상태
    private readonly Dictionary<Vector3Int, Room> map = new(); // 방 좌표, 방
    private readonly List<Vector3Int> frontier = new(); // 방 좌표

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
        foreach (Transform child in root) DestroyImmediate(child.gameObject);
        map.Clear();
        frontier.Clear();

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
        foreach (var kv in map) BuildCovers(kv.Value);
    }

    private Room PlaceRoom(Vector3Int grid)
    {   

        // Vector3.Scale = 벡터의 성분별 곱셈
        var worldPos = Vector3.Scale((Vector3)grid, cellSize);
        var go = Instantiate(roomShellPrefab, worldPos, Quaternion.identity, root);
        var r = new Room { grid = grid, doorMask = 0, instance = go };
        map[grid] = r;
        return r;
    }

    private void BuildCovers(Room r)
    {
        var parent = r.instance.transform;

        foreach (var (dir, name, vertical) in SocketSpec)
        {
            // 1) doorMask 기반
            bool open = (r.doorMask & (1 << DirUtil.Bit(dir))) != 0;

            // 열려있으면 아무것도 안함
            //if (open && doorPrefab) Instantiate(doorPrefab, t.position, tag.rotation, parent);
            if (open) continue;

            // t = 소켓 , name = Socket_X+ 이런거임
            var t = parent.Find(name);
            // if (!t)
            // {
            //     Debug.LogWarning($"{parent.name}: socket '{name}' not found.");
            //     continue;
            // }

            GameObject prefab = (dir == Dir.YPlus || dir == Dir.YMinus) ? capPrefapY :
                                (dir == Dir.XPlus || dir == Dir.XMinus) ? capPrefapX : capPrefapZ;

            //if (!prefab) { Debug.LogWarning($"Missing cap prefab for {(vertical ? "Y" : "XZ")} direction."); continue; }
            
            Instantiate(prefab, t.position, t.rotation, parent);
        }
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
    }
}
