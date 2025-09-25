using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DoorPassageTrigger : MonoBehaviour
{
    public int targetRoomIndex = -1;

    void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
        gameObject.SetActive(false);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (targetRoomIndex < 0) return;
        GameManager.I.OnDoorwayPassed(targetRoomIndex); // 걸어서 점어간 시점에 방전환
    }
}
