using UnityEngine;

public class RoadSegment : MonoBehaviour
{
    public Transform Start;
    public Transform End;

    void OnDrawGizmos()
    {
        if (!Start || !End) return;

        Gizmos.color = Color.green;
        Gizmos.DrawSphere(Start.position, 0.4f);
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(End.position, 0.4f);
        Gizmos.DrawLine(Start.position, End.position);
    }
}
