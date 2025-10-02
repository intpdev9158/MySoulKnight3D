using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 32³ 방에서 벽 두께를 제외한 내부(예: 28³)에
/// blockSize 보폭으로 "동굴형 클러스터" 큐브를 배치.
/// - 3D fBM 노이즈로 초기화 + 3D 셀룰러 오토마타로 스무딩
/// - 경로 보장/문/스폰 없음
/// - 피벗이 코너면 originAtCorner=true, 센터면 false
/// </summary>
[ExecuteAlways]
public class ObstaclePlacer : MonoBehaviour
{
    [Header("Room Grid (cells)")]
    public Vector3Int totalSize = new(32, 32, 32);
    [Min(1)] public int wallThickness = 4;
    [Min(0.001f)] public float unitSize = 1f;
    [Tooltip("true: (0,0,0)이 방 코너 / false: (0,0,0)이 방 중앙")]
    public bool originAtCorner = false;

    [Header("Block Size")]
    [Min(1)] public int blockSize = 2;   // 2=2x2x2 추천

    [Header("Noise Seed")]
    [Range(0f, 1f)] public float baseFill = 0.48f; // 초기 채움 기준 (노이즈와 혼합)
    public int seed = 12345;

    [Header("3D fBM Noise")]
    public bool useNoise = true;
    [Tooltip("덩어리 수")]
    [Range(0.01f, 1f)] public float noiseScale = 0.2f; // 작을수록 큰 덩어리
    [Tooltip("연결될 수 있는 큐브 수")]
    [Range(1, 6)] public int noiseOctaves = 2;
    [Range(0.1f, 1f)] public float noiseGain = 0.5f;     // 각 옥타브 가중치(감쇠)
    [Range(1.5f, 4f)] public float noiseLacunarity = 2f; // 주파수 배수

    [Header("Cellular Automata (3D)")]
    [Tooltip("스무딩 반복 횟수(2~5 권장)")]
    [Range(0, 8)] public int smoothSteps = 1;
    [Tooltip("살아있는 이웃 수가 이 값 미만이면 죽음(26 이웃)")]
    [Range(0, 26)] public int deathLimit = 9;
    [Tooltip("죽어있는 셀도 이 값 초과 이웃이면 탄생")]
    [Range(0, 26)] public int birthLimit = 14;

    [Header("Limits")]
    [Tooltip("생성 상한(0=자동 상한)")]
    public int maxObstacles = 0;

    [Header("References")]
    public GameObject cubePrefab;
    public Transform container;

    // 필드 추가
    [Header("Seed")]
    public bool randomizeSeedOnRebuild = true;

    // 내부 격자(포함-포함)의 절대 인덱스 경계
    Vector3Int innerMin, innerMax;

    bool obstaclesSpawned = false;

    void OnValidate()
    {
        int smallest = Mathf.Min(totalSize.x, Mathf.Min(totalSize.y, totalSize.z));
        int maxAllowed = Mathf.Max(1, smallest / 4);
        wallThickness = Mathf.Clamp(wallThickness, 1, maxAllowed);

        unitSize = Mathf.Max(0.001f, unitSize);
        blockSize = Mathf.Max(1, blockSize);

        noiseScale = Mathf.Clamp(noiseScale, 0.01f, 1f);
        noiseOctaves = Mathf.Clamp(noiseOctaves, 1, 6);
        noiseGain = Mathf.Clamp(noiseGain, 0.1f, 1f);
        noiseLacunarity = Mathf.Clamp(noiseLacunarity, 1.5f, 4f);

        deathLimit = Mathf.Clamp(deathLimit, 0, 26);
        birthLimit = Mathf.Clamp(birthLimit, 0, 26);
    }

