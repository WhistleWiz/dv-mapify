using System;
using HarmonyLib;

namespace Mapify.Patches;

/// <summary>
/// Gets rid of a mysterious NullReferenceException that freezes the game
/// </summary>
[HarmonyPatch(typeof(InventoryItemSpec), nameof(InventoryItemSpec.LocalizedName), MethodType.Getter)]
public class InventoryItemSpec_LocalizedName_Patch
{
    static Exception Finalizer(Exception __exception, ref string __result)
    {
        if(__exception is null) return null;

        // can't add __instance.gameObject.name to the log, that will cause another NullReferenceException
        Mapify.LogError($"{nameof(InventoryItemSpec_LocalizedName_Patch)}: {__exception.Message}");
        __result = __exception.Message;
        return null;
    }
}
