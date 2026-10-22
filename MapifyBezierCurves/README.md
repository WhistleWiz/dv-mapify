# BezierCurves

This is based on https://github.com/bgr/BezierCurveEditor which is a fork of [Bezier Curve Editor by Arkham Interactive](https://assetstore.unity.com/packages/tools/bezier-curve-editor-11278) ([archived link](https://web.archive.org/web/20220716121459/https://assetstore.unity.com/packages/tools/bezier-curve-editor-11278)).  
The only difference between this and bgr/BezierCurveEditor (which is the BezierCurves DLL included in DV) is that BezierCurve.OnDrawGizmos is much faster when drawing many curves. At runtime we still use the BezierCurves dll from DV.
