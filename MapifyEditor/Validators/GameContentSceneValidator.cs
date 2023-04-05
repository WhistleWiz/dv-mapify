using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mapify.Editor.Validators
{
    public class GameContentSceneValidator : SceneValidator
    {
        protected override IEnumerator<Result> ValidateScene(Scene terrainScene, Scene railwayScene, Scene gameContentScene)
        {
            GameObject[] roots = gameContentScene.GetRootGameObjects();

            #region Lights

            Light[] lights = roots.SelectMany(go => go.GetComponentsInChildren<Light>()).ToArray();
            int directionalLightCount = lights.Count(light => light.type == LightType.Directional);
            if (directionalLightCount != 1)
                yield return Result.Error($"There must be exactly one directional light in the {GetPrettySceneName()} scene. Found {directionalLightCount}");

            #endregion

            #region Stations

            Track[] tracks = railwayScene.GetRootGameObjects()
                .SelectMany(go => go.GetComponentsInChildren<Track>())
                .ToArray();
            foreach (Station station in roots.SelectMany(go => go.GetComponentsInChildren<Station>()))
            {
                // Tracks
                station.storageTrackNames = new List<string>();
                station.transferInTrackNames = new List<string>();
                station.transferOutTrackNames = new List<string>();
                foreach (Track track in tracks)
                {
                    Match match = RailwaySceneValidator.STATION_TRACK_NAME_PATTERN.Match(track.name);
                    if (!match.Success) continue;
                    if (match.Groups[1].Value != station.stationID) continue;
                    string trackType = match.Groups[4].Value;
                    switch (trackType)
                    {
                        case "S":
                            station.storageTrackNames.Add(track.name);
                            break;
                        case "I":
                            station.transferInTrackNames.Add(track.name);
                            break;
                        case "O":
                            station.transferOutTrackNames.Add(track.name);
                            break;
                    }
                }

                // Teleport location
                if (station.teleportLocation == null)
                    yield return Result.Error($"You must set a teleport location for station {station.stationName}!", station);

                // Job booklet spawn area
                VanillaObject vanillaObject = station.GetComponent<VanillaObject>();
                if ((vanillaObject == null || !$"{vanillaObject.asset}".StartsWith("Station")) && station.bookletSpawnArea == null)
                    yield return Result.Error($"You must specify a job booklet spawn area for custom station {station.stationName}!", station);

                station.inputCargoGroupsSerialized = JsonUtility.ToJson(station.inputCargoGroups);
                station.outputCargoGroupsSerialized = JsonUtility.ToJson(station.outputCargoGroups);
            }

            #endregion
        }

        public override string GetScenePath()
        {
            return "Assets/Scenes/GameContent.unity";
        }
    }
}