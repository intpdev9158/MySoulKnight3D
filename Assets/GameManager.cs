using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState { PreGame, Exploring , InCombat, DoorInteractable, Transitioning, Completed, Dead }

public class GameManager : MonoBehaviour
{
    public static GameManager I { get; private set; }

    [SerializeField] DungeonGen dungeon; // 던전 생성기
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
            currentRoom.LockDoors(false); //open
        }

        State = GameState.PreGame; // Start 버튼 대기 (필요 없으면 BeginRun() 바로 호출)
        BeginRun();

    }

    public void BeginRun()
    {
        if (State != GameState.PreGame) return;
        State = GameState.Exploring; // 1방 전투x
    }

    // 플레이어가 트리거 지나간 순간 호출
    public void OnDoorwayPassed(int nextIdx)
    {
        if (nextIdx < 0) return;

        // 1방 or 전투방 클리어후 지나갈 수 있음
        bool canPass =
            (State == GameState.Exploring && currentIndex == 0) ||
            (State == GameState.DoorInteractable);

        if (!canPass) return;

        State = GameState.Transitioning;
        EnterRoom(nextIdx); // 다음방 바로 시작
    }

    void EnterRoom(int idx)
    {
        currentIndex = idx;
        currentRoom = dungeon.GetRoomController(idx);
        if (currentRoom == null) return;

        if (idx == 0)
        {
            currentRoom.LockDoors(false);
            State = GameState.Exploring;
            return;
        }

        State = GameState.InCombat;

        currentRoom.OnAllEnemiesDead -= OnRoomCleared;
        currentRoom.OnAllEnemiesDead += OnRoomCleared;

        currentRoom.LockDoors(true);
        currentRoom.Activate(player);


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
