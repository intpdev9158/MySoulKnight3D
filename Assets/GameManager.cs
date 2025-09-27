using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState { PreGame, Exploring , InCombat, DoorInteractable, Transitioning, Completed, Dead }

public class GameManager : MonoBehaviour
{
    public static GameManager I { get; private set; }

    // 플레이어 조작 토글 대상들
    [SerializeField] Behaviour[] controlBehaviours;

    // UI
    [SerializeField] UIFlowHUD ui;

    // DunGeon
    [SerializeField] DungeonGen dungeon; // 던전 생성기
    public DungeonGen Dungeon => dungeon; // DoorController가 타깃 방 조회할 때 사용
    [SerializeField] Transform player;

    public GameState State { get; private set; }

    int currentIndex = 0;
    RoomController currentRoom;

    void Awake() { I = this; }

    void Start()
    {

        if (ui) { ui.ShowStart(true); ui.ShowClear(false); ui.ShowExit(false); }
        State = GameState.PreGame;
        
        // 시작 대기: 전체 일시정지
        SetTimePaused(true);

        // 던전 생성
        dungeon.Generate();

        currentIndex = 0;
        currentRoom = dungeon.GetRoomController(currentIndex);

        var spawn = dungeon.GetPlayerSpawn(currentIndex);
        if (spawn) player.position = spawn.position;

        // 1방
        if (currentRoom != null)
        {
            currentRoom.OnAllEnemiesDead -= OnRoomCleared;
            currentRoom.OnAllEnemiesDead += OnRoomCleared;
            currentRoom.UnlockLobby();
        }

        State = GameState.PreGame; // Start 버튼 대기 (필요 없으면 BeginRun() 바로 호출)

    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (State == GameState.PreGame)
            {
                BeginRun_ByUser();
            }
            else if (State == GameState.Completed)
            {
                ExitGame_ByUser();
            }
        }
    }

    public void BeginRun_ByUser()
    {
        if (State != GameState.PreGame) return;
        State = GameState.Exploring; // 1방 전투x

        if (ui) ui.ShowStart(false);
        SetTimePaused(false); // 게임 재개
    }

    public void ExitGame_ByUser()
    {
        // 혹시 몰라서 원복 후 종료 (선택)
        Time.timeScale = 1f;

        // 에디터에서는 재생 모드만 종료
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }

    public void OnRoomEntered(int enteredIndex)
    {
        // 이동 방향 상관없이, 방 안으로 들어온 순간만 처리
        if (enteredIndex == currentIndex) return;

        currentIndex = enteredIndex;
        currentRoom = dungeon.GetRoomController(currentIndex);
        if (currentRoom == null) return;

        // 로비
        if (currentIndex == 0)
        {
            State = GameState.Exploring;
            return;
        }

        // 이미 클리어 된 방
        if (currentRoom.IsCleared)
        {
            State = GameState.DoorInteractable;
            return;
        }

        if (!currentRoom.HasActivated)
        {
            State = GameState.InCombat;
            currentRoom.OnAllEnemiesDead -= OnRoomCleared;
            currentRoom.OnAllEnemiesDead += OnRoomCleared;
            currentRoom.Activate(player); // 내부에서 문 잠그고 스폰
        }
        else
        {
            // 전투 진행 중이던 방 재입장
            State = currentRoom.RemainingEnemies > 0 ? GameState.InCombat : GameState.DoorInteractable;
        }
    }

    void OnRoomCleared()
    {
        State = GameState.DoorInteractable; // 문 상호작용 가능

        if (dungeon.IsLastRoom(currentIndex))
        {
            State = GameState.Completed;
            if (ui) { ui.ShowClear(false); ui.ShowExit(true); }

            SetTimePaused(true); // 게임 재개

        }
        else
        {
            if (ui) { ui.ShowClear(true); ui.HideClearAfter(); }

            SetTimePaused(false); // 게임 재개
        }
    }

    System.Collections.IEnumerator HidePanelAfter(GameObject panel, float t)
    {
        yield return new WaitForSeconds(t);
        if (panel) panel.SetActive(false);
    }


    public void OnPlayerDead()
    {
        State = GameState.Dead;

    }

    public void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // ==== 시간/커서 일괄 제어 ====
    void SetTimePaused(bool paused)
    {
        Time.timeScale = paused ? 0f : 1f;

        // 마우스 룩/입력에 대한 UX
        Cursor.lockState = paused ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible   = paused;

        // Rigidbody 없으니 속도 보정은 불필요. 필요하면 여기서 개별 처리 가능.
    }

}
