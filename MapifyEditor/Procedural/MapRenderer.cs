#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace Mapify.Editor
{
    public static class MapRenderer
    {
        private const int TEXTURE_SIZE = 2048;

        public static void RenderMapFromTerrain(Terrain[] terrains, MapInfo mapInfo)
        {
            Texture2D combinedHeightmap = CreateHeightmap(terrains, mapInfo);
            Texture2D scaledHeightmap = Resize(combinedHeightmap, TEXTURE_SIZE, TEXTURE_SIZE);
            mapInfo.mapTextureSerialized = scaledHeightmap.EncodeToJPG();
            mapInfo.mapTextureSize = new[] { scaledHeightmap.width, scaledHeightmap.height };
        }

        private static Texture2D CreateHeightmap(IReadOnlyList<Terrain> terrains, MapInfo mapInfo)
        {
            int terrainCount = terrains.Count;

            int heightmapResolution = terrains[0].terrainData.heightmapResolution;
            int terrainWidth = heightmapResolution - 1;

            // beginning of function rewrite that doesn't crash when processing combined heightmaps larger than 16k wide:
            int combinationSize = 16384; // per-side size of combinedTexture, now a fixed value
            
            // Find how small the tiles need to be to fit into a combinationSize width texture.
            int maxTileSize = Mathf.CeilToInt(combinationSize / Mathf.CeilToInt(Mathf.Sqrt(terrainCount)));

            // Find the largest power of 2 smaller than (or equal to) that for actual tile texture size target
            int maxPermissibleRes = Mathf.FloorToInt(Mathf.Pow(2, Mathf.Floor(Mathf.Log(maxTileSize, 2))));

            // how wide will combinedTexture actually be?
            int scaledWidth = maxPermissibleRes * Mathf.CeilToInt(Mathf.Sqrt(terrainCount));

            // now declare this with the actual combined size
            Texture2D combinedTexture = new Texture2D(scaledWidth, scaledWidth, TextureFormat.RGBA32, false);

            // these are unchanged, but now measure in maxPermissibleRes instead of terrainWidth
            int currentX = 0;
            int currentY = 0;

            // colors[] needs to still be the size (px count) of the tile heightmap so this is fine
            Color[] colors = new Color[terrainWidth * terrainWidth]; // do not change


            for (int i = 0; i < terrainCount; i++)
            {
                // I need a new texture to store the tile heightmap data into, per tile
                Texture2D tempTexture = new Texture2D(terrainWidth, terrainWidth, TextureFormat.RGBA32, false);

                // This block checks the heightmap of the tile pixel by pixel, figures out the correct map color at that
                // spot, and saves that color into the array colors[] - The Worst Bitmap Format(TM).
                // None of this changes.
                Terrain terrain = terrains[i];
                float terrainY = terrain.transform.position.y;
                TerrainData terrainData = terrain.terrainData;
                float terrainHeight = terrainData.size.y;
                float[,] heightmapData = terrainData.GetHeights(0, 0, heightmapResolution, heightmapResolution);

                for (int y = 0; y < terrainWidth; y++)
                {
                    for (int x = 0; x < terrainWidth; x++)
                    {
                        float height = heightmapData[y, x];
                        float worldHeight = terrainY + height * terrainHeight;
                        float waterLevel = worldHeight / mapInfo.waterLevel;
                        float terrainLevel = worldHeight / (terrainY + terrainHeight);
                        Color color = worldHeight <= mapInfo.waterLevel
                            ? mapInfo.waterColor.Evaluate(float.IsNaN(waterLevel) ? 0.0f : waterLevel)
                            : mapInfo.terrainColor.Evaluate(float.IsNaN(terrainLevel) ? 0.0f : terrainLevel);
                        colors[y * terrainWidth + x] = color;
                    }
                }

                // lay the heightmap colors into the tempTexture
                // and don't forget to Apply() it, because Graphics.Blit() runs on the GPU, not the CPU
                tempTexture.SetPixels(colors);
                tempTexture.Apply();

                // This sets up a RenderTexture and calls Graphics.Blit() so that I don't need to. Keep your code DRY, Cvetka.
                Texture2D tempScaledTexture = Resize(tempTexture, maxPermissibleRes, maxPermissibleRes);

                // now copy THAT image into where it goes within combinedTexture.
                Graphics.CopyTexture(tempScaledTexture, 0, 0, 0, 0, maxPermissibleRes, maxPermissibleRes, combinedTexture, 0, 0, currentX, currentY);

                // The rest of this block steps to the next terrain tile; basically unchanged, but now runs on maxPermissibleRes instead of
                // terrainWidth, and scaledWidth instead of totalWidth (which is no longer used at all and has been removed.)
                currentX += maxPermissibleRes;
                if (currentX + maxPermissibleRes <= scaledWidth)
                    continue;
                currentX = 0;
                currentY += maxPermissibleRes;
            }

            combinedTexture.Apply();

            return combinedTexture;
        }

        private static Texture2D Resize(Texture2D source, int width, int height)
        {
            RenderTexture rt = RenderTexture.GetTemporary(width, height, 0);
            Graphics.Blit(source, rt, new Material(Shader.Find("Hidden/BlitCopy")));

            Texture2D result = new Texture2D(width, height, TextureFormat.RGBA32, false);
            result.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            result.Apply();

            RenderTexture.ReleaseTemporary(rt);
            return result;
        }

        public static void RenderMapFromImage(MapInfo mapInfo)
        {
            Texture2D scaledMapImage = Resize(mapInfo.fixedMapImage, TEXTURE_SIZE, TEXTURE_SIZE);
            mapInfo.mapTextureSerialized = scaledMapImage.EncodeToJPG();
            mapInfo.mapTextureSize = new[] { scaledMapImage.width, scaledMapImage.height };
        }
    }
}
#endif
