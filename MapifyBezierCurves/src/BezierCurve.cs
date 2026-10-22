#region UsingStatements

using System;
using System.Collections.Generic;
using UnityEngine;

#endregion

/// <summary>
///     - Class for describing and drawing Bezier Curves
///     - Efficiently handles approximate length calculation through 'dirty' system
///     - Has static functions for getting points on curves constructed by Vector3 parameters (GetPoint, GetCubicPoint, GetQuadraticPoint, and GetLinearPoint)
/// </summary>
[ExecuteInEditMode]
[Serializable]
public class BezierCurve : MonoBehaviour, ISerializationCallbackReceiver
{
    // the number of interpolated points for each curve segment (pair of BezierPoints) is calculated
    // by approximating the segment length using this crude value, just to get enough precision to
    // account for segments's shape dictated by its handles (instead of just using straight lines)
    private const int RESOLUTION_TO_NUMPOINTS_FACTOR = 3;

    #region PublicVariables

    [HideInInspector] public int version = 1; // will be updated by BezierCurveUpgrade

    /// <summary>
    ///     - used for calculating the number of interpolated points for each pair of bezier points
    ///     - the value corresponds approximately to the number of points per meter
    ///     - the number of interpolated points will be approximately "length of curve * resolution"
    ///     - used for calculating the "length" variable
    /// </summary>
    public float resolution = 5;

    [NonSerialized] public bool lengthDirty = true;

    /// <summary>
    ///     - color this curve will be drawn with in the editor
    ///             - set in the editor
    /// </summary>
    public Color drawColor = Color.white;

    public static bool drawInterpolatedPoints = false; // have all curves in scene use the same value

    #endregion

    #region PublicProperties

    /// <summary>
    ///     - set in the editor
    ///     - used to determine if the curve should be drawn as "closed" in the editor
    ///     - used to determine if the curve's length should include the curve between the first and the last points in "points" array
    ///     - setting this value will cause the curve to become dirty
    /// </summary>
    [SerializeField] private bool _close;
    public bool close
    {
        get { return _close; }
        set
        {
            if (_close == value) return;
            _close = value;
            lengthDirty = true;
            interpolationCacheDirty = true;
        }
    }

    [SerializeField]
    private bool _mirror;
    public bool mirror
    {
        get { return _mirror; }
        set
        {
            if (_mirror == value) return;
            _mirror = value;
            lengthDirty = true;
            interpolationCacheDirty = true;
        }
    }

    public enum Axis { X, Y, Z }

    [SerializeField]
    private Axis _axis;
    public Axis axis
    {
        get { return _axis; }
        set
        {
            if (_axis == value) return;
            _axis = value;
            lengthDirty = true;
            interpolationCacheDirty = true;
        }
    }

    /// <summary>
    ///             - set internally
    ///             - gets point corresponding to "index" in "points" array
    ///             - does not allow direct set
    /// </summary>
    /// <param name='index'>
    ///     - the index
    /// </param>
    public BezierPoint this[int index]
    {
        get { return points[index]; }
    }

    /// <summary>
    ///     - number of points stored in 'points' variable
    ///             - set internally
    ///             - does not include "handles"
    /// </summary>
    /// <value>
    ///     - The point count
    /// </value>
    public int pointCount
    {
        get { return points.Length; }
    }

    /// <summary>
    ///     - The approximate length of the curve
    ///     - recalculates if the curve is "dirty"
    /// </summary>
    private float _length;
    public float length
    {
        get
        {
            if (lengthDirty || _length == 0)
            {
                _length = 0;
                for (int i = 0; i < points.Length - 1; i++)
                {
                    _length += ApproximateLength(points[i], points[i + 1], resolution);
                }

                if (close) _length += ApproximateLength(points[points.Length - 1], points[0], resolution);

                lengthDirty = false;
            }

            return _length;
        }
    }

    [NonSerialized] public int lastClickedPointIndex = -1;

    #endregion

    #region PrivateVariables

    /// <summary>
    ///     - Array of point objects that make up this curve
    ///             - Populated through editor
    /// </summary>
    [SerializeField] private BezierPoint[] points = new BezierPoint[0];

