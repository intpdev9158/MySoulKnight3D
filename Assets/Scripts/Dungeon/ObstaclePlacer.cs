using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 방 크기(기본 32³)에서 벽 두께(기본 2)를 제외한 내부(예: 28³)에
/// 일정 보폭(blockSize)으로 큐브를 랜덤 배치한다.
/// - 경로 보장/문/스폰 관련 없음 (완전 미니멀)
/// - 피벗이 코너면 originAtCorner=true, 센터면 false
/// - 컨텍스트 메뉴로 Rebuild/지우기 지원
/// </summary>
[ExecuteAlways]
public class ObstaclePlacer : MonoBehaviour
{
    [Header("Room Grid (cells)")]
    public Vector3Int totalSize = new(32, 32, 32); // 전체 셀 수 (각 축)
    [Min(1)] public int wallThickness = 2;         // 양쪽 벽 두께(셀)
    [Min(0.001f)] public float unitSize = 1f;      // 셀 1칸 월드 길이
    [Tooltip("true: (0,0,0)이 방 코너 / false: (0,0,0)이 방 중앙")]
    public bool originAtCorner = false;

    [Header("Blocky Placement")]
    [Min(1)] public int blockSize = 2;             // 1=1x1x1, 2=2x2x2 등 “보폭/블록 크기”

    [Header("Random Fill")]
    [Range(0f, 1f)] public float fillProb = 0.3f;  // 블록 배치 확률
    public bool useNoise = true;                    // Perlin 질감 섞기
    [Range(0.01f, 1f)] public float noiseScale = 0.1f;
    public int seed = 12345;

    [Header("Limits")]
    [Tooltip("생성 개수 상한(0이면 자동계산 상한 사용)")]
    public int maxObstacles = 0;

    [Header("References")]
    [Tooltip("생성할 큐브 프리팹(기본 1m 큐브 권장, 머티리얼은 GPU Instancing 가능)")]
    public GameObject cubePrefab;
    [Tooltip("생성물을 담을 부모. 비어있으면 자동으로 'Obstacles' 생성")]
    public Transform container;

    // 내부 그리드(포함-포함)에서의 절대 셀 인덱스 경계
    Vector3Int innerMin; // (wallThickness, wallThickness, wallThickness)
    Vector3Int innerMax; // (totalSize - wallThickness - 1)

    void OnValidate()
    {
        // 정수 오버로드로 안전하게
        int smallest = Mathf.Min(totalSize.x, Mathf.Min(totalSize.y, totalSize.z));
        int maxAllowed = Mathf.Max(1, smallest / 4);
        wallThickness = Mathf.Clamp(wallThickness, 1, maxAllowed);

        unitSize   = Mathf.Max(0.001f, unitSize);
        blockSize  = Mathf.Max(1, blockSize);
        noiseScale = Mathf.Clamp(noiseScale, 0.01f, 1f);
    }

