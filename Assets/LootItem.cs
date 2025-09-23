using UnityEngine;
using UnityEngine.Events;

public enum LootType { Coin, Potion, Mana }

public class LootItem : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] LootType type = LootType.Coin;
    [SerializeField] int amount = 1;

    [Header("Idle motion")]
    [SerializeField] float bobAmp = 0.1f; // 위아래 폭
    [SerializeField] float bobSpeed = 3f; // 위아래 속도
    [SerializeField] float rotSpeed = 90f; // 회전 속도

    // [Header("FX (optional)")]
    // [SerializeField] AudioClip pickupSfx;
    // [SerializeField] GameObject pickupVfx;

    float y0;


    void Start()
    {
        y0 = transform.position.y;
    }


    void Update()
    {
        // 살짝 떠다니고 회전
        var p = transform.position;
        p.y = y0 + Mathf.Sin(Time.time * bobSpeed) * bobAmp; // sin함수
        transform.position = p;
        transform.Rotate(Vector3.up, rotSpeed * Time.deltaTime, Space.World);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        switch (type)
        {
            case LootType.Coin:
                Debug.Log("get Coin");
                break;

            case LootType.Mana:
                Debug.Log("get Mana");
                break;

            case LootType.Potion:
                Debug.Log("get Potion");
                break;    

        }

        Destroy(gameObject);
    }
}
