using UnityEngine;
using UnityEngine.Events;

public enum ItemType { Coin, Potion, Mana }

public class LootItem : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] ItemType type = ItemType.Coin;
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

    // ★ 추가: 드로퍼가 수량/타입 주입할 수 있게 public 세터 제공
    public void SetType(ItemType t)   => type = t;
    public void SetAmount(int a)      => amount = Mathf.Max(1, a);

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        // 임시방편
        if (type == ItemType.Potion)
        {
            // 플레이어에서 PlayerHealth 찾아서 하트 회복
            var hp = other.GetComponentInChildren<PlayerHealth>();
            if (hp) hp.Heal(amount);
            Destroy(gameObject);
            return;
        }

        InventoryCounter.Instance?.Add(type, amount);

        // switch (type)
        // {
        //     case ItemType.Coin:
        //         Debug.Log("get Coin");
        //         break;

        //     case ItemType.Mana:
        //         Debug.Log("get Mana");
        //         break;

        //     case ItemType.Potion:
        //         Debug.Log("get Potion");
        //         break;    

        // }

        Destroy(gameObject);
    }

}
