using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    
    public Transform player;
    public float speed = 3f;
    public float stopDistance = 1.5f;
    [Range(0.0f, 0.4f)] public float turnLerp = 0.18f; // 회전 부드러움


    Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    void FixedUpdate()
    {
        if (!player) return;

        Vector3 toPlayer = player.position - transform.position;
        Vector3 flatDir = new Vector3(toPlayer.x, 0f, toPlayer.z);
        float dist = flatDir.magnitude;

        // 플레이어 바라보기
        if (flatDir.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(flatDir.normalized, Vector3.up);
            Quaternion newRot = Quaternion.Slerp(rb.rotation, targetRot, turnLerp);
            rb.MoveRotation(newRot);
        }

        // 일정 거리까지만 이동
        if (dist > stopDistance)
        {
            Vector3 step = flatDir.normalized * speed * Time.fixedDeltaTime;
            rb.MovePosition(rb.position + step);
        }
    }
}
