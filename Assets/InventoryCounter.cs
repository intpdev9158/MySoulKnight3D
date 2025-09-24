using UnityEngine;
using System;
using System.Collections.Generic;


public class InventoryCounter : MonoBehaviour
{
    public static InventoryCounter Instance { get; private set; }

    // 타입별 개수 카운트
    readonly Dictionary<ItemType, int> _counts = new();

    // Action : 델리게이트 = 매서드를 매개 변수로 전달가능
    // event : 다른 클래스는 += / -= 만 가능 , 호출(Invoke)은 선언한 클래스 내부에서만 가능하게 하는 한정자
    // 구독된 콜백들을 모두 호출해서 알려주는 용도
    // 이게 있어서 다른 시스템이 매 프레임 폴링, UI함수 호출 x
    public event Action<ItemType, int> OnCountChanged;


    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        foreach (ItemType t in Enum.GetValues(typeof(ItemType)))
            _counts[t] = 0;

        //Debug.Log("Instance");
    }

    // Dicionary에 key가 있다면 true 리턴 , 그 값을 v에 담아줌
    public int GetCount(ItemType type) => _counts.TryGetValue(type, out var v) ? v : 0;

    public void Add(ItemType type, int amount)
    {
        Debug.Log($"Add : {amount}");
        if (amount <= 0) return;
        _counts[type] = Mathf.Clamp(GetCount(type) + amount, 0, int.MaxValue);
        OnCountChanged?.Invoke(type, _counts[type]);
    }

    public bool Use(ItemType type, int amount)
    {
        int cur = GetCount(type);
        if (cur < amount) return false;
        _counts[type] = cur - amount;
        OnCountChanged?.Invoke(type, _counts[type]);
        return true;

    }
}
