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
    public float sprintMultiplier = 1.6f; // Ctrl 가속
    public float gravity = -9.81f;
    public float jumpHeight = 1.2f;

    [Header("Flight")]
    public bool flyMode = true;
    public float verticalSpeed = 5f;
    public KeyCode ascendKey = KeyCode.Space;
    public KeyCode descendKey = KeyCode.LeftShift;

    private CharacterController controller;
    private float pitch;
    private Vector3 velocity;
    float lastSpaceDownTime = -1f;
    const float DoubleTapWindow = 0.25f;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        if (!cam) cam = Camera.main ? Camera.main.transform : null;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        Look();
        HandleToggle();
        Move();
    }

    void Look()
    {
        float mx = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float my = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        transform.Rotate(Vector3.up * mx);

        pitch -= my;
        pitch = Mathf.Clamp(pitch, minPitch, maxpitch);
        cam.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    void HandleToggle()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (Time.time - lastSpaceDownTime <= DoubleTapWindow)
            {
                // 더블탭 : 공중 <-> 지상
                flyMode = !flyMode;
                velocity.y = 0f; // 전환시 수직속도 초기화
                lastSpaceDownTime = -1f;
            }
            else lastSpaceDownTime = Time.time;
        }
    }

    void Move()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        bool sprint = Input.GetKey(KeyCode.LeftControl);

        float speed = moveSpeed * (sprint ? sprintMultiplier : 1f);
        Vector3 input = (transform.right * h + transform.forward * v).normalized;
        Vector3 horizontal = input * speed;
        Vector3 motion = horizontal;

        if (flyMode)
        {
            float verticalInput = 0f;
            if (Input.GetKey(ascendKey)) verticalInput += 1f;
            if (Input.GetKey(descendKey)) verticalInput -= 1f;

            velocity.y = 0f; // 중력 무시
            motion += transform.up * verticalInput * verticalSpeed;
        }
        else
        {
            if (controller.isGrounded && velocity.y < 0f)
            {
                velocity.y = -2f;
            }

            if (controller.isGrounded && Input.GetKeyDown(KeyCode.Space))
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }

            velocity.y += gravity * Time.deltaTime;
            motion += new Vector3(0f, velocity.y, 0f);
        }

        controller.Move(motion * Time.deltaTime);
    }
}
