using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UIResourceHUD : MonoBehaviour
{
    [Header("Coin UI")]
    public TMP_Text coinText;

    [Header("Armor UI")]
    public TMP_Text armorText;
    public Image armorFill;
    public int armorMax = 100;

    [Header("Armor UI")]
    public TMP_Text manaText;
    public Image manaFill;
    public int manaMax = 100;

    void OnEnable()
    {
        if (InventoryCounter.Instance)
            // HandleChanged 메서드를 이 이벤트에 추가로 등록 하라는 뜻
            // 즉 OnCountChanged 함수가 발생 하면 HandleChanged 함수도 실행해달라는 뜻!
            InventoryCounter.Instance.OnCountChanged += HandleChanged;
            Debug.Log("OnEnable");

        RefreshAll();
    }

    void OnDisable()
    {
        if (InventoryCounter.Instance)
            InventoryCounter.Instance.OnCountChanged -= HandleChanged;
    }

    void HandleChanged(ItemType type, int newCount)
    {
        Debug.Log("HandleChanged");
        switch (type)
        {
            case ItemType.Coin:
                if (coinText) coinText.text = newCount.ToString();
                break;

            case ItemType.Mana:
                if (manaText) manaText.text = $"{newCount} / {manaMax}";
                if (manaFill) manaFill.fillAmount = Mathf.Clamp01(newCount / (float)manaMax);
                break;
        }
    }

    void RefreshAll()
    {
        if (!InventoryCounter.Instance) return;

        HandleChanged(ItemType.Coin, InventoryCounter.Instance.GetCount(ItemType.Coin));
        //HandleChanged(ItemType.Potion, InventoryCounter.Instance.GetCount(ItemType.Potion));
        HandleChanged(ItemType.Mana, InventoryCounter.Instance.GetCount(ItemType.Mana));
    }
}
