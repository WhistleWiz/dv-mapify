#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Mapify.Editor;
using Mapify.Editor.Utils;
using Mapify.Editor.Validators;
using UnityEngine;

namespace MapifyEditor.Export.Validators.Project
{
    public class MapInfoValidator : Validator
    {
        private const string MAP_NAME_REGEX = "[a-zA-Z0-9-_& ]";
        private const string CUSTOM_CARGO_MOD_ID = "DVCustomCargo";

        protected override IEnumerator<Result> Validate(Scenes scenes)
        {
            MapInfo[] mapInfos = EditorAssets.FindAssets<MapInfo>();
            if (mapInfos.Length != 1)
            {
                yield return Result.Error($"There should be exactly one MapInfo! Found {mapInfos.Length}");
                yield break;
            }

            MapInfo mapInfo = mapInfos[0];

            if (!Regex.IsMatch(mapInfo.name, MAP_NAME_REGEX))
                yield return Result.Error($"Your map name must match the following pattern: {MAP_NAME_REGEX}", mapInfo);
            if (mapInfo.name == Names.DEFAULT_MAP_NAME)
                yield return Result.Error($"Your map name cannot be {Names.DEFAULT_MAP_NAME}");

            // Loading Screen
            // mapInfo.LoadingScreenImages is null after upgrading from an older Mapify version that didn't support custom loading screens
            if (mapInfo.LoadingScreenImages != null && mapInfo.LoadingScreenImages.Any(image => image == null))
            {
                yield return Result.Error("Loading screen image is null", mapInfo);
            }

            // World
            if (mapInfo.waterLevel < -1)
                yield return Result.Error("Water level cannot be lower than -1", mapInfo);

            Terrain[] terrains = scenes.terrainScene.GetAllComponents<Terrain>();
            if (terrains.Length > 0)
            {
                float worldSize = terrains.CalculateWorldSize();
                float worldHeight = terrains[0].transform.position.y;

                Vector3 spawnPos = mapInfo.defaultSpawnPosition;
                if (spawnPos.x < 0 || spawnPos.z < 0 || spawnPos.x > worldSize || spawnPos.x > worldSize)
                    yield return Result.Error($"The spawn position's X and Z values must be within the world's bounds (0-{worldSize})", mapInfo);
                if (spawnPos.y < worldHeight)
                    yield return Result.Error($"The spawn position's Y value must be above the terrain ({worldHeight})", mapInfo);
                if (spawnPos.y < mapInfo.waterLevel)
                    yield return Result.Error($"The spawn position must be above the water level ({mapInfo.waterLevel}", mapInfo);
            }

            if (mapInfo.useFixedMapImage)
            {
                if (mapInfo.fixedMapImage == null)
                {
                    yield return Result.Error($"MapInfo: '{nameof(MapInfo.fixedMapImage)}' must be set when '{nameof(MapInfo.useFixedMapImage)}' is true", mapInfo);
                }
                else if(mapInfo.fixedMapImage.width != mapInfo.fixedMapImage.height)
                {
                    yield return Result.Warning($"MapInfo: '{nameof(MapInfo.fixedMapImage)}' should be square or it will be stretched. Current dimensions: {mapInfo.fixedMapImage.width}x{mapInfo.fixedMapImage.height}", mapInfo);
                }
            }

            // required mods
            if (!mapInfo.requiredMods.Contains(CUSTOM_CARGO_MOD_ID) && scenes.gameContentScene.GetAllComponents<WarehouseMachine>().SelectMany(whm => whm.supportedCustomCargoTypes).Any())
            {
                yield return Result.Warning($"MapInfo: your map uses one or more custom cargos but the Custom Cargo mod is not in the required mods list. You should add '{CUSTOM_CARGO_MOD_ID}' and the ID of the mod that provides the custom cargo your map uses to the required mods list on MapInfo", mapInfo);
            }
        }
    }
}
#endif
