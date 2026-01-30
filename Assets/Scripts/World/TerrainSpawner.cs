using UnityEngine;
using System.Collections.Generic;

public class TerrainSpawner : MonoBehaviour
{
    public TerrainChunk terrainPrefab;

    public int tilesPerSide = 2;
    public float tileSize = 20f;

    List<TerrainChunk> spawned = new();

    void Start()
    {
        Spawn();
    }

    void Spawn()
    {
        for (int x = -tilesPerSide; x <= tilesPerSide; x++)
        {
            if (x == 0) continue; // road occupies center

            Vector3 pos =
                transform.position +
                transform.right * x * tileSize;

            TerrainChunk t =
                Instantiate(terrainPrefab, pos, Quaternion.identity, transform);

            spawned.Add(t);
        }
    }
}