    #endregion

    #region UnityFunctions

    private bool interpolationCacheDirty = true;
    private Vector3[][] interpolationCache = Array.Empty<Vector3[]>();

    //TODO When DV upgrades to Unity 2023.1+, we should use Gizmos.DrawLineList which is faster
    void OnDrawGizmos()
    {
        Gizmos.color = drawColor;

        if (points.Length <= 1) return;

        if (interpolationCacheDirty)
        {
            RecalculateInterpolationCache();
            interpolationCacheDirty = false;
        }

        for (int i = 0; i < interpolationCache.Length - 1; i++)
        {
            var interpolated = interpolationCache[i];

            Vector3 lastPoint = interpolated[0];
            Vector3 currentPoint = Vector3.zero;
            DrawInterpolatedPoint(lastPoint);

            for (int j = 1; j < interpolated.Length; j++)
            {
                currentPoint = interpolated[j];
                Gizmos.DrawLine(lastPoint, currentPoint);
                lastPoint = currentPoint;
                DrawInterpolatedPoint(lastPoint);
            }
        }

        // we don't use close and mirror in Mapify so I've left them commented out

        // if (close) DrawCurve(points[points.Length - 1], points[0], resolution);
        //
        // if (mirror)
        // {
        //     for (int i = 0; i < points.Length - 1; i++)
        //     {
        //         DrawCurveMirrored(transform, points[i], points[i + 1], resolution, axis);
        //     }
        // }
    }

    private void RecalculateInterpolationCache()
    {
        interpolationCache = new Vector3[points.Length][];

        for (int i = 0; i < points.Length - 1; i++)
        {
            var p1 = points[i];
            var p2 = points[i + 1];

            interpolationCache[i] = Interpolate(p1.position, p1.globalHandle2, p2.position, p2.globalHandle1, 1f);
        }
    }

    void Awake()
    {
        BezierCurveUpgrade.Upgrade(this);
        lengthDirty = true;
        interpolationCacheDirty = true;
    }

    #endregion

    #region PublicFunctions

    public void SnapAllNodesToAxis(Axis axis)
    {
        for (int i = 0; i < points.Length; i++)
        {
            switch (axis)
            {
                case Axis.X:
                    points[i].localPosition = new Vector3(0, points[i].localPosition.y, points[i].localPosition.z);
                    points[i].handle1 = new Vector3(0, points[i].handle1.y, points[i].handle1.z);
                    points[i].handle2 = new Vector3(0, points[i].handle2.y, points[i].handle2.z);
                    break;
                case Axis.Y:
                    points[i].localPosition = new Vector3(points[i].localPosition.x, 0, points[i].localPosition.z);
                    points[i].handle1 = new Vector3(points[i].handle1.x, 0, points[i].handle1.z);
                    points[i].handle2 = new Vector3(points[i].handle2.x, 0, points[i].handle2.z);
                    break;
                case Axis.Z:
                    points[i].localPosition = new Vector3(points[i].localPosition.x, points[i].localPosition.y, 0);
                    points[i].handle1 = new Vector3(points[i].handle1.x, points[i].handle1.y, 0);
                    points[i].handle2 = new Vector3(points[i].handle2.x, points[i].handle2.y, 0);
                    break;
            }
        }
    }

    public void MirrorAllNodesAroundAxis(Axis axis)
    {
        for (int i = 0; i < points.Length; i++)
        {
            switch (axis)
            {
                case Axis.X:
                    points[i].localPosition = new Vector3(-points[i].localPosition.x, points[i].localPosition.y, points[i].localPosition.z);
                    points[i].handle1 = new Vector3(-points[i].handle1.x, points[i].handle1.y, points[i].handle1.z);
                    break;
                case Axis.Y:
                    points[i].localPosition = new Vector3(points[i].localPosition.x, -points[i].localPosition.y, points[i].localPosition.z);
                    points[i].handle1 = new Vector3(points[i].handle1.x, -points[i].handle1.y, points[i].handle1.z);
                    break;
                case Axis.Z:
                    points[i].localPosition = new Vector3(points[i].localPosition.x, points[i].localPosition.y, -points[i].localPosition.z);
                    points[i].handle1 = new Vector3(points[i].handle1.x, points[i].handle1.y, -points[i].handle1.z);
                    break;
            }
        }
    }

