using UnityEngine;

public class HorizonDrift : MonoBehaviour
{
    public float speed = 0.3f;
    public float resetZ = -5f;
    public float startZ = 15f;

    void Update()
    {
        transform.Translate(Vector3.back * speed * Time.deltaTime, Space.World);

        if (transform.position.z <= resetZ)
        {
            transform.position = new Vector3(
                transform.position.x,
                transform.position.y,
                startZ
            );
        }
    }
}
