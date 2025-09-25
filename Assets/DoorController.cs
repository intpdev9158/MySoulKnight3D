using System.Security.Cryptography.X509Certificates;
using Unity.VisualScripting;
using UnityEditor.Build.Content;
using UnityEngine;
using UnityEngine.InputSystem;

public enum Doordir { XPlus, XMinus, YPlus, YMinus, ZPlus, ZMinus }

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
    public DoorPassageTrigger passageTrigger; // 통로 트리거 (열면 활성화)

    [Header("Interact (Aim+Dis)")]
    [SerializeField] float interactRange = 3.0f; // 최대 거리
    [SerializeField] LayerMask interactMask = ~0; // 문이 속한 레이어(권장: Door)
    [SerializeField] bool showPromptOnlyWhenAimed = true;

    enum DoorState { Locked, Closed, Opened }
    DoorState state = DoorState.Locked;

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
        state = locked ? DoorState.Locked : DoorState.Closed;

        if (lockedIndicator) lockedIndicator.SetActive(locked);
        if (interactIndicator) interactIndicator.SetActive(!locked);

        if (!locked && state == DoorState.Closed)
        {
            if (doorVisual) doorVisual.SetActive(true);
            if (doorBlocker) doorBlocker.enabled = true;
            if (passageTrigger) passageTrigger.gameObject.SetActive(false);
        }
    }



    public void Open()
    {
        if (state != DoorState.Closed) return;
        state = DoorState.Opened;

        // visual/blocker 해제 
        if (doorVisual) doorVisual.SetActive(false);
        if (doorBlocker) doorBlocker.enabled = false;

        // 상호작용 표시 x , 통로 트리거 활성화
        if (interactIndicator) interactIndicator.SetActive(false);

        if (passageTrigger)
        {
            passageTrigger.targetRoomIndex = targetRoomIndex;
            passageTrigger.gameObject.SetActive(true);
        }

    }

    void UpdateIndicators()
    {
        if (lockedIndicator) lockedIndicator.SetActive(state == DoorState.Locked);
        if (interactIndicator) interactIndicator.SetActive(state == DoorState.Closed);
    }
}
