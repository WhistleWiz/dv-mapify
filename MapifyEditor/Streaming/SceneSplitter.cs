#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mapify.Editor.Utils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Mapify.Editor
{
    public static class SceneSplitter
    {
        private static bool isWaitingForCleanup;
        private static List<Scene> openedChunkScenes = new List<Scene>();

        private struct SplittedSceneInfo
        {
            public string name;
            public string path;
        }

        private struct StreamingObject
        {
            public GameObject baseGameObject;
            public int xChunk;
            public int zChunk;
        }

        public static SceneSplitData SplitScene(Scene editorScene, MapInfo mapInfo)
        {
            if (isWaitingForCleanup) throw new InvalidOperationException($"{nameof(SceneSplitter)}#{nameof(Cleanup)} must be called before {nameof(SceneSplitter)}#{nameof(SplitScene)} can be called again!");
            isWaitingForCleanup = true;

            EditorUtility.DisplayProgressBar("Mapify", "Splitting streaming scenes", 0.0f);

            int chunkSize = mapInfo.chunkSize;

            var streamingObjects = editorScene.GetRootGameObjects()
                .SelectMany(obj =>
                    obj.GetFirstComponentInChildren<LODGroup>()
                        .Cast<Component>()
                        .Concat(obj.GetFirstComponentInChildren<Renderer>())
                        .Select(r => r.gameObject)
                        .Distinct()
                )
                .Select(obj => new StreamingObject
                {
                    baseGameObject = obj,
                    xChunk = Mathf.FloorToInt(obj.transform.position.x / chunkSize),
                    zChunk = Mathf.FloorToInt(obj.transform.position.z / chunkSize)
                })
                .ToArray();

            if (streamingObjects.Length == 0)
            {
                EditorUtility.ClearProgressBar();
                return new SceneSplitData();
            }

            int numChunksX = Mathf.CeilToInt(mapInfo.worldSize / chunkSize);
            int numChunksZ = Mathf.CeilToInt(mapInfo.worldSize / chunkSize);
            int chunkCount = numChunksX * numChunksZ;

            var splittedScenes = CreateSplitScenes(numChunksX, numChunksZ, editorScene.name);
            openedChunkScenes = new List<Scene>();

            for (int chunkX = 0; chunkX < numChunksX; chunkX++)
            {
                for (int chunkZ = 0; chunkZ < numChunksZ; chunkZ++)
                {
                    var chunkOneDimensionalIndex = chunkX * numChunksZ + chunkZ;

                    Debug.Log($"Splitting scene ({chunkX}, {chunkZ})");
                    EditorUtility.DisplayProgressBar("Mapify", $"Splitting streaming scene ({chunkX}, {chunkZ})", chunkOneDimensionalIndex / (float)chunkCount);

                    // https://www.jetbrains.com/help/rider/AccessToModifiedClosure.html
                    var x = chunkX;
                    var z = chunkZ;
                    var objectsInThisChunk = streamingObjects
                        .Where(obj => obj.xChunk == x && obj.zChunk == z)
                        .ToArray();

                    // makes the export quicker
                    if(!objectsInThisChunk.Any()) continue;

                    var splitted = splittedScenes[chunkOneDimensionalIndex];
                    Scene chunkScene = EditorSceneManager.OpenScene(splitted.path, OpenSceneMode.Additive);
                    openedChunkScenes.Add(chunkScene);

                    GameObject chunkRoot = new GameObject(splitted.name);
                    SceneManager.MoveGameObjectToScene(chunkRoot, chunkScene);

                    foreach (var streamObj in objectsInThisChunk)
                    {
                        Object.Instantiate(streamObj.baseGameObject, chunkRoot.transform, true);
                    }
                }
            }

            EditorSceneManager.SaveScenes(openedChunkScenes.ToArray());
            EditorUtility.ClearProgressBar();

            return new SceneSplitData {
                names = splittedScenes.Select(x => x.name).ToArray(),
                xSize = chunkSize,
                ySize = 0, // We don't support vertical chunks
                zSize = chunkSize
            };
        }

        private static SplittedSceneInfo[] CreateSplitScenes(int numChunksX, int numChunksZ, string editorSceneName)
        {
            EditorUtility.DisplayProgressBar("Mapify", "Creating split streaming scene", 0.0f);

            const string saveDir = Scenes.STREAMING_DIR;
            if (Directory.Exists(saveDir))
            {
                Directory.Delete(saveDir, true);
            }
            Directory.CreateDirectory(saveDir);

            var chunkCount = numChunksX * numChunksZ;
            var splittedScenes = new SplittedSceneInfo[chunkCount];
            string firstScenePath = "";

            for (int chunkX = 0; chunkX < numChunksX; chunkX++)
            {
                for (int chunkZ = 0; chunkZ < numChunksZ; chunkZ++)
                {
                    var chunkOneDimensionalIndex = chunkX * numChunksZ + chunkZ;
                    EditorUtility.DisplayProgressBar("Mapify", $"Creating split streaming scene ({chunkX}, {chunkZ})", chunkOneDimensionalIndex / (float)chunkCount);

                    string sceneName = $"{editorSceneName}__x{chunkX}_z{chunkZ}";
                    string scenePath = $"{saveDir}/{sceneName}.unity";

                    if (chunkX == 0 && chunkZ == 0)
                    {
                        CreateEmptyScene(scenePath);
                        firstScenePath = scenePath;
                    }
                    else
                    {
                        File.Copy(firstScenePath, scenePath);
                    }

                    splittedScenes[chunkOneDimensionalIndex] = new SplittedSceneInfo
                    {
                        path = scenePath,
                        name = sceneName
                    };
                }
            }

            AssetDatabase.Refresh();
            return splittedScenes;
        }

        private static void CreateEmptyScene(string scenePath)
        {
            // This is *super* sketchy, but Unity won't let me create a new scene in additive mode so this is what we gotta do ¯\_(ツ)_/¯
            using StreamWriter writer = File.CreateText(scenePath);
            writer.WriteLine("%YAML 1.1");
            writer.WriteLine("%TAG !u! tag:unity3d.com,2011:");
        }

        public static void Cleanup(bool deleteScenes = true)
        {
            foreach (Scene scene in openedChunkScenes)
            {
                EditorSceneManager.CloseScene(scene, true);
            }

            const string saveDir = Scenes.STREAMING_DIR;
            if (deleteScenes && Directory.Exists(saveDir))
            {
                Directory.Delete(saveDir, true);
                File.Delete($"{saveDir}.meta");
            }

            isWaitingForCleanup = false;
        }

        [MenuItem("Mapify/Debug/Split Scene", priority = int.MaxValue)]
        private static void DebugSplitScenes()
        {
            SplitScene(SceneManager.GetSceneByPath(Scenes.STREAMING), EditorAssets.FindAsset<MapInfo>());
            Cleanup(false);
        }
    }
}
#endif
