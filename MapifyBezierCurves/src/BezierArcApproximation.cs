using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/*
  Rewritten to C# from the javascript Bezier curve library by Pomax (MIT licensed)
  http://pomax.github.io/bezierinfo
  https://raw.githubusercontent.com/Pomax/bezierjs/gh-pages/lib/bezier.js
  https://raw.githubusercontent.com/Pomax/bezierjs/gh-pages/lib/utils.js
*/

public class BezierArcApproximation : MonoBehaviour
{
    const float MIN_ERROR = 0.1f;

    public struct Arc
    {
        public Vector3 center;
        public float s;
        public float e;
        public float r;
        public float bezierStartT;
        public float bezierEndT;

        public float Length { get { return Mathf.Abs(r * (e - s)); } }
    }

    public float error = 0.5f;
    public BezierCurve curve;

    static Vector3 LineLineIntersection2D(float x1, float y1, float x2, float y2, float x3, float y3, float x4, float y4)
    {

        float nx = (x1 * y2 - y1 * x2) * (x3 - x4) - (x1 - x2) * (x3 * y4 - y3 * x4);
        float ny = (x1 * y2 - y1 * x2) * (y3 - y4) - (y1 - y2) * (x3 * y4 - y3 * x4);
        float d = (x1 - x2) * (y3 - y4) - (y1 - y2) * (x3 - x4);
        return d == 0 ? Vector3.negativeInfinity : new Vector3(nx / d, 0, ny / d);
    }

    /**
     * Given three points, find the (only!) circle
     * that passes through all three points, based
     * on the fact that the perpendiculars of the
     * chords between the points all cross each
     * other at the center of that circle.
     */
    static Arc GetCCenter(Vector3 p1, Vector3 p2, Vector3 p3)
    {
        // deltas
        var dx1 = (p2.x - p1.x);
        var dy1 = (p2.z - p1.z);
        var dx2 = (p3.x - p2.x);
        var dy2 = (p3.z - p2.z);

        // perpendiculars (quarter circle turned)
        const float PiHalf = Mathf.PI / 2;
        var SinPiHalf = Mathf.Sin(PiHalf);
        var CosPiHalf = Mathf.Cos(PiHalf);
        var dx1p = dx1 * CosPiHalf - dy1 * SinPiHalf;
        var dy1p = dx1 * SinPiHalf + dy1 * CosPiHalf;
        var dx2p = dx2 * CosPiHalf - dy2 * SinPiHalf;
        var dy2p = dx2 * SinPiHalf + dy2 * CosPiHalf;

        // chord midpoints
        var mx1 = (p1.x + p2.x) / 2;
        var my1 = (p1.z + p2.z) / 2;
        var mx2 = (p2.x + p3.x) / 2;
        var my2 = (p2.z + p3.z) / 2;

        // midpoint offsets
        var mx1n = mx1 + dx1p;
        var my1n = my1 + dy1p;
        var mx2n = mx2 + dx2p;
        var my2n = my2 + dy2p;

        // intersection of these lines:
        var i = LineLineIntersection2D(mx1, my1, mx1n, my1n, mx2, my2, mx2n, my2n);
        var r = Vector3.Distance(i, p1);

        // arc start/end values, over mid point
        var s = Mathf.Atan2(p1.z - i.z, p1.x - i.x);
        var m = Mathf.Atan2(p2.z - i.z, p2.x - i.x);
        var e = Mathf.Atan2(p3.z - i.z, p3.x - i.x);

        // determine arc direction (cw/ccw correction)
        float z;
        if (s < e)
        {
            if (s > m || m > e) { s += Mathf.PI * 2; }
            if (s > e) { z = e; e = s; s = z; }
        }
        else
        {
            if (e < m && m < s) { z = e; e = s; s = z; } else { e += Mathf.PI * 2; }
        }

        return new Arc { center = new Vector3(i.x, 0, i.z), s = s, e = e, r = r };
    }

    static Vector3 GetPoint2D(BezierCurve bez, float t)
    {
        var p = bez.GetPointAt(t);
        p.y = 0;
        return p;
    }

