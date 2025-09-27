using System;
using System.Collections;
using UnityEngine;

public class EnemyRangedShooter : MonoBehaviour
{

    private EnemyAI enemyAI;
    private Transform player;

    public Transform firePoint;
    public GameObject projectilePrefab;
    public float projectileSpeed = 14f;
    public float fireCooldown = 2.5f;

    public GameObject chargeSpherePrefab;
    public float chargeTime = 1.0f;
    public Vector3 chargeStartScale = new Vector3(0.25f, 0.25f, 0.25f);
    public Vector3 chargeEndScale = new Vector3(1.1f, 1.1f, 1.1f);

    private float _cd;


    void Awake()
    {
        enemyAI = GetComponent<EnemyAI>();
        if (enemyAI != null) player = enemyAI.player;
    }


    void Update()
    {
        if (enemyAI != null && player == null)
        {
            player = enemyAI.player;
        }

        _cd -= Time.deltaTime;
        if (_cd < 0f && player)
        {
            _cd = fireCooldown;
            // 코루틴은 함수를 호출한다.
            // 함수는 한 프레임에 호출되어 완료가 된다. 
            // 이에 IEnumerator 형식을 반환 값으로 가지는 함수를 사용한다.
            StartCoroutine(FireRoutine());
        }
    }

    private GameObject currentCharge;

    // IEnumerator는 함수 내부에 실행을 중지하고, 
    // 다음 프레임에서 실행을 재개할 수 있는 yield return 구문을 사용한다.  
    IEnumerator FireRoutine()
    {
        currentCharge = null;

        if (chargeSpherePrefab)
        {
            currentCharge = Instantiate(chargeSpherePrefab, firePoint.position, firePoint.rotation);
            currentCharge.transform.localScale = chargeStartScale;
            float t = 0f;
            while (t < chargeTime)
            {
                t += Time.deltaTime;
                // Clamp(n , min , max) : n이 최솟값 최댓값 사이에 있는지 확인하고 벗어나면 최솟값 최댓값을 반환
                // Clamp01 : 0~1 사이 값을 반환!
                float k = Mathf.Clamp01(t / chargeTime);
                currentCharge.transform.position = firePoint.position;
                // 선형 보간(Lerp) : 어떤 수치 -> 어떤 수치 로 부드럽게 변경 되게
                currentCharge.transform.localScale = Vector3.Lerp(chargeStartScale, chargeEndScale, k);
                // yield return null : 다음 프레임에 실행을 재개한다.
                // yield return new WaitForSeconds : 지정된 시간 후에 재개한다.
                yield return null;
            }
        }

        Vector3 targetPos = player.position;
        Vector3 dir = (targetPos - firePoint.position).normalized;

        GameObject proj = Instantiate(projectilePrefab, firePoint.position, Quaternion.LookRotation(dir, Vector3.up));
        Rigidbody rb = proj.GetComponent<Rigidbody>();
        if (rb) rb.linearVelocity = dir * projectileSpeed;

        if (currentCharge) Destroy(currentCharge);
    }

    // ← 몬스터가 죽을 때 EnemyHealth에서 호출해줄 메서드
    public void OnDeath()
    {
        if (currentCharge) Destroy(currentCharge);  // 남아있는 차징 구체 제거
        currentCharge = null;
        StopAllCoroutines();                        // 돌고 있는 FireRoutine 중단
    }
}
