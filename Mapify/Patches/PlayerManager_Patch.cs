using HarmonyLib;
using Mapify.Map;
using UnityEngine;

namespace Mapify.Patches;

/// <summary>
/// IsPlayerPositionValid makes assumptions about the water level, this patch does not.
/// </summary>
[HarmonyPatch(typeof(PlayerManager), nameof(PlayerManager.IsPlayerPositionValid))]
public static class PlayerManager_IsPlayerPositionValid_Patch
{
    private static bool Prefix(Vector3 playerPosition, ref bool __result)
    {
        if (Maps.IsDefaultMap) return true;

        var margin = 0.5;
        __result = !(
            playerPosition.x > LevelInfo.WorldSize.x + margin
            || playerPosition.x < -margin
            || playerPosition.z > LevelInfo.WorldSize.z + margin
            || playerPosition.z < -margin
            || playerPosition.y > (double) LevelInfo.WorldSize.y
            || playerPosition.y < -50); // <-- this is the only difference with the original
        return false;
    }
}


