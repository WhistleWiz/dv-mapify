using System.Collections.Generic;
using System.Linq;
using Mapify.Editor.Utils;
using UnityEditor;
using UnityEngine;

namespace Mapify.Editor
{
    [RequireComponent(typeof(BezierCurve))]
    public class Track : MonoBehaviour
    {
        public const float SNAP_RANGE = 1.0f;
        public const float SNAP_UPDATE_RANGE = 500f;

        [Header("Visuals")]
        [Tooltip("The age of the track. Older tracks are rougher and more rusted, newer tracks are smoother and cleaner")]
        public TrackAge age;
        [Tooltip("Whether speed limit, grade, and marker signs should be generated. Only applies to road tracks")]
        public bool generateSigns;
        [Tooltip("Whether ballast is generated for the track. Doesn't apply to switches")]
        public bool generateBallast = true;
        [Tooltip("Whether sleepers and anchors are generated for the track. Doesn't apply to switches")]
        public bool generateSleepers = true;

        [Header("Job Generation")]
        [Tooltip("The ID of the station this track belongs to")]
        public string stationId;
        [Tooltip("The ID of the yard this track belongs to")]
        public char yardId;
        [Tooltip("The numerical ID of this track in it's respective yard")]
        public byte trackId;
        [Tooltip("The purpose of this track")]
        public TrackType trackType;

        public bool isInSnapped { get; private set; }
        public bool isOutSnapped { get; private set; }
        private BezierCurve _curve;

        public BezierCurve Curve {
            get {
                if (_curve != null) return _curve;
                return _curve = GetComponent<BezierCurve>();
            }
        }

        private Switch _parentSwitch;

        private Switch ParentSwitch {
            get {
                if (_parentSwitch) return _parentSwitch;
                return _parentSwitch = GetComponentInParent<Switch>();
            }
        }

        public bool IsSwitch => ParentSwitch != null;
        public bool IsTurntable => GetComponentInParent<Turntable>() != null;

        private void OnValidate()
        {
            if (!isActiveAndEnabled || IsSwitch || IsTurntable)
                return;
            switch (trackType)
            {
                case TrackType.Road:
                    Curve.drawColor = new Color32(255, 255, 255, 255);
                    break;
                case TrackType.Storage:
                    Curve.drawColor = new Color32(255, 127, 80, 255);
                    break;
                case TrackType.Loading:
                    Curve.drawColor = new Color32(0, 0, 128, 255);
                    break;
                case TrackType.In:
                    Curve.drawColor = new Color32(50, 240, 50, 255);
                    break;
                case TrackType.Out:
                    Curve.drawColor = new Color32(106, 90, 205, 255);
                    break;
                case TrackType.Parking:
                    Curve.drawColor = new Color32(200, 235, 0, 255);
                    break;
                case TrackType.PassengerStorage:
                    Curve.drawColor = new Color32(0, 128, 128, 255);
                    break;
                case TrackType.PassengerLoading:
                    Curve.drawColor = new Color32(0, 255, 255, 255);
                    break;
            }
        }

        private void OnDrawGizmos()
        {
            if ((transform.position - Camera.current.transform.position).sqrMagnitude >= SNAP_UPDATE_RANGE * SNAP_UPDATE_RANGE)
                return;
            if (!isInSnapped)
                DrawDisconnectedIcon(Curve[0].position);
            if (!isOutSnapped)
                DrawDisconnectedIcon(Curve.Last().position);
            Snap();
        }

        internal void Snap()
        {
            BezierPoint[] points = FindObjectsOfType<BezierCurve>().SelectMany(curve => new[] { curve[0], curve.Last() }).ToArray();
            GameObject[] selectedObjects = Selection.gameObjects;
            bool isSelected = !IsSwitch && !IsTurntable && (selectedObjects.Contains(gameObject) || selectedObjects.Contains(Curve[0].gameObject) || selectedObjects.Contains(Curve.Last().gameObject));
            TrySnap(points, isSelected, true);
            TrySnap(points, isSelected, false);
        }

        private static void DrawDisconnectedIcon(Vector3 position)
        {
            Handles.color = Color.red;
            Handles.Label(position, "Disconnected", EditorStyles.whiteBoldLabel);
            const float size = 0.25f;
            Transform cameraTransform = Camera.current.transform;
            Vector3 cameraForward = cameraTransform.forward;
            Vector3 cameraUp = cameraTransform.up;
            Quaternion rotation = Quaternion.LookRotation(cameraForward, cameraUp);
            Handles.DrawLine(position - rotation * Vector3.one * size, position + rotation * Vector3.one * size);
            Handles.DrawLine(position - rotation * new Vector3(size, -size, 0f), position + rotation * new Vector3(size, -size, 0f));
        }

        private void TrySnap(IEnumerable<BezierPoint> snapPoints, bool move, bool first)
        {
            if (first) isInSnapped = false;
            else isOutSnapped = false;

            BezierPoint point = first ? Curve[0] : Curve.Last();
            Vector3 pos = point.transform.position;
            Vector3 closestPos = Vector3.zero;
            float closestDist = float.MaxValue;

            Collider[] colliders = new Collider[1];
            // Turntables will search for track within 0.05m, so set it a little lower to be safe.
            if (!IsSwitch && Physics.OverlapSphereNonAlloc(pos, 0.04f, colliders) != 0)
            {
                Collider collider = colliders[0];
                Track track = collider.GetComponent<Track>();
                if (collider is CapsuleCollider capsule && track != null && track.IsTurntable)
                {
                    Vector3 center = capsule.transform.TransformPoint(capsule.center);
                    closestPos = pos + (Vector3.Distance(pos, center) - capsule.radius) * -(pos - center).normalized;
                    closestPos.y = center.y;
                    closestDist = Vector3.Distance(pos, closestPos);
                }
            }

            if (closestDist >= float.MaxValue)
                foreach (BezierPoint otherBp in snapPoints)
                {
                    if (otherBp.Curve() == point.Curve()) continue;
                    Vector3 otherPos = otherBp.transform.position;
                    float dist = Mathf.Abs(Vector3.Distance(otherPos, pos));
                    if (dist > SNAP_RANGE || dist >= closestDist) continue;
                    if (IsSwitch && otherBp.GetComponentInParent<Track>().IsSwitch) continue;
                    closestPos = otherPos;
                    closestDist = dist;
                }

            if (closestDist >= float.MaxValue) return;

            if (first) isInSnapped = true;
            else isOutSnapped = true;
            if (move) point.transform.position = closestPos;
        }

        internal void Snapped(BezierPoint point)
        {
            if (point == Curve[0])
                isInSnapped = true;
            if (point == Curve.Last())
                isOutSnapped = true;
        }

        public static Track Find(string stationId, char yardId, byte trackId, TrackType trackType)
        {
            return FindObjectsOfType<Track>().FirstOrDefault(t => t.stationId == stationId && t.yardId == yardId && t.trackId == trackId && t.trackType == trackType);
        }
    }
}