    /// <summary>
    ///     - Adds the given point to the end of the curve ("points" array)
    /// </summary>
    /// <param name='point'>
    ///     - The point to add.
    /// </param>
    public void AddPoint(BezierPoint point)
    {
        List<BezierPoint> tempArray = new List<BezierPoint>(points);
        tempArray.Add(point);
        points = tempArray.ToArray();
        lengthDirty = true;
        interpolationCacheDirty = true;
    }

    public void InsertPoint(int index, BezierPoint point)
    {
        List<BezierPoint> tempArray = new List<BezierPoint>(points);
        tempArray.Insert(index, point);
        points = tempArray.ToArray();
        lengthDirty = true;
        interpolationCacheDirty = true;
    }

    public BezierPoint CreatePointAt(Vector3 position)
    {
        GameObject pointObject = new GameObject("Point " + pointCount);

        pointObject.transform.parent = transform;
        pointObject.transform.position = position;

        BezierPoint newPoint = pointObject.AddComponent<BezierPoint>();
        newPoint._curve = this;

        return newPoint;
    }

    /// <summary>
    ///     - Adds a point at position
    /// </summary>
    /// <returns>
    ///     - The point object
    /// </returns>
    /// <param name='position'>
    ///     - Where to add the point
    /// </param>
    public BezierPoint AddPointAt(Vector3 position)
    {
        BezierPoint newPoint = CreatePointAt(position);

        AddPoint(newPoint);

        return newPoint;
    }

    public BezierPoint AddPointBehind(Vector3 position)
    {
        BezierPoint newPoint = CreatePointAt(position);

        newPoint.transform.SetAsFirstSibling();
        InsertPoint(0, newPoint);

        return newPoint;
    }

    public BezierPoint InsertPointAt(int index, Vector3 position)
    {
        BezierPoint newPoint = CreatePointAt(position);

        newPoint.transform.SetSiblingIndex(index);
        InsertPoint(index, newPoint);

        return newPoint;
    }


    /// <summary>
    ///     - Removes the given point from the curve ("points" array)
    /// </summary>
    /// <param name='point'>
    ///     - The point to remove
    /// </param>
    public void RemovePoint(BezierPoint point)
    {
        List<BezierPoint> tempArray = new List<BezierPoint>(points);
        tempArray.Remove(point);
        points = tempArray.ToArray();
        lengthDirty = false;
        interpolationCacheDirty = true;
    }

    public void RemovePoint(int index)
    {
        List<BezierPoint> tempArray = new List<BezierPoint>(points);
        tempArray.RemoveAt(index);
        points = tempArray.ToArray();
        lengthDirty = false; // i wonder why this is false, not true... -Tostiman
        interpolationCacheDirty = true;
    }

    /// <summary>
    /// To be called when errors start flying.
    /// </summary>
    public void CleanupNullPoints()
    {
        List<BezierPoint> cleanPoints = new List<BezierPoint>();

        foreach (var p in points)
        {
            if (p != null) cleanPoints.Add(p);
        }
        points = cleanPoints.ToArray();
        lengthDirty = false;
    }

    /// <summary>
    ///     - Gets a copy of the bezier point array used to define this curve
    /// </summary>
    /// <returns>
    ///     - The cloned array of points
    /// </returns>
    public BezierPoint[] GetAnchorPoints()
    {
        return (BezierPoint[])points.Clone();
    }


    public BezierPoint Last()
    {
        return this[points.Length - 1];
    }

