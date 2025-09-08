using UnityEngine;

public class bullet : MonoBehaviour
{

    public int damage = 2;

    public float speed = 20f;
    public float lifeTime = 2f;

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }


    void Update()
    {
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            var eh = other.GetComponent<EnemyHealth>();
            if (eh) eh.TakeDamage(damage);
            //Destroy(other.gameObject); // 적 삭제
            Destroy(gameObject); // 총알 삭제
        }
    }
}
