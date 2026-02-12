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

    [Header("Dream Surface")]
    public Material surfaceMaterial;
    public float surfaceWidth = 140f;
    public float surfaceYOffset = -2f;

    private Vector3 currentPosition;
    private Vector3 currentForward;

    private float smoothCurvature;

    private float sectionCurvature;
    private float sectionWidth;
    private float sectionBanking;
    private float sectionElevation;

    private int chunksInCurrentSection;

    private float cumulativeElevation;

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
        CreateDreamSurface(chunk);

        roadChunks.Add(chunk);

        currentPosition += currentForward * chunkLength;

        // Smooth curvature
        smoothCurvature = Mathf.Lerp(
            smoothCurvature,
            sectionCurvature,
            0.05f
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
            Mathf.Lerp(1.3f, 0.75f, sectionWidth);

        chunk.transform.localScale =
            new Vector3(width, 1f, 1f);

        float bank =
            Mathf.Lerp(-4f, 4f, sectionBanking);

        chunk.transform.Rotate(Vector3.forward, bank, Space.Self);
    }

    void ApplyElevation(GameObject chunk)
    {
        float amplitude =
            Mathf.Lerp(0.5f, 4f, sectionElevation);

        cumulativeElevation += amplitude * 0.02f;

        chunk.transform.position +=
            Vector3.up * cumulativeElevation;
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

    void CreateDreamSurface(GameObject chunk)
    {
        GameObject surface =
            GameObject.CreatePrimitive(PrimitiveType.Quad);

        surface.transform.position =
            chunk.transform.position
            + Vector3.up * surfaceYOffset;

        surface.transform.rotation =
            chunk.transform.rotation *
            Quaternion.Euler(90f, 0f, 0f);

        surface.transform.localScale =
            new Vector3(surfaceWidth, chunkLength, 1f);

        surface.transform.parent = chunk.transform;

        if (surfaceMaterial != null)
        {
            surface.GetComponent<MeshRenderer>().material =
                surfaceMaterial;
        }

        Destroy(surface.GetComponent<Collider>());
    }
}