    [ContextMenu("Rebuild Obstacles")]
    public void Rebuild()
    {
        if (!cubePrefab)
        {
            Debug.LogWarning($"[{name}] cubePrefab 비어있음.");
            return;
        }

        PrepareContainer();

        // 내부 그리드 경계(포함-포함)
        innerMin = new Vector3Int(wallThickness, wallThickness, wallThickness);
        innerMax = new Vector3Int(
            totalSize.x - wallThickness - 1,
            totalSize.y - wallThickness - 1,
            totalSize.z - wallThickness - 1
        );

        int sx = innerMax.x - innerMin.x + 1; // 예: 28
        int sy = innerMax.y - innerMin.y + 1; // 예: 28
        int sz = innerMax.z - innerMin.z + 1; // 예: 28
        if (sx <= 0 || sy <= 0 || sz <= 0)
        {
            Debug.LogError($"[{name}] 내부 격자 크기 비정상. totalSize/wallThickness 확인.");
            return;
        }

        ClearChildren();

        // 블록 시작점 범위(경계 넘지 않게 마지막 시작 = size - blockSize)
        int xMax = sx - blockSize;
        int yMax = sy - blockSize;
        int zMax = sz - blockSize;

        var rng = new System.Random(seed);

        // 이론상 최대 블록 수
        int blocksX = Mathf.Max(0, sx / blockSize);
        int blocksY = Mathf.Max(0, sy / blockSize);
        int blocksZ = Mathf.Max(0, sz / blockSize);
        int theoreticalMax = blocksX * blocksY * blocksZ;

        // 상한 결정
        int cap = (maxObstacles > 0) ? Mathf.Min(maxObstacles, theoreticalMax) : theoreticalMax;

        int placed = 0;

        // 블록 단위 순회
        for (int x = 0; x <= xMax; x += blockSize)
        for (int y = 0; y <= yMax; y += blockSize)
        for (int z = 0; z <= zMax; z += blockSize)
        {
            // 블록의 "중심 셀" (정수)
            int cx = x + blockSize / 2;
            int cy = y + blockSize / 2;
            int cz = z + blockSize / 2;

            // 내부 인덱스(0..sx-1) → 절대 셀(0..totalSize-1)
            int ax = innerMin.x + cx;
            int ay = innerMin.y + cy;
            int az = innerMin.z + cz;

            // 확률 샘플
            float p = (float)rng.NextDouble();
            if (useNoise)
            {
                float nx = (cx + 100.123f) * noiseScale;
                float ny = (cy + 217.456f) * noiseScale;
                float nz = (cz + 345.789f) * noiseScale;
                float n  = Mathf.PerlinNoise(nx, nz) * Mathf.PerlinNoise(ny, nx); // 0..1
                p = p * 0.5f + n * 0.5f;
            }
            if (p >= fillProb) continue;

            // 블록 중심의 로컬/월드 좌표
            Vector3 localPos = CellAbsToLocal(new Vector3Int(ax, ay, az));
            Vector3 worldPos = transform.TransformPoint(localPos);

            // 스폰
            var go = Instantiate(cubePrefab, worldPos, Quaternion.identity, container);
            go.transform.localScale = Vector3.one * (unitSize * blockSize);

            placed++;
            if (placed >= cap) goto DONE;
        }

    DONE:
        // 끝
        ;
    }

    // ===== 좌표 변환 =====

    /// <summary> 방 로컬 최소 코너 좌표(피벗이 센터면 -size/2, 코너면 0) </summary>
    Vector3 GetRoomLocalMin()
    {
        if (originAtCorner) return Vector3.zero;
        Vector3 sizeLocal = new Vector3(totalSize.x, totalSize.y, totalSize.z) * unitSize;
        return -0.5f * sizeLocal;
    }

    /// <summary> 절대 셀 인덱스 → 방 로컬 좌표(셀 중심) </summary>
    Vector3 CellAbsToLocal(Vector3Int cellAbs)
    {
        Vector3 localMin = GetRoomLocalMin();
        return localMin + new Vector3(
            (cellAbs.x + 0.5f) * unitSize,
            (cellAbs.y + 0.5f) * unitSize,
            (cellAbs.z + 0.5f) * unitSize
        );
    }

    // ===== 컨테이너 준비/정리 =====

    void PrepareContainer()
    {
        if (container) return;

        var existing = transform.Find("Obstacles");
        if (existing) container = existing;
        else
        {
            var go = new GameObject("Obstacles");
            go.transform.SetParent(transform, false);
            container = go.transform;
        }
    }

    void ClearChildren()
    {
        if (!container) return;

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            for (int i = container.childCount - 1; i >= 0; --i)
                Object.DestroyImmediate(container.GetChild(i).gameObject);
        }
        else
#endif
        {
            for (int i = container.childCount - 1; i >= 0; --i)
                Object.Destroy(container.GetChild(i).gameObject);
        }
    }

    [ContextMenu("Clear Obstacles")]
    public void ClearObstacles() => ClearChildren();

#if UNITY_EDITOR
    // 내부 박스 기즈모(디버그)
    void OnDrawGizmosSelected()
    {
        Vector3 localMin = GetRoomLocalMin() + new Vector3(wallThickness, wallThickness, wallThickness) * unitSize;
        Vector3 sizeLocal = new Vector3(
            (totalSize.x - wallThickness * 2) * unitSize,
            (totalSize.y - wallThickness * 2) * unitSize,
            (totalSize.z - wallThickness * 2) * unitSize
        );

        Gizmos.color = Color.yellow;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(localMin + sizeLocal * 0.5f, sizeLocal);
    }
#endif
}
