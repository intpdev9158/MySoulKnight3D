using UnityEngine;


[RequireComponent(typeof(CharacterController))]

public class FPSController : MonoBehaviour
{

    [Header("Look")]
    public Transform cam;
    public float mouseSensitivity = 200f;
    public float minPitch = -80f;
    public float maxpitch = 80f;


    [Header("Move")]
    public float moveSpeed = 5f;
    public float gravity = -9.81f;
    public float jumpHeight = 1.2f;

    private CharacterController controller;
    private float pitch;
    private Vector3 velocity;



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        controller = GetComponent<CharacterController>();
        if (cam == null) cam = Camera.main.transform;

        // 마우스 커서 잠금
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Update is called once per frame
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

        // 좌우는 플레이어
        transform.Rotate(Vector3.up * mx);

        // 상하는 카메라(Pitch) , 각도제한 
        pitch -= my;
        pitch = Mathf.Clamp(pitch, minPitch, maxpitch);
        cam.localRotation = Quaternion.Euler(pitch, 0f, 0f);

    }

    void Move()
    {
        // WASD 평면 입력
        float h = Input.GetAxisRaw("Horizontal"); // A D
        float v = Input.GetAxisRaw("Vertical"); // W S

        Vector3 input = (transform.right * h + transform.forward * v).normalized;

        // 지면 이동
        Vector3 horizontal = input * moveSpeed;

        // 바닥 체크
        if (controller.isGrounded && velocity.y < 0f)
        {
            velocity.y = -2f; // 약간의 하중
        }


        // 점프
        if (controller.isGrounded && Input.GetKeyDown(KeyCode.Space))
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // 중력 적용
        velocity.y += gravity * Time.deltaTime;

        // 최종 이동
        Vector3 motion = horizontal + new Vector3(0f, velocity.y, 0f);
        controller.Move(motion * Time.deltaTime);
    }
}
