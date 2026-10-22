using System.Collections.Generic;
using DV.Teleporters;
using HarmonyLib;
using Mapify.Map;
using UnityEngine;

namespace Mapify.Patches;

/// <summary>
/// When the game is updated or the custom map is changed, the player gets teleported to the nearest station by StartGameData_FromSaveGame. Only stations that have a locomotive with a license that the player has are considered. However, this check fails if a station does not have any StationLocoSpawners. So in this patch we consider all stations if no suitable station has been found.
/// </summary>
[HarmonyPatch(typeof(StationFastTravelDestination), nameof(StationFastTravelDestination.GetClosestStationTeleporterWithPlayerLicensedLoco))]
public static class StationFastTravelDestination_ResampleEquidistant_Patch
{
    private static void Postfix(ref StationFastTravelDestination __result, Vector3 positionToCheck)
    {
        if (Maps.IsDefaultMap || __result != null)
        {
            return;
        }

        var stationTeleporters = new List<StationFastTravelDestination>();

        foreach (var activeDestination in FastTravelDestination.ActiveDestinations)
        {
            if (activeDestination is not StationFastTravelDestination travelDestination) continue;
            stationTeleporters.Add(travelDestination);
        }

        __result = StationFastTravelDestination.GetClosestStationTeleporter(stationTeleporters, positionToCheck);
    }
}