    struct TBetweenPointsData
    {
        public BezierPoint p1;
        public BezierPoint p2;
        public float t;
    }
    // Helper method for finding a pair of points and a corresponding local 't' for given global 't'
    TBetweenPointsData GetTBetweenPoints(float t)
    {
        if (t <= 0)
        {
            return new TBetweenPointsData { p1 = points[0], p2 = points[1], t = 0 };
        }
        else if (t >= 1)
        {
            if (close)
            {
                return new TBetweenPointsData { p1 = points[points.Length - 1], p2 = points[0], t = 1 };
            }
            else
            {
                return new TBetweenPointsData { p1 = points[points.Length - 2], p2 = points[points.Length - 1], t = 1 };
            }
        }
        else
        {
            float totalPercent = 0;
            float curvePercent = 0;

            BezierPoint p1 = null;
            BezierPoint p2 = null;

            int approxResolution = 10;


            int safetyCounter = 0;
            int maxIters = 10;

            while (p1 == null && p2 == null)
            {
                totalPercent = 0;

                int end = close ? points.Length : points.Length - 1;
                for (int i = 0; i < end; i++)
                {
                    var pa = points[i];
                    var pb = points[(i + 1) % points.Length];
                    curvePercent = ApproximateLength(pa, pb, approxResolution) / length;

                    if ((totalPercent + curvePercent > t) || Mathf.Abs(t - (totalPercent + curvePercent)) < 5e-6)
                    {
                        p1 = pa;
                        p2 = pb;
                        break;
                    }
                    else
                    {
                        totalPercent += curvePercent;
                    }
                }

                approxResolution += 10;

                if (++safetyCounter >= maxIters)
                {
                    Debug.LogError("BezierCurve couldn't find a point", this);
                    return default(TBetweenPointsData);
                }
            }

            if (p1 == null) Debug.LogError("p1 is null");
            if (p2 == null) Debug.LogError("p2 is null");

            var ret = new TBetweenPointsData();
            ret.p1 = p1;
            ret.p2 = p2;
            ret.t = (t - totalPercent) / curvePercent;

            return ret;
        }
    }

    /// <summary>
    ///     - Gets the point at 't' percent along this curve
    /// </summary>
    /// <returns>
    ///     - Returns the point at 't' percent
    /// </returns>
    /// <param name='t'>
    ///     - Value between 0 and 1 representing the percent along the curve (0 = 0%, 1 = 100%)
    /// </param>
    public Vector3 GetPointAt(float t)
    {
        TBetweenPointsData tbp = GetTBetweenPoints(t);
        return GetPoint(tbp.p1, tbp.p2, tbp.t);
    }

    public Vector3 GetTangentAt(float t)
    {
        TBetweenPointsData tbp = GetTBetweenPoints(t);
        return GetTangent(tbp.p1, tbp.p2, tbp.t);
    }

    public Vector3 GetTangent(BezierPoint bp1, BezierPoint bp2, float t)
    {
        if (bp1.handleStyle == BezierPoint.HandleStyle.None &&
            bp2.handleStyle == BezierPoint.HandleStyle.None)
        {
            return (bp2.position - bp1.position).normalized;
        }

        Vector3 a = bp1.position;
        Vector3 b = bp1.globalHandle2;
        Vector3 c = bp2.globalHandle1;
        Vector3 d = bp2.position;

        return Tangent(a, b, c, d, t);
    }

    public Vector3 GetLocalTangent(BezierPoint bp1, BezierPoint bp2, float t)
    {
        if (bp1.handleStyle == BezierPoint.HandleStyle.None &&
            bp2.handleStyle == BezierPoint.HandleStyle.None)
        {
            return (bp2.localPosition - bp1.localPosition).normalized;
        }

        Vector3 a = bp1.localPosition;
        Vector3 b = bp1.localPosition + bp1.handle2;
        Vector3 c = bp2.localPosition + bp2.handle1;
        Vector3 d = bp2.localPosition;

        return Tangent(a, b, c, d, t);
    }

    public static Vector3 Tangent(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float t)
    {
        Vector3 C1 = (d - (3.0f * c) + (3.0f * b) - a);
        Vector3 C2 = ((3.0f * c) - (6.0f * b) + (3.0f * a));
        Vector3 C3 = ((3.0f * b) - (3.0f * a));
        //Vector3 C4 = (a);

        return ((3.0f * C1 * t * t) + (2.0f * C2 * t) + C3);
    }

