using Unity.VisualScripting;
using UnityEngine;

public class PlayerShoot : MonoBehaviour
{

    public GameObject bulletPrefab;
    public Transform firePoint;
    public Camera cam;


    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Shoot();
        }

    }

    void Shoot()
    {
        if (!bulletPrefab || !firePoint || !cam) return;

        // 카메라가 바라보는 방향으로 발사
        Vector3 dir = cam.transform.forward;

        // 총구 위치에서, 카메라 방향을 바라보도록 회전해서 생성
        Quaternion rot = Quaternion.LookRotation(dir, Vector3.up);
        Instantiate(bulletPrefab, firePoint.position, rot);

        
    }
}