    public static void CalculateArcs(BezierCurve bez, float errorThreshold, List<Arc> result)
    {
        errorThreshold = Mathf.Max(MIN_ERROR, errorThreshold);

        // start of 'iterate' function in js code
        float t_s = 0;
        float t_e;
        float safety;

        // we do a binary search to find the "good `t` closest to no-longer-good"
        do
        {
            safety = 0;

            // step 1: start with the maximum possible arc
            t_e = 1;

            // points:
            Vector3 np1 = GetPoint2D(bez, t_s);
            Vector3 np2;
            Vector3 np3;
            Arc arc = default(Arc);
            Arc? prev_arc;

            // booleans:
            bool curr_good = false;
            bool prev_good;
            bool done;

            // numbers:
            float t_m;
            float prev_e = 1;
            int step = 0;

            // step 2: find the best possible arc
            do
            {
                prev_good = curr_good;
                prev_arc = arc;
                t_m = (t_s + t_e) / 2;
                step++;

                np2 = GetPoint2D(bez, t_m);
                np3 = GetPoint2D(bez, t_e);

                arc = GetCCenter(np1, np2, np3);

                //also save the t values
                arc.bezierStartT = t_s;
                arc.bezierEndT = t_e;

                var error = Error(bez, arc.center, np1, t_s, t_e);
                curr_good = error <= errorThreshold;

                done = prev_good && !curr_good;
                if (!done) prev_e = t_e;

                // this arc is fine: we can move 'e' up to see if we can find a wider arc
                if (curr_good)
                {
                    // if e is already at max, then we're done for this arc.
                    if (t_e >= 1)
                    {
                        // make sure we cap at t=1
                        arc.bezierEndT = prev_e = 1;
                        prev_arc = arc;
                        // if we capped the arc segment to t=1 we also need to make sure that
                        // the arc's end angle is correct with respect to the bezier end point.
                        if (t_e > 1)
                        {
                            var d = new Vector3(arc.center.x + arc.r * Mathf.Cos(arc.e), 0, arc.center.z + arc.r * Mathf.Sin(arc.e));
                            arc.e += Vector3.Angle(arc.center, d);
                        }
                        break;
                    }
                    // if not, move it up by half the iteration distance
                    t_e += (t_e - t_s) / 2;
                }
                else
                {
                    // this is a bad arc: we need to move 'e' down to find a good arc
                    t_e = t_m;
                }
            }
            while (!done && safety++ < 100);

            if (safety >= 100)
            {
                Debug.Log("Hit safety");
                break;
            }

            prev_arc = prev_arc.HasValue ? prev_arc.Value : arc;

            result.Add(prev_arc.Value);

            t_s = prev_e;

        }
        while (t_e < 1);
        // end of 'iterate' function in js code
    }

    static float Error(BezierCurve bez, Vector3 pc, Vector3 np1, float s, float e)
    {
        var q = (e - s) / 4;
        Vector3 c1 = GetPoint2D(bez, s + q);
        Vector3 c2 = GetPoint2D(bez, e - q);
        var reff = Vector3.Distance(pc, np1);
        var d1 = Vector3.Distance(pc, c1);
        var d2 = Vector3.Distance(pc, c2);
        return Mathf.Abs(d1 - reff) + Mathf.Abs(d2 - reff);
    }

    void OnValidate()
    {
        if (!curve) curve = GetComponent<BezierCurve>();

        error = Mathf.Max(error, MIN_ERROR);

#if UNITY_EDITOR
        arcs = null;  // force new Draw when any of the fields change
#endif
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!curve) return;

        DrawArcs(curve);
    }

    static readonly Color[] colors =
    {
        Color.red,
        Color.green,
        Color.blue,
        Color.yellow,
        Color.magenta,
        Color.cyan,
        Color.gray,
    };


    List<Arc> arcs;
    float lastLength = -1;

    void DrawArcs(BezierCurve bez)
    {
        // performance optimization for large curves - reuse last calculation if curve wasn't changed
        if (arcs == null || lastLength != bez.length)
        {
            lastLength = bez.length;
            arcs = new List<Arc>();
            CalculateArcs(bez, error, arcs);
        }

        for (int i = 0; i < arcs.Count; i++)
        {
            var arc = arcs[i];
            var clr = colors[i % colors.Length];

            // Unity angles go in the different direction than math's,
            // that's why there are negations in 'c' and 'd'
            var a = arc.center;
            var b = Vector2.up;
            var c = Quaternion.Euler(0, -arc.s * Mathf.Rad2Deg, 0) * Vector3.right;
            var d = -(arc.e - arc.s) * Mathf.Rad2Deg;
            var e = arc.r;

            clr.a = 0.2f;
            Handles.color = clr;
            Handles.DrawSolidArc(a, b, c, d, e);
            clr.a = 1;
            Handles.color = clr;
            Handles.DrawWireArc(a, b, c, d, e);

            // text
            if (SceneView.currentDrawingSceneView == null || SceneView.currentDrawingSceneView.camera == null) return;

            var cam = SceneView.currentDrawingSceneView.camera;
            var worldPos = arc.center + Quaternion.Euler(0, -arc.s * Mathf.Rad2Deg + d / 2, 0) * (Vector3.right * arc.r);
            var textPos = cam.ViewportToWorldPoint(cam.WorldToViewportPoint(worldPos));

            GUIStyle labelStyle = new GUIStyle();
            labelStyle.normal.textColor = Handles.color;

            var str = string.Format("R: {0:0.##}m\nL: {1:0.##}m", arc.r, arc.Length);

            Handles.Label(textPos, str, labelStyle);
        }
    }
#endif

}
