using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class TerrainChunk : MonoBehaviour
{
    [Header("Terrain Shape")]
    public int size = 20;
    public float vertexSpacing = 1f;
    public float heightScale = 2f;
    public float noiseScale = 0.1f;

    [Header("Road Blending")]
    public Transform roadCenter;
    public float roadHalfWidth = 3f;      // half of road width
    public float blendDistance = 10f;      // how far terrain reacts
    public float roadSink = 0.2f;          // terrain sinks slightly near road

    [Header("Color")]
    public Gradient calmGradient;
    public Gradient aggressiveGradient;

    [Header("Runtime Reactivity")]
    public PlayerDriveMetrics player;
    public RoadState roadState;
    public float calmHeightScale = 1.2f;
    public float aggressiveHeightScale = 3.5f;

    Mesh mesh;

    void Awake()
    {
        Generate();
    }

    void Generate()
    {
        mesh = new Mesh();
        mesh.name = "Terrain Chunk";
        GetComponent<MeshFilter>().mesh = mesh;

        int vertsPerLine = size + 1;
        Vector3[] verts = new Vector3[vertsPerLine * vertsPerLine];
        Color[] colors = new Color[verts.Length];
        int[] tris = new int[size * size * 6];

        Vector3 roadPos = roadCenter ? roadCenter.position : Vector3.zero;
        Vector3 roadForward = roadCenter ? roadCenter.forward.normalized : Vector3.forward;

        float engineStress = player ? player.engineStress : 0f;
        float agitation = roadState ? roadState.agitation : 0f;

        float dynamicHeightScale =
            Mathf.Lerp(
                calmHeightScale,
                aggressiveHeightScale,
                engineStress
            );

        int i = 0;

        for (int z = 0; z <= size; z++)
        {
            for (int x = 0; x <= size; x++)
            {
                Vector3 worldXZ =
                    transform.position +
                    new Vector3(x * vertexSpacing, 0f, z * vertexSpacing);

                // ================= BASE NOISE =================
                float noise =
                    Mathf.PerlinNoise(
                        worldXZ.x * noiseScale,
                        worldXZ.z * noiseScale
                    );

                float terrainHeight = noise * dynamicHeightScale;
                float finalHeight = terrainHeight;

                // ================= ROAD BLENDING =================
                if (roadCenter)
                {
                    Vector3 toPoint = worldXZ - roadPos;

                    Vector3 lateral =
                        toPoint -
                        Vector3.Dot(toPoint, roadForward) * roadForward;

                    float lateralDist = lateral.magnitude;

                    float t =
                        Mathf.InverseLerp(
                            roadHalfWidth,
                            roadHalfWidth + blendDistance,
                            lateralDist
                        );

                    // Smooth cubic falloff (important)
                    t = t * t * (3f - 2f * t);

                    float roadInfluence =
                        Mathf.Lerp(
                            -roadSink,
                            terrainHeight,
                            t
                        );

                    finalHeight = roadInfluence;
                }

                verts[i] = new Vector3(
                    x * vertexSpacing,
                    finalHeight,
                    z * vertexSpacing
                );

                // ================= COLOR =================
                Color calm = calmGradient.Evaluate(noise);
                Color aggressive = aggressiveGradient.Evaluate(noise);

                colors[i] =
                    Color.Lerp(
                        calm,
                        aggressive,
                        agitation
                    );

                i++;
            }
        }

        // ================= TRIANGLES =================
        int ti = 0;
        int vi = 0;

        for (int z = 0; z < size; z++)
        {
            for (int x = 0; x < size; x++)
            {
                tris[ti++] = vi;
                tris[ti++] = vi + vertsPerLine;
                tris[ti++] = vi + 1;

                tris[ti++] = vi + 1;
                tris[ti++] = vi + vertsPerLine;
                tris[ti++] = vi + vertsPerLine + 1;

                vi++;
            }
            vi++;
        }

        mesh.vertices = verts;
        mesh.triangles = tris;
        mesh.colors = colors;
        mesh.RecalculateNormals();

        GetComponent<MeshCollider>().sharedMesh = mesh;
    }
}
