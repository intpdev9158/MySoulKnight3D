using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    [Header("Hearts")]
    public int maxHearts = 3;
    public int currentHearts = 3;

    [Tooltip("좌우순서")]
    public Image[] heartImages; // 하트 이미지 배열
    public Sprite fullHeart;
    public Sprite emptyHeart;

    [Header("Hit Cooldown")]
    public float invincibleTime = 0.6f; // 연속 피격방지 초
    private float _hurtTimer;


    void Awake()
    {
        currentHearts = Mathf.Clamp(currentHearts, 0, maxHearts);
        RefreshHearts();
    }

    void Update()
    {
        if (_hurtTimer > 0f) _hurtTimer -= Time.deltaTime;

        // Update() 안에 임시
        if (Input.GetKeyDown(KeyCode.K)) TakeDamage(1);
        //if (Input.GetKeyDown(KeyCode.H)) Heal(1);

    }

    public void TakeDamage(int hearts = 1)
    {
        if (_hurtTimer > 0f) return;

        currentHearts = Mathf.Max(0, currentHearts - Mathf.Abs(hearts));
        _hurtTimer = invincibleTime;

        RefreshHearts();

        if (currentHearts <= 0)
        {
            Die();
        }

    }

    public void Heal(int hearts = 1)
    {
        currentHearts = Mathf.Min(maxHearts, currentHearts + Mathf.Abs(hearts));
        RefreshHearts();
    }

    // public void SetMaxHearts(int newMax, bool refill = true)
    // {
    //     maxHearts = Mathf.Max(1, newMax);
    //     if (refill) currentHearts = maxHearts;
    //     else currentHearts = Mathf.Min(currentHearts, maxHearts);
    //     RefreshHearts();
    // }

    void RefreshHearts()
    {
        if (heartImages == null) return;

        for (int i = 0; i < heartImages.Length; i++)
        {
            bool activeSlot = i < maxHearts; // 최대 하트 개수까지만 표시
            if (heartImages[i]) heartImages[i].enabled = activeSlot;

            if (!activeSlot) continue;

            bool filled = i < currentHearts; // 현재 체력 이하까지만 꽉 찬 하트
            if (heartImages[i])
                heartImages[i].sprite = filled ? fullHeart : emptyHeart;

        }
    }

    void Die()
    {
        Debug.Log("Player Dead");
    }

}
