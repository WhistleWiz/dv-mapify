using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mapify;

/// <summary>
/// Adds streaming scenes to our Streamer when they're loaded
/// </summary>
public class StreamerInit: MonoBehaviour
{
    public Streamer streamer;

    private void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode _)
    {
        if (!streamer.sceneCollection.names.Contains(scene.name)) return;

        Mapify.LogDebug($"{nameof(StreamerInit)}.{nameof(OnSceneLoaded)}: adding {scene.name} to streamer");
        if (scene.rootCount == 0)
        {
            var chunkRoot = new GameObject(scene.name);
            SceneManager.MoveGameObjectToScene(chunkRoot, scene);
            streamer.AddSceneGO(scene.name, chunkRoot);
            return;
        }
        streamer.AddSceneGO(scene.name, scene.GetRootGameObjects().First());
    }
}
