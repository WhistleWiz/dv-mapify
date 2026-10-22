using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class BezierCurveUpgrade
{
    public static void Upgrade(BezierCurve curve)
    {
        // resolution logic change - adapt old serialized resolution value to new behavior
        if (curve.version < 2)
        {
            if (curve.pointCount >= 2)
            {
                Debug.Log(string.Format("[BezierCurve upgrade] adapting curve resolution value for '{0}'", curve.name), curve);

                // will use maximum resolution that matches old curve interpolation density
                float shortestSegmentLength = float.MaxValue;
                for (int i = 0; i < curve.pointCount - 1; i++)
                {
                    float length = BezierCurve.ApproximateLength(curve[i], curve[i + 1], numPoints: 5);
                    if (length < shortestSegmentLength) shortestSegmentLength = length;
                }
                float newRes = curve.resolution / shortestSegmentLength;
                Debug.Log(string.Format("Old resolution: {0}, new resolution: {1}", curve.resolution, newRes), curve);

                curve.resolution = newRes;
            }

            curve.version = 2;
        }
    }
}
