using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState { PreGame, Exploring , InCombat, DoorInteractable, Transitioning, Completed, Dead }

public class GameManager : MonoBehaviour
{
    public static GameManager I { get; private set; }

    [SerializeField] DungeonGen dungeon; // 던전 생성기
    public DungeonGen Dungeon => dungeon; // DoorController가 타깃 방 조회할 때 사용
    [SerializeField] Transform player;
    [SerializeField] GameObject clearPanel;
    [SerializeField] GameObject deadPanel;

    public GameState State { get; private set; }

    int currentIndex = 0;
    RoomController currentRoom;

    void Awake() { I = this; }

    void Start()
    {
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
        BeginRun();

    }

    public void BeginRun()
    {
        if (State != GameState.PreGame) return;
        State = GameState.Exploring; // 1방 전투x
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
            if (clearPanel) clearPanel.SetActive(true);
        }
    }

    public void OnPlayerDead()
    {
        State = GameState.Dead;
        if (deadPanel) deadPanel.SetActive(true);
    }


    public void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }



}