    /// <summary>
    ///     - Get the index of the given point in this curve
    /// </summary>
    /// <returns>
    ///     - The index, or -1 if the point is not found
    /// </returns>
    /// <param name='point'>
    ///     - Point to search for
    /// </param>
    public int GetPointIndex(BezierPoint point)
    {
        int result = -1;
        for (int i = 0; i < points.Length; i++)
        {
            if (points[i] == point)
            {
                result = i;
                break;
            }
        }

        return result;
    }

    /// <summary>
    ///     - Sets this curve to 'dirty'
    ///     - Forces the curve to recalculate its length
    /// </summary>
    public void SetDirty()
    {
        lengthDirty = true;
        interpolationCacheDirty = true;
    }

    #endregion

    #region PublicStaticFunctions

    /// <summary>
    /// All arguments are global positions. Returns 'numPoints + 1' interpolated points from 'p1' until 'p2', inclusive.
    /// </summary>
    public static Vector3[] Interpolate(Vector3 p1, Vector3 p1Handle2, Vector3 p2, Vector3 p2Handle1, int numPoints)
    {
        var points = new Vector3[numPoints + 1];
        points[0] = p1;
        points[points.Length - 1] = p2;

        float _res = numPoints;

        for (int i = 1; i < points.Length - 1; i++)
        {
            points[i] = GetPoint(p1, p1Handle2, p2, p2Handle1, i / _res);
        }

        return points;
    }

    public static Vector3[] Interpolate(Vector3 p1, Vector3 p1Handle2, Vector3 p2, Vector3 p2Handle1, float resolution)
    {
        int numPoints = GetNumPoints(p1, p1Handle2, p2, p2Handle1, resolution);
        return Interpolate(p1, p1Handle2, p2, p2Handle1, numPoints);
    }


    static void DrawInterpolatedPoint(Vector3 position)
    {
#if UNITY_EDITOR
        if (!drawInterpolatedPoints) return;
        float size = UnityEditor.HandleUtility.GetHandleSize(position);
        float radius = 0.03f;
        var oldColor = Gizmos.color;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(position, size * radius);
        Gizmos.color = oldColor;
#endif
    }


    public static void DrawCurveMirrored(Transform localTransform, BezierPoint p1, BezierPoint p2, int resolution, Axis axis)
    {
        int limit = resolution + 1;
        float _res = resolution;
        Vector3 lastPoint = p1.position;

        // TODO: Not really nice
        lastPoint = localTransform.InverseTransformPoint(lastPoint);
        lastPoint = GetMirroredPoint(lastPoint, axis);
        lastPoint = localTransform.TransformPoint(lastPoint);


        Vector3 currentPoint = Vector3.zero;

        for (int i = 1; i < limit; i++)
        {
            currentPoint = GetPoint(p1, p2, i / _res);

            currentPoint = localTransform.InverseTransformPoint(currentPoint);
            currentPoint = GetMirroredPoint(currentPoint, axis);
            currentPoint = localTransform.TransformPoint(currentPoint);

            Gizmos.DrawLine(lastPoint, currentPoint);
            lastPoint = currentPoint;
        }
    }

    public static void DrawCurveMirrored(Transform localTransform, BezierPoint p1, BezierPoint p2, float resolution, Axis axis)
    {
        DrawCurveMirrored(localTransform, p1, p2, GetNumPoints(p1, p2, resolution), axis);
    }

    public static Vector3 GetMirroredPoint(Vector3 point, Axis axis)
    {
        switch (axis)
        {
            case Axis.X:
                point.x *= -1; break;
            case Axis.Y:
                point.y *= -1; break;
            case Axis.Z:
                point.z *= -1; break;
        }

        return point;
    }

    public static Vector3 GetPoint(BezierPoint p1, BezierPoint p2, float t)
    {
        return GetPoint(p1.position, p1.globalHandle2, p2.position, p2.globalHandle1, t);
    }

    /// <summary>
    /// All arguments are global positions. Returns global position at 't' percent along the curve.
    /// </summary>
    public static Vector3 GetPoint(Vector3 p1, Vector3 p1Handle2, Vector3 p2, Vector3 p2Handle1, float t)
    {
        if (p1Handle2 != p1)
        {
            if (p2Handle1 != p2) return GetCubicCurvePoint(p1, p1Handle2, p2Handle1, p2, t);
            else return GetQuadraticCurvePoint(p1, p1Handle2, p2, t);
        }
        else
        {
            if (p2Handle1 != p2) return GetQuadraticCurvePoint(p1, p2Handle1, p2, t);
            else return GetLinearPoint(p1, p2, t);
        }
    }

