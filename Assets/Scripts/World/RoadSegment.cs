using UnityEngine;

public class RoadSegment
{
    public Vector3 startPos;
    public Vector3 startForward;
    public Vector3 startUp;

    public Vector3 endPos;
    public Vector3 endForward;
    public Vector3 endUp;

    public float length;

    public void Generate(float curvature, float banking)
    {
        Quaternion turn =
            Quaternion.AngleAxis(
                Mathf.Lerp(-25f, 25f, curvature),
                startUp
            );

        endForward = turn * startForward;

        Quaternion bankRot =
            Quaternion.AngleAxis(
                Mathf.Lerp(-12f, 12f, banking),
                endForward
            );

        endUp = bankRot * startUp;

        endPos = startPos + endForward * length;
    }
}
