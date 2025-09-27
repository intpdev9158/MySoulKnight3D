using UnityEngine;

[RequireComponent(typeof(Collider))]
public class RoomEnterTrigger : MonoBehaviour
{
    public int roomIndex = -1;

    void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (roomIndex < 0) return;
        GameManager.I.OnRoomEntered(roomIndex);
    }
    
}
