// Procedural Terrain Painter by Staggart Creations http://staggart.xyz
// Copyright protected under Unity Asset Store EULA

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

#if UNITY_2021_2_OR_NEWER
using UnityEngine.TerrainTools;
#else
using UnityEngine.Experimental.TerrainAPI;
#endif

namespace sc.terrain.proceduralpainter
{
    public class ModifierStack
    {
        private static int m_resolution;
        private static float heightScale;

        public static Material filterMat;
        private static RenderTexture alphaMap;

        private const string UndoActionName = "Painted Terrain";

        private static readonly int HeightmapID = Shader.PropertyToID("_Heightmap");
        private static readonly int HeightmapScaleID = Shader.PropertyToID("_HeightmapScale");
        private static readonly int NormalMapID = Shader.PropertyToID("_NormalMap");
        private static readonly int TerrainPosScaleID = Shader.PropertyToID("_TerrainPosScale");
        private static readonly int TerrainBoundsID = Shader.PropertyToID("_TerrainBounds");

        /// <summary>
        /// Call once per terrain
        /// </summary>
        public static void Configure(Terrain terrain, Bounds bounds, int resolution)
        {
            if (alphaMap == null || m_resolution != resolution)
            {
                if (alphaMap != null)
                {
                    alphaMap.Release();
                    Object.DestroyImmediate(alphaMap);
                }

                alphaMap = new RenderTexture(resolution, resolution, 0, GraphicsFormat.R8_UNorm)
                {
                    name = "TerrainPainter_AlphaMap",
                    enableRandomWrite = false
                };

                alphaMap.Create();
                m_resolution = resolution;
            }

            if (!filterMat)
                filterMat = new Material(Shader.Find("Hidden/TerrainPainter/Modifier"));

            filterMat.SetTexture(HeightmapID, terrain.terrainData.heightmapTexture);
            filterMat.SetTexture(NormalMapID, terrain.normalmapTexture);

            heightScale = bounds.max.y - bounds.min.y;
            filterMat.SetFloat(HeightmapScaleID, heightScale);

            float invWidth = 1f / bounds.size.x;
            float invHeight = 1f / bounds.size.z;

            Vector4 terrainPosScale = new Vector4(
                (terrain.GetPosition().x - bounds.min.x) * invWidth,
                (terrain.GetPosition().z - bounds.min.z) * invHeight,
                terrain.terrainData.size.x * invWidth,
                terrain.terrainData.size.z * invHeight
            );

            filterMat.SetVector(TerrainPosScaleID, terrainPosScale);
            filterMat.SetVector(
                TerrainBoundsID,
                new Vector4(bounds.min.x, bounds.max.z, bounds.size.x, bounds.size.z)
            );
        }

        public static void ProcessLayers(Terrain terrain, List<LayerSettings> layerSettings)
        {
            for (int i = layerSettings.Count - 1; i >= 0; i--)
            {
                ProcessSingleLayer(terrain, layerSettings[i]);
            }
        }

        public static void ProcessSingleLayer(Terrain terrain, LayerSettings settings)
        {
            if (!settings.enabled || !settings.layer) return;

            Graphics.SetRenderTarget(alphaMap);
            Graphics.Blit(Texture2D.whiteTexture, alphaMap);

            for (int i = settings.modifierStack.Count - 1; i >= 0; i--)
            {
                settings.modifierStack[i].Configure(filterMat);
                settings.modifierStack[i].Execute(alphaMap);
            }

            Vector2 scaledSize = new Vector2(
                terrain.terrainData.size.x,
                terrain.terrainData.size.z
            );

            PaintContext ctx = TerrainPaintUtility.BeginPaintTexture(
                terrain,
                new Rect(0, 0, scaledSize.x, scaledSize.y),
                settings.layer
            );

            Graphics.Blit(alphaMap, ctx.destinationRenderTexture);
            TerrainPaintUtility.EndPaintTexture(ctx, UndoActionName);
        }

        /// <summary>
        /// Safe cleanup (called on disable / domain reload)
        /// </summary>
        public static void Dispose()
        {
            if (alphaMap != null)
            {
                alphaMap.Release();
                Object.DestroyImmediate(alphaMap);
                alphaMap = null;
            }

            m_resolution = 0;
        }
    }
}