    /// <summary>
    ///     - Gets point 't' percent along n-order curve
    /// </summary>
    /// <returns>
    ///     - The point 't' percent along the curve
    /// </returns>
    /// <param name='t'>
    ///     - Value between 0 and 1 representing the percent along the curve (0 = 0%, 1 = 100%)
    /// </param>
    /// <param name='points'>
    ///     - The points used to define the curve
    /// </param>
    public static Vector3 GetPoint(float t, params Vector3[] points)
    {
        t = Mathf.Clamp01(t);

        int order = points.Length - 1;
        Vector3 point = Vector3.zero;
        Vector3 vectorToAdd;

        for (int i = 0; i < points.Length; i++)
        {
            vectorToAdd = points[points.Length - i - 1] * (BinomialCoefficient(i, order) * Mathf.Pow(t, order - i) * Mathf.Pow((1 - t), i));
            point += vectorToAdd;
        }

        return point;
    }

    public static Vector3 GetPointLocal(BezierPoint p1, BezierPoint p2, float t)
    {
        //Vector3 p1H1 = p1.localPosition + p1.handle1;
        Vector3 p1H2 = p1.localPosition + p1.handle2;
        Vector3 p2H1 = p2.localPosition + p2.handle1;
        //Vector3 p2H2 = p2.localPosition + p2.handle2;

        if (p1.handle2 != Vector3.zero)
        {
            if (p2.handle1 != Vector3.zero) return GetCubicCurvePoint(p1.localPosition, p1H2, p2H1, p2.localPosition, t);
            else return GetQuadraticCurvePoint(p1.localPosition, p1H2, p2.localPosition, t);
        }

        else
        {
            if (p2.handle1 != Vector3.zero) return GetQuadraticCurvePoint(p1.localPosition, p2H1, p2.localPosition, t);
            else return GetLinearPoint(p1.localPosition, p2.localPosition, t);
        }
    }

    /// <summary>
    ///     - Gets the point 't' percent along a third-order curve
    /// </summary>
    /// <returns>
    ///     - The point 't' percent along the curve
    /// </returns>
    /// <param name='p1'>
    ///     - The point at the beginning of the curve
    /// </param>
    /// <param name='p2'>
    ///     - The second point along the curve
    /// </param>
    /// <param name='p3'>
    ///     - The third point along the curve
    /// </param>
    /// <param name='p4'>
    ///     - The point at the end of the curve
    /// </param>
    /// <param name='t'>
    ///     - Value between 0 and 1 representing the percent along the curve (0 = 0%, 1 = 100%)
    /// </param>
    public static Vector3 GetCubicCurvePoint(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4, float t)
    {
        t = Mathf.Clamp01(t);

        Vector3 part1 = Mathf.Pow(1 - t, 3) * p1;
        Vector3 part2 = 3 * Mathf.Pow(1 - t, 2) * t * p2;
        Vector3 part3 = 3 * (1 - t) * Mathf.Pow(t, 2) * p3;
        Vector3 part4 = Mathf.Pow(t, 3) * p4;

        return part1 + part2 + part3 + part4;
    }

    /// <summary>
    ///     - Gets the point 't' percent along a second-order curve
    /// </summary>
    /// <returns>
    ///     - The point 't' percent along the curve
    /// </returns>
    /// <param name='p1'>
    ///     - The point at the beginning of the curve
    /// </param>
    /// <param name='p2'>
    ///     - The second point along the curve
    /// </param>
    /// <param name='p3'>
    ///     - The point at the end of the curve
    /// </param>
    /// <param name='t'>
    ///     - Value between 0 and 1 representing the percent along the curve (0 = 0%, 1 = 100%)
    /// </param>
    public static Vector3 GetQuadraticCurvePoint(Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        t = Mathf.Clamp01(t);

        Vector3 part1 = Mathf.Pow(1 - t, 2) * p1;
        Vector3 part2 = 2 * (1 - t) * t * p2;
        Vector3 part3 = Mathf.Pow(t, 2) * p3;

        return part1 + part2 + part3;
    }

