using UnityEngine;
using System.Collections.Generic;

public class TerrainSpawner : MonoBehaviour
{
    [Header("References")]
    public Transform car;
    public Terrain terrainPrefab;

    [Header("Grid")]
    public int tilesAhead = 3;
    public int tilesSide = 1;
    public float tileSize = 500f;

    Dictionary<Vector2Int, Terrain> spawned = new();
    Vector2Int currentTile;

    void Update()
    {
        if (!car) return;

        currentTile = new Vector2Int(
            Mathf.FloorToInt(car.position.x / tileSize),
            Mathf.FloorToInt(car.position.z / tileSize)
        );

        UpdateTiles();
    }

    void UpdateTiles()
    {
        for (int z = 0; z <= tilesAhead; z++)
        {
            for (int x = -tilesSide; x <= tilesSide; x++)
            {
                Vector2Int id = new Vector2Int(
                    currentTile.x + x,
                    currentTile.y + z
                );

                if (!spawned.ContainsKey(id))
                    SpawnTile(id);
            }
        }

        Cleanup();
    }

    void SpawnTile(Vector2Int id)
    {
        Vector3 pos = new Vector3(
            id.x * tileSize,
            0f,
            id.y * tileSize
        );

        Terrain t = Instantiate(
            terrainPrefab,
            pos,
            Quaternion.identity,
            transform
        );

        // IMPORTANT: unique TerrainData per tile
        TerrainData dataCopy = Instantiate(t.terrainData);
        t.terrainData = dataCopy;

        // Height generation happens INSIDE TerrainHeightSimple
        // using absolute world coordinates (no offsets)

        spawned.Add(id, t);
    }

    void Cleanup()
    {
        List<Vector2Int> remove = new();

        foreach (var kv in spawned)
        {
            Vector2Int id = kv.Key;

            if (Mathf.Abs(id.y - currentTile.y) > tilesAhead + 1)
                remove.Add(id);
        }

        foreach (var id in remove)
        {
            Destroy(spawned[id].gameObject);
            spawned.Remove(id);
        }
    }
}
