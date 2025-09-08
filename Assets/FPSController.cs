using UnityEngine;

// 자동으로 CharacterController 컴포넌트도 필요하다는뜻
[RequireComponent(typeof(CharacterController))]

// 왜 Rigidbody 말고 CharacterController?
// FPS 이동은 “캐릭터 충돌 + 경사/계단 넘기기”가 중요하고 물리 시뮬이 과한 게 독이 될 때가 많아. 
// CC는 입력대로 깔끔하게 움직임.
public class FPSController : MonoBehaviour
{

    [Header("Look")] // 인스펙터창에서 그룹 제목 표시!
    public Transform cam;
    public float mouseSensitivity = 200f;
    // 카메라 각도 제한
    public float minPitch = -80f;
    public float maxpitch = 80f;


    [Header("Move")]
    public float moveSpeed = 5f;
    public float gravity = -9.81f;
    public float jumpHeight = 1.2f;

    private CharacterController controller; // 캐릭터 충돌 / 이동 담당 컴포넌트
    private float pitch; // 카메라 상하 회전 누적 각도
    private Vector3 velocity; // 현재 속도 벡터 (주로 Y축 중력/점프 저장)



    void Start()
    {
        controller = GetComponent<CharacterController>();
        if (cam == null) cam = Camera.main.transform; // cam이 지정되지 않았으면 메인카메라로 자동 지정

        // 마우스 커서 잠금
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }


    void Update()
    {
        Look();
        Move();

        //
    }

    void Look()
    {
        // 마우스 입력 
        float mx = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float my = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // 좌우(Yaw) , 플레이어 본체를 Y축으로 회전
        transform.Rotate(Vector3.up * mx);

        // 상하(Pitch)는 카메라 각도 누적 (마우스 위로 = 음수로 뒤집히지 않게) 
        pitch -= my;
        pitch = Mathf.Clamp(pitch, minPitch, maxpitch); // 시야 각도 제한 (-80 ~ 80)
        cam.localRotation = Quaternion.Euler(pitch, 0f, 0f); // 카메라 로컬 회전만 바꿔서 물체 기울어짐 방지

    }

    void Move()
    {
        // WASD 평면 입력 GetAxisRaw는 -1/0/1로 튐 → 키 떼면 '딱' 멈춤
        float h = Input.GetAxisRaw("Horizontal"); // A D
        float v = Input.GetAxisRaw("Vertical"); // W S

        // 플레이어의 x/y축 기준으로 이동 입력을 월드 벡터로 변환
        // right = x축, forward = z축
        Vector3 input = (transform.right * h + transform.forward * v).normalized;
        // 대각선 과속 방지 위해 normalized. 크기1 * 속도 = 초당 m
        Vector3 horizontal = input * moveSpeed;

        // 바닥에 붙여주기 : isGrounded가 참이고 하강중이면 살짝 음수로 눌러서 통통 튐 방지
        if (controller.isGrounded && velocity.y < 0f)
        {
            velocity.y = -2f; // 약간의 하중
        }


        // 점프
        if (controller.isGrounded && Input.GetKeyDown(KeyCode.Space))
        {
            // 점프 초기속도 공식: v = √(2gh) → 중력은 음수라 -2f*gravity
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // 중력 지속 적용: a * t 를 속도에 누적
        velocity.y += gravity * Time.deltaTime;

        // 최종 이동 벡터 = 수평 이동 + 수직 속도
        Vector3 motion = horizontal + new Vector3(0f, velocity.y, 0f);
        // CharacterController로 이동(충돌/경사/계단 처리를 CC가 해줌)
        controller.Move(motion * Time.deltaTime);
    }
}
