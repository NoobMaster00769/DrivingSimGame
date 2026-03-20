using UnityEngine;

public class InfiniteWaterVolume : MonoBehaviour
{
    public Transform player;
    public float waterHeight = -3f;

    void LateUpdate()
    {
        if (!player) return;

        Vector3 target = player.position;
        target.y = waterHeight;

        transform.position = target;
        transform.rotation = Quaternion.identity;
    }
}