    /// <summary>
    ///     - Gets point 't' percent along a linear "curve" (line)
    ///     - This is exactly equivalent to Vector3.Lerp
    /// </summary>
    /// <returns>
    ///             - The point 't' percent along the curve
    /// </returns>
    /// <param name='p1'>
    ///     - The point at the beginning of the line
    /// </param>
    /// <param name='p2'>
    ///     - The point at the end of the line
    /// </param>
    /// <param name='t'>
    ///     - Value between 0 and 1 representing the percent along the line (0 = 0%, 1 = 100%)
    /// </param>
    public static Vector3 GetLinearPoint(Vector3 p1, Vector3 p2, float t)
    {
        return p1 + ((p2 - p1) * t);
    }

    /// <summary>
    ///     - Approximates the length
    /// </summary>
    /// <returns>
    ///     - The approximate length
    /// </returns>
    /// <param name='p1'>
    ///     - The bezier point at the start of the curve
    /// </param>
    /// <param name='p2'>
    ///     - The bezier point at the end of the curve
    /// </param>
    /// <param name='numPoints'>
    ///     - The number of points along the curve used to create measurable segments
    /// </param>
    public static float ApproximateLength(BezierPoint p1, BezierPoint p2, int numPoints = 10)
    {
        return ApproximateLength(p1.position, p1.globalHandle2, p2.position, p2.globalHandle1, numPoints);
    }

    public static float ApproximateLength(Vector3 p1, Vector3 p1Handle2, Vector3 p2, Vector3 p2Handle1, int numPoints = 10)
    {
        float _res = numPoints;
        float total = 0;
        Vector3 lastPosition = p1;
        Vector3 currentPosition;

        for (int i = 0; i < numPoints + 1; i++)
        {
            currentPosition = GetPoint(p1, p1Handle2, p2, p2Handle1, i / _res);
            total += (currentPosition - lastPosition).magnitude;
            lastPosition = currentPosition;
        }

        return total;
    }

    public static float ApproximateLength(BezierPoint p1, BezierPoint p2, float resolution = 0.5f)
    {
        int numPoints = GetNumPoints(p1, p2, resolution);
        return ApproximateLength(p1, p2, numPoints);
    }

    public static float ApproximateLength(Vector3 p1, Vector3 p1Handle2, Vector3 p2, Vector3 p2Handle1, float resolution = 0.5f)
    {
        int numPoints = GetNumPoints(p1, p1Handle2, p2, p2Handle1, resolution);
        return ApproximateLength(p1, p1Handle2, p2, p2Handle1, numPoints);
    }

    /// <summary>
    /// Returns the number of points required to interpolate the given bezier points to a given resolution.
    /// </summary>
    public static int GetNumPoints(BezierPoint p1, BezierPoint p2, float resolution)
    {
        return GetNumPoints(p1.position, p1.globalHandle2, p2.position, p2.globalHandle1, resolution);
    }

    public static int GetNumPoints(Vector3 p1, Vector3 p1Handle2, Vector3 p2, Vector3 p2Handle1, float resolution)
    {
        float length = ApproximateLength(p1, p1Handle2, p2, p2Handle1, RESOLUTION_TO_NUMPOINTS_FACTOR);
        int numPoints = Mathf.RoundToInt(length * resolution);
        return Math.Max(2, numPoints);
    }

    #endregion

    #region UtilityFunctions

    private static int BinomialCoefficient(int i, int n)
    {
        return Factoral(n) / (Factoral(i) * Factoral(n - i));
    }

    private static int Factoral(int i)
    {
        if (i == 0) return 1;

        int total = 1;

        while (i - 1 >= 0)
        {
            total *= i;
            i--;
        }

        return total;
    }

    #endregion


    public void OnBeforeSerialize()
    {
        // nothing here, but ISerializationCallbackReceiver requires it
    }

    public void OnAfterDeserialize()
    {
        // without this, gizmos disappear after rebuilding Mapify
        interpolationCacheDirty = true;
    }
}
