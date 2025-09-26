using System.Security.Cryptography.X509Certificates;
using Unity.VisualScripting;
using UnityEditor.Build.Content;
using UnityEngine;
using UnityEngine.InputSystem;

public enum Doordir { XPlus, XMinus, YPlus, YMinus, ZPlus, ZMinus }

public static class DoorDirUtil
{
    public static Doordir Opp(Doordir d) => d switch
    {
        Doordir.XPlus => Doordir.XMinus,
        Doordir.XMinus => Doordir.XPlus,
        Doordir.YPlus => Doordir.YMinus,
        Doordir.YMinus => Doordir.YPlus,
        Doordir.ZPlus => Doordir.ZMinus,
        Doordir.ZMinus => Doordir.ZPlus,
        _ => Doordir.XPlus
    };
}

public class DoorController : MonoBehaviour
{
    public Doordir Direction;
    public RoomController OwnerRoom { get; set; }

    [Header("Runtime Linking")]
    public int sourceRoomIndex = -1; // 이 문이 속한 방 인덱스
    public int targetRoomIndex = -1; // 이 문이 연결하는 다음 방 인덱스

    [Header("Refs")]
    public GameObject doorVisual;           // 문 메시(열면 숨김)
    public Collider doorBlocker;            // 플레이어 막는 콜라이더(열면 비활성)
    public GameObject lockedIndicator;      // "적을 처치하세요"
    public GameObject interactIndicator;    // "E : 문열기"

    [Header("Interact (Aim+Dis)")]
    [SerializeField] float interactRange = 3.0f; // 최대 거리
    [SerializeField] LayerMask interactMask = ~0; // 문이 속한 레이어(권장: Door)
    [SerializeField] bool showPromptOnlyWhenAimed = true;

    enum DoorState { Locked, Closed, Opened }
    DoorState state = DoorState.Locked;

    // 한 번 열린 문은 다시 닫지 않음
    bool permanentlyOpen = false;

    void Start()
    {
        UpdateIndicators();
    }

    void Update()
    {
        if (state != DoorState.Closed) return; // Locked/Opened면 입력 무시

        // 에임이 문을 가리키고 있고, 거리 안이면 E활성
        bool aimedAtThisDoor = false;

        Camera cam = Camera.main;
        if (cam)
        {
            Ray ray = new Ray(cam.transform.position, cam.transform.forward);
            if (Physics.Raycast(ray, out var hit, interactRange, interactMask, QueryTriggerInteraction.Ignore))
            {
                // 이 문(혹은 자식/부모)에 맞았는지 확인
                var dc = hit.collider.GetComponentInParent<DoorController>();
                if (dc == this) aimedAtThisDoor = true;
            }
        }

        if (interactIndicator)
        {
            interactIndicator.SetActive(state == DoorState.Closed && (!showPromptOnlyWhenAimed || aimedAtThisDoor));
        }

        // E키로 열기
        if (aimedAtThisDoor)
        {
            bool ePressed =
#if ENABLE_INPUT_SYSTEM
                Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
#else
            Input.GetKeyDown(KeyCode.E);
#endif
            if (ePressed) Open();
        }

    }

    public void SetLocked(bool locked)
    {   
        // 영구 개방이면 잠금/해제 호출 무시 "열림"유지
        if (permanentlyOpen)
        {
            ForceOpenVisual();
            return;
        }
        
        state = locked ? DoorState.Locked : DoorState.Closed;

        if (lockedIndicator) lockedIndicator.SetActive(locked);
        if (interactIndicator) interactIndicator.SetActive(!locked);

        if (!locked)
        {
            if (doorVisual) doorVisual.SetActive(true);
            if (doorBlocker) doorBlocker.enabled = true;
        }
    }



    public void Open()
    {
        if (state != DoorState.Closed) return;

        // 내 쪽 먼저 열기
        ForceOpenVisual();

        // 반대편(타깃방)의 대응 문도 강제로 열기
        if (targetRoomIndex >= 0 && GameManager.I?.Dungeon)
        {
            var targetRoom = GameManager.I.Dungeon.GetRoomController(targetRoomIndex);
            var opposite = targetRoom.GetDoor(DoorDirUtil.Opp(Direction));
            opposite?.ForceOpenVisual();
        }

    }

    // 다음방 상태와 무관하게 오픈
    public void ForceOpenVisual()
    {
        permanentlyOpen = true;
        state = DoorState.Opened;
        if (doorVisual) doorVisual.SetActive(false);
        if (doorBlocker) doorBlocker.enabled = false;
        if (interactIndicator) interactIndicator.SetActive(false);
        if (lockedIndicator) lockedIndicator.SetActive(false);
    }

    void UpdateIndicators()
    {
        if (lockedIndicator) lockedIndicator.SetActive(state == DoorState.Locked);
        if (interactIndicator) interactIndicator.SetActive(state == DoorState.Closed);
    }
}
