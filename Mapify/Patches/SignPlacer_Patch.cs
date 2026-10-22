using DV.Signs;
using HarmonyLib;
using Mapify.Map;

namespace Mapify.Patches;

/// <summary>
/// Without this patch, dead end tracks break the speed limit sign generation
/// </summary>
[HarmonyPatch(typeof(SignPlacer), nameof(SignPlacer.GetSegmentInfoAfterJunction))]
public static class SignPlacer_GetSegmentInfoAfterJunction_Patch
{
    private static bool Prefix(ref SignPlacer.CurveSegmentInfo __result, Junction.Branch branch)
    {
        if (Maps.IsDefaultMap || branch.track == null) { return true; }

        Junction junction = branch.first ? branch.track.outJunction : branch.track.inJunction;
        if (junction != null)
        {
            return true;
        }

        __result = new SignPlacer.CurveSegmentInfo
        {
            assignedSpeed = 20
        };
        return false;
    }
}
