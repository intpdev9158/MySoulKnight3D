using UnityEngine;

[System.Serializable]
public class LootEntry
{
    public LootType type = LootType.Coin;
    public int min = 1, max = 1; // 드랍갯수 최소/최대 값
    [Range(0, 1f)] public float dropChance = 1f;
    public float weight = 1f;
}

public class LootDropper : MonoBehaviour
{

    [SerializeField] LootEntry[] table;
    [SerializeField] GameObject coinPrefab;
    [SerializeField] GameObject manaPrefab;
    [SerializeField] GameObject potionPrefab;

    public void Drop()
    {
        if (table == null || table.Length == 0) return;

        // 유효 항목 가중치 합
        float sum = 0f;
        foreach (var e in table) sum += e.dropChance * e.weight;
        if (sum <= 0f) return;

        // 선택
        float r = Random.value * sum, acc = 0f;
        LootEntry pick = null;
        foreach (var e in table)
        {
            acc += e.dropChance * e.weight;
            if (r <= acc) { pick = e; break; }
        }
        if (pick == null) return;

        int amt = Random.Range(pick.min, pick.max + 1);
        var prefab = PrefabOf(pick.type);
        if (!prefab) return;

        // 적 위치 근처
        var pos = transform.position + Vector3.up * 0.3f + Random.insideUnitSphere * 0.2f;
        var go = Instantiate(prefab, pos, Quaternion.identity);

        // 수량
        // var li = go.GetComponent<LootItem>();
        // if (li) { li.SetType(pick.type);  li.SetAmount(amt); }
    }

    GameObject PrefabOf(LootType t)
    {
        return t switch
        {
            LootType.Coin => coinPrefab,
            LootType.Mana => manaPrefab,
            LootType.Potion => potionPrefab,
            _ => null
        };
    }



}
