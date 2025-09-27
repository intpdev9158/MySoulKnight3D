using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Damage & LifeTime")]
    public int damage = 1;
    public float lifeTime = 6f;

    [Header("Hit Ruleds")]
    public LayerMask destroyOnHitLayers;
    public string ignoreTag = "Enemy";

    void OnEnable()
    {
        Invoke(nameof(SelfDestruct), lifeTime);
    }

    void Onsable()
    {
        CancelInvoke(nameof(SelfDestruct));

    }

    void SelfDestruct()
    {
        Destroy(gameObject);
    }   


    void OnTriggerEnter(Collider other)
    {

        // 발사자(예: Enemy)와의 접촉은 무
        if (!string.IsNullOrEmpty(ignoreTag) && other.CompareTag(ignoreTag))
            return;

        // 플레이어에게 데미지 주고 삭제
        if (other.CompareTag("Player"))
        {
            if (other.TryGetComponent<PlayerHealth>(out var ph))
                ph.TakeDamage(1);

            Destroy(gameObject);
            return;
        }

        // 그 외: 지정 레이어면 삭제 (destroyLayers가 비어있으면 무시)
        if (destroyOnHitLayers.value != 0 && // 레이어에 하나라도 있을때
            ((1 << other.gameObject.layer) & destroyOnHitLayers.value) != 0)
            // << : 해당 레이어 번호 위치의 비트만 1로 세운 마스크를 만듬 ex) 1 << 6(other.layer) = 0000_0100_0000
            // & : AND 연산
        {
            Destroy(gameObject);
        }
    }

}