    [ContextMenu("Rebuild Obstacles")]
    public void Rebuild()
    {
        if (!cubePrefab)
        {
            Debug.LogWarning($"[{name}] cubePrefab 비어있음.");
            return;
        }

        if (obstaclesSpawned) return;

        PrepareContainer();
        ClearChildren();

        // Rebuild() 맨 앞쪽에 추가
        if (randomizeSeedOnRebuild)
        {
            // 유니티 난수에서 임의 시드 뽑기 (에디터/런타임 모두 OK)
            seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
        }

        // 내부 경계 (포함-포함) → 28³ 등
        innerMin = new Vector3Int(wallThickness, wallThickness, wallThickness);
        innerMax = new Vector3Int(
            totalSize.x - wallThickness - 1,
            totalSize.y - wallThickness - 1,
            totalSize.z - wallThickness - 1
        );

        int sx = innerMax.x - innerMin.x + 1;
        int sy = innerMax.y - innerMin.y + 1;
        int sz = innerMax.z - innerMin.z + 1;
        if (sx <= 0 || sy <= 0 || sz <= 0)
        {
            Debug.LogError($"[{name}] 내부 격자 크기가 비정상입니다.");
            return;
        }

        // 블록 인덱스 그리드 크기(끝 남는 칸도 포함되게 ceil)
        int bx = Mathf.CeilToInt(sx / (float)blockSize);
        int by = Mathf.CeilToInt(sy / (float)blockSize);
        int bz = Mathf.CeilToInt(sz / (float)blockSize);

        // 1) 초기화: fBM 3D + baseFill 로 시드
        bool[,,] grid = new bool[bx, by, bz];
        var rng = new System.Random(seed);
        float ox = rng.Next(0, 10000);
        float oy = rng.Next(0, 10000);
        float oz = rng.Next(0, 10000);

        for (int x = 0; x < bx; x++)
            for (int y = 0; y < by; y++)
                for (int z = 0; z < bz; z++)
                {
                    // 블록 중심(내부좌표) → 노이즈 샘플 좌표
                    float cx = (x + 0.5f) * blockSize;
                    float cy = (y + 0.5f) * blockSize;
                    float cz = (z + 0.5f) * blockSize;

                    // 0..1의 fBM 3D
                    float n = useNoise ? FBM3D(cx + ox, cy + oy, cz + oz, noiseScale, noiseOctaves, noiseGain, noiseLacunarity) : 0.5f;

                    // baseFill을 기준으로 스칼라 임계값 결정 (n이 클수록 채움)
                    bool alive = n >= (1f - baseFill);
                    grid[x, y, z] = alive;
                }

        // 2) 셀룰러 오토마타 스무딩 (26-이웃)
        for (int step = 0; step < smoothSteps; step++)
        {
            grid = StepAutomata(grid, deathLimit, birthLimit);
        }

        // 3) 배치
        int theoreticalMax = bx * by * bz;
        int cap = (maxObstacles > 0) ? Mathf.Min(maxObstacles, theoreticalMax) : theoreticalMax;

        int placed = 0;
        for (int x = 0; x < bx; x++)
            for (int y = 0; y < by; y++)
                for (int z = 0; z < bz; z++)
                {
                    if (!grid[x, y, z]) continue;

                    // 이 블록의 내부 인덱스 범위 시작점(0..sx-1)
                    int ix = x * blockSize;
                    int iy = y * blockSize;
                    int iz = z * blockSize;

                    // 블록의 "중심 셀" (경계 넘어가면 안쪽으로 클램프)
                    int cx = Mathf.Min(ix + blockSize / 2, sx - 1);
                    int cy = Mathf.Min(iy + blockSize / 2, sy - 1);
                    int cz = Mathf.Min(iz + blockSize / 2, sz - 1);

                    // 내부→절대 셀 인덱스
                    int ax = innerMin.x + cx;
                    int ay = innerMin.y + cy;
                    int az = innerMin.z + cz;

                    Vector3 localPos = CellAbsToLocal(new Vector3Int(ax, ay, az));
                    Vector3 worldPos = transform.TransformPoint(localPos);

                    var go = Instantiate(cubePrefab, worldPos, Quaternion.identity, container);
                    go.transform.localScale = Vector3.one * (unitSize * blockSize);

                    placed++;
                    if (placed >= cap) goto DONE;
                }

            DONE:
        ;
        obstaclesSpawned = true;
    }

    // ===== 3D Cellular Automata =====

    static bool[,,] StepAutomata(bool[,,] src, int deathLimit, int birthLimit)
    {
        int sx = src.GetLength(0), sy = src.GetLength(1), sz = src.GetLength(2);
        bool[,,] dst = new bool[sx, sy, sz];

        for (int x = 0; x < sx; x++)
        for (int y = 0; y < sy; y++)
        for (int z = 0; z < sz; z++)
        {
            int aliveN = CountAliveNeighbors(src, x, y, z);
            if (src[x, y, z])
                dst[x, y, z] = aliveN >= deathLimit; // 살아있고 이웃 적으면 죽음
            else
                dst[x, y, z] = aliveN > birthLimit;   // 죽어있고 이웃 많으면 탄생
        }
        return dst;
    }

    static int CountAliveNeighbors(bool[,,] g, int x, int y, int z)
    {
        int sx = g.GetLength(0), sy = g.GetLength(1), sz = g.GetLength(2);
        int c = 0;
        for (int dx = -1; dx <= 1; dx++)
        for (int dy = -1; dy <= 1; dy++)
        for (int dz = -1; dz <= 1; dz++)
        {
            if (dx == 0 && dy == 0 && dz == 0) continue;
            int nx = x + dx, ny = y + dy, nz = z + dz;
            if (nx < 0 || ny < 0 || nz < 0 || nx >= sx || ny >= sy || nz >= sz) continue; // 바깥은 빈 공간 취급
            if (g[nx, ny, nz]) c++;
        }
        return c;
    }

    // ===== 3D fBM (Perlin 2D 조합) =====
    // Mathf.PerlinNoise는 2D이므로 xz, yx, yz 평면을 샘플해 평균
    static float FBM3D(float x, float y, float z, float scale, int octaves, float gain, float lacunarity)
    {
        float amp = 1f, freq = scale, sum = 0f, denom = 0f;
        for (int o = 0; o < octaves; o++)
        {
            float n1 = Mathf.PerlinNoise((x + 19.1f) * freq, (z + 73.2f) * freq);
            float n2 = Mathf.PerlinNoise((y + 37.7f) * freq, (x + 11.8f) * freq);
            float n3 = Mathf.PerlinNoise((y + 53.4f) * freq, (z + 29.6f) * freq);
            float n = (n1 + n2 + n3) / 3f; // 0..1
            sum += n * amp;
            denom += amp;
            amp *= gain;
            freq *= lacunarity;
        }
        return denom > 0f ? sum / denom : 0.5f;
    }

    // ===== 좌표 변환 =====

    Vector3 GetRoomLocalMin()
    {
        if (originAtCorner) return Vector3.zero;
        Vector3 sizeLocal = new Vector3(totalSize.x, totalSize.y, totalSize.z) * unitSize;
        return -0.5f * sizeLocal; // (0,0,0)이 센터면 최소 코너는 -size/2
    }

    Vector3 CellAbsToLocal(Vector3Int cellAbs)
    {
        Vector3 localMin = GetRoomLocalMin();
        return localMin + new Vector3(
            (cellAbs.x + 0.5f) * unitSize,
            (cellAbs.y + 0.5f) * unitSize,
            (cellAbs.z + 0.5f) * unitSize
        );
    }

    // ===== 컨테이너 =====

    void PrepareContainer()
    {
        if (container) return;
        var t = transform.Find("Obstacles");
        if (t) container = t;
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
