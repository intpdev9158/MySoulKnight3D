using UnityEngine;

public class HPBarBillboard : MonoBehaviour
{

    Transform cam;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (Camera.main) cam = Camera.main.transform;
    }

    // Update is called once per frame
    void Update()
    {
        if (!cam) return;

        transform.rotation = Quaternion.LookRotation(transform.position - cam.position, Vector3.up);

        // 만약 Y축만 회전하고 싶으면 위 한 줄 대신 아래 두 줄 사용:
        // Vector3 fwd = cam.forward; fwd.y = 0f; fwd.Normalize();
        // transform.rotation = Quaternion.LookRotation(fwd, Vector3.up);
    }
}
