using UnityEngine;

public class WorldDrift : MonoBehaviour
{
    public Transform[] groundTiles;
    [HideInInspector] public float speed;

    public float tileLength = 20f;

    void Update()
    {
        foreach (Transform tile in groundTiles)
        {
            tile.Translate(Vector3.back * speed * Time.deltaTime, Space.World);

            if (tile.position.z <= -tileLength)
            {
                tile.position += Vector3.forward * tileLength * groundTiles.Length;
            }
        }
    }
}
