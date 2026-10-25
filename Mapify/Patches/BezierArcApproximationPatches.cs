using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using static BezierArcApproximation;

namespace Mapify.Patches
{
    [HarmonyPatch(typeof(BezierArcApproximation))]
    internal class BezierArcApproximationPatches
    {
        private const float BigRadius = 1000000;

        [HarmonyPostfix, HarmonyPatch(nameof(CalculateArcs))]
        private static void CalculateArcsPostfix(BezierCurve bez, List<Arc> result)
        {
            // We have good food.
            if (result.Count != 0) return;

            // The track was probably so perfectly straight the calculation
            // could not approximate an arc. So instead we feed it a fake
            // arc with an extremely large radius.
            result.Add(new()
            {
                bezierStartT = 0,
                bezierEndT = 1,
                r = BigRadius,
                e = bez.length / BigRadius,
                s = 0,
                center = Vector3.zero
            });

            Mapify.LogInfo($"Curve '{bez.name}' could not approximate an arc");
        }
    }
}
