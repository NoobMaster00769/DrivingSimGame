using System.Collections.Generic;
using UnityEngine;

public class LevelLayoutGenerator : MonoBehaviour
{
    public LevelChunkData firstChunk;
    public RoadState roadState;
    public Transform player;

    [Header("Chunk Settings")]
    public float chunkLength = 40f;
    public int chunksAhead = 20;
    public int chunksBehind = 6;

    [Header("Section Settings")]
    public int sectionSize = 18;

    private Vector3 currentPosition;
    private Vector3 currentForward;

    private float smoothCurvature;

    private float sectionCurvature;
    private float sectionWidth;
    private float sectionBanking;
    private float sectionElevation;

    private float elevationHeight;
    private float elevationPhase;

    private int chunksInCurrentSection;

    private List<GameObject> roadChunks = new List<GameObject>();

    void Start()
    {
        currentPosition = Vector3.zero;
        currentForward = Vector3.forward;

        BeginNewSection();

        for (int i = 0; i < chunksAhead; i++)
        {
            SpawnNextChunk();
        }
    }

    void Update()
    {
        if (!player) return;

        float distanceToEnd =
            Vector3.Distance(player.position, currentPosition);

        if (distanceToEnd < chunkLength * chunksAhead * 0.6f)
        {
            SpawnNextChunk();
        }

        Cleanup();
    }

    void BeginNewSection()
    {
        sectionCurvature = roadState.curvature;
        sectionWidth = roadState.width;
        sectionBanking = roadState.banking;
        sectionElevation = roadState.elevationEnergy;

        chunksInCurrentSection = 0;
    }

    void SpawnNextChunk()
    {
        if (chunksInCurrentSection >= sectionSize)
        {
            BeginNewSection();
        }

        GameObject prefab = firstChunk.levelChunks[0];

        GameObject chunk =
            Instantiate(prefab,
                        currentPosition,
                        Quaternion.LookRotation(currentForward, Vector3.up));

        ApplySectionReactivity(chunk);
        ApplyElevation(chunk);
        CreateTerrain(chunk);

        roadChunks.Add(chunk);

        currentPosition += currentForward * chunkLength;

        // Smooth curvature transition
        smoothCurvature = Mathf.Lerp(
            smoothCurvature,
            sectionCurvature,
            0.08f
        );

        float turn =
            Mathf.Lerp(-2f, 2f, smoothCurvature);

        currentForward =
            Quaternion.AngleAxis(turn, Vector3.up) * currentForward;

        currentForward.Normalize();

        chunksInCurrentSection++;
    }

    void ApplySectionReactivity(GameObject chunk)
    {
        float width =
            Mathf.Lerp(1.25f, 0.75f, sectionWidth);

        chunk.transform.localScale =
            new Vector3(width, 1f, 1f);

        float bank =
            Mathf.Lerp(-4f, 4f, sectionBanking);

        chunk.transform.Rotate(Vector3.forward, bank, Space.Self);
    }

    void ApplyElevation(GameObject chunk)
    {
        elevationPhase += 0.18f;

        float amplitude =
            Mathf.Lerp(2f, 9f, sectionElevation);

        float rhythmInfluence =
            roadState.player.rhythm;

        elevationHeight +=
            Mathf.Sin(elevationPhase) *
            amplitude *
            rhythmInfluence *
            0.02f;

        chunk.transform.position +=
            Vector3.up * elevationHeight;
    }

    void Cleanup()
    {
        if (roadChunks.Count <= chunksAhead + chunksBehind)
            return;

        GameObject oldest = roadChunks[0];

        float distance =
            Vector3.Distance(player.position, oldest.transform.position);

        if (distance > chunkLength * chunksBehind)
        {
            Destroy(oldest);
            roadChunks.RemoveAt(0);
        }
    }

    void CreateTerrain(GameObject chunk)
    {
        float sideOffset = 28f;

        GameObject left = GameObject.CreatePrimitive(PrimitiveType.Cube);
        GameObject right = GameObject.CreatePrimitive(PrimitiveType.Cube);

        left.transform.position =
            chunk.transform.position - chunk.transform.right * sideOffset;

        right.transform.position =
            chunk.transform.position + chunk.transform.right * sideOffset;

        left.transform.rotation = chunk.transform.rotation;
        right.transform.rotation = chunk.transform.rotation;

        left.transform.localScale =
            new Vector3(50f, 2f, chunkLength);

        right.transform.localScale =
            new Vector3(50f, 2f, chunkLength);

        left.transform.parent = chunk.transform;
        right.transform.parent = chunk.transform;
    }
}