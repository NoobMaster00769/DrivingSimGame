using UnityEngine;

[CreateAssetMenu(menuName = "LevelChunkData")]
public class LevelChunkData : ScriptableObject
{
    public Vector2 chunkSize = new Vector2(10f, 40f);

    public GameObject[] levelChunks;
}