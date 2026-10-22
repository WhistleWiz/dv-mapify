using System.Linq;
using Mapify.Editor.Utils;
using UnityEditor;
using UnityEngine;

namespace Mapify.Editor
{
    [ExecuteInEditMode] // this is necessary for snapping to work
    [RequireComponent(typeof(BezierCurve))]
    public class Track : MonoBehaviour
    {
        public const float SNAP_RANGE = 1.0f;
        public const float SNAP_RANGE_SQUARED = SNAP_RANGE * SNAP_RANGE; // yeah i know, 1x1 = 1. It's futureproofing, okay?
        public const float SNAP_UPDATE_RANGE = 500;
        public const float SNAP_UPDATE_RANGE_SQR = SNAP_UPDATE_RANGE*SNAP_UPDATE_RANGE;
        public const float TURNTABLE_SEARCH_RANGE = 0.05f;

        // ReSharper disable MemberCanBePrivate.Global
        public static readonly Color32 COLOR_ROAD = new Color32(255, 255, 255, 255);
        public static readonly Color32 COLOR_STORAGE = new Color32(172, 134, 101, 255);
        public static readonly Color32 COLOR_LOADING = new Color32(0, 0, 128, 255);
        public static readonly Color32 COLOR_IN = new Color32(50, 240, 50, 255);
        public static readonly Color32 COLOR_OUT = new Color32(106, 90, 205, 255);
        public static readonly Color32 COLOR_PARKING = new Color32(200, 235, 0, 255);
        public static readonly Color32 COLOR_PASSENGER_STORAGE = new Color32(0, 100, 100, 255);
        public static readonly Color32 COLOR_PASSENGER_LOADING = new Color32(0, 255, 255, 255);
        // ReSharper restore MemberCanBePrivate.Global

        [Header("Visuals")]
        [Tooltip("The age of the track. Older tracks are rougher and more rusted, newer tracks are smoother and cleaner")]
        public TrackAge age;
        [Tooltip("Whether speed limit, grade, and marker signs should be generated. Only applies to road tracks")]
        public bool generateSigns = true;
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

        private BezierCurve _curve;

#if UNITY_EDITOR
        [Header("Editor Visualization")]
        [SerializeField]
        private bool showLoadingGauge;

        public bool isInSnapped { get; private set; }
        public bool isOutSnapped { get; private set; }

        [SerializeField] [HideInInspector]
        private SphereCollider frontSnapCollider;
        [SerializeField] [HideInInspector]
        private SphereCollider rearSnapCollider;

        private bool snapShouldUpdate = true;
        private Vector3 previousPositionFirstPoint;
        private Vector3 previousPositionLastPoint;

        // the track connected to the first point in our curve
        private SnappedTrack snappedTrackBefore;
        // the track connected to the last point in our curve
        private SnappedTrack snappedTrackAfter;
#endif

        // The name the track had in the editor. This is useful for debugging since the name gets changed to LogicTrack at runtime.
        [HideInInspector]
        public string InEditorName;

        public BezierCurve Curve {
            get {
                if (_curve != null) return _curve;
                return _curve = GetComponent<BezierCurve>();
            }
        }

        public bool IsSwitch => GetComponentInParent<SwitchBase>() != null;
        public bool IsVanillaSwitch => GetComponentInParent<Switch>() != null;
        public bool IsCustomSwitch => GetComponentInParent<CustomSwitch>() != null;
        public bool IsTurntable => GetComponentInParent<Turntable>() != null;

        public string LogicName =>
            trackType == TrackType.Road
                ? $"{(generateSigns ? "" : "[#] ")}{name}"
                : $"[Y]_[{stationId}]_[{yardId}-{trackId:D2}-{trackType.LetterId()}]";

#if UNITY_EDITOR

        private void Start()
        {
            // necessary for SetupSnapColliders after updating Mapify
            snapShouldUpdate = true;
        }

        private void OnValidate()
        {
            SyncSnapRangeToSnapColliders();
            SyncDrawColorToGizmos();
        }

        private void SyncDrawColorToGizmos()
        {
            if (!isActiveAndEnabled || IsSwitch || IsTurntable)
                return;

            switch (trackType)
            {
                case TrackType.Road:
                    Curve.drawColor = COLOR_ROAD;
                    break;
                case TrackType.Storage:
                    Curve.drawColor = COLOR_STORAGE;
                    break;
                case TrackType.Loading:
                    Curve.drawColor = COLOR_LOADING;
                    break;
                case TrackType.In:
                    Curve.drawColor = COLOR_IN;
                    break;
                case TrackType.Out:
                    Curve.drawColor = COLOR_OUT;
                    break;
                case TrackType.Parking:
                    Curve.drawColor = COLOR_PARKING;
                    break;
                case TrackType.PassengerStorage:
                    Curve.drawColor = COLOR_PASSENGER_STORAGE;
                    break;
                case TrackType.PassengerLoading:
                    Curve.drawColor = COLOR_PASSENGER_LOADING;
                    break;
            }
        }

        private void SyncSnapRangeToSnapColliders()
        {
            if (frontSnapCollider)
            {
                frontSnapCollider.radius = SNAP_RANGE / 2f;
            }
            if (rearSnapCollider)
            {
                rearSnapCollider.radius = SNAP_RANGE / 2f;
            }
        }

        private void SetupSnapColliders()
        {
            if (!frontSnapCollider)
            {
                frontSnapCollider = CreateSnapCollider(_curve[0].gameObject);
            }
            if(!rearSnapCollider)
            {
                rearSnapCollider = CreateSnapCollider(_curve.Last().gameObject);
            }
        }

        public static SphereCollider CreateSnapCollider(GameObject parent)
        {
            var snapCollider = parent.AddComponent<SphereCollider>();
            snapCollider.radius = SNAP_RANGE/2f;
            snapCollider.hideFlags = HideFlags.HideInInspector | HideFlags.DontSaveInBuild;
            return snapCollider;
        }

        private void OnEnable()
        {
            snapShouldUpdate = true;
        }

        private void OnDisable()
        {
            snappedTrackBefore?.UnSnapped();
            snappedTrackAfter?.UnSnapped();
        }

        private void OnDestroy()
        {
            snappedTrackBefore?.UnSnapped();
            snappedTrackAfter?.UnSnapped();
        }

        private void OnDrawGizmos()
        {
            if (showLoadingGauge)
            {
                DrawLoadingGauge();
            }
            if (IsTurntable ||
                (Curve[0].transform.SqrDistanceToSceneCamera() > SNAP_UPDATE_RANGE_SQR && Curve.Last().transform.SqrDistanceToSceneCamera() > SNAP_UPDATE_RANGE_SQR))
            {
                return;
            }

            if (!isInSnapped)
            {
                DrawDisconnectedIcon(Curve[0].position);
            }
            if (!isOutSnapped)
            {
                DrawDisconnectedIcon(Curve.Last().position);
            }

            // switch snapping is done in SwitchBase
            if (!IsSwitch)
            {
                TrySnapTrack();
            }
        }

        internal void TrySnapTrack(bool forceUpdate = false)
        {
            // first or last point moved?
            if (Curve[0].position != previousPositionFirstPoint ||
                Curve.Last().position != previousPositionLastPoint)
            {
                snapShouldUpdate = true;

                previousPositionFirstPoint = Curve[0].position;
                previousPositionLastPoint = Curve.Last().position;
            }

            if (snapShouldUpdate || forceUpdate)
            {
                GameObject[] selectedObjects = Selection.gameObjects;
                bool shouldMove = !IsSwitch && !IsTurntable && (selectedObjects.Contains(gameObject) || selectedObjects.Contains(Curve[0].gameObject) || selectedObjects.Contains(Curve.Last().gameObject));

                SetupSnapColliders();
                TrySnapPoint(true, shouldMove);
                TrySnapPoint(false, shouldMove);

                snapShouldUpdate = false;
            }
        }

        private readonly Collider[] colliderResults = new Collider[10];

        internal void TrySnapPoint(bool first, bool shouldMove)
        {
            var snapCollider = first ? frontSnapCollider : rearSnapCollider;
            var resultCount = Physics.OverlapSphereNonAlloc(snapCollider.transform.position, snapCollider.radius, colliderResults);

            var closest = new SnapCandidate();

            for (int i = 0; i < resultCount; i++)
            {
                var collider = colliderResults[i];

                // tracks
                {
                    var point = collider.GetComponent<BezierPoint>();
                    if (point)
                    {
                        if (point._curve != Curve)
                        {
                            var distanceSquared = Vector3.SqrMagnitude(point.transform.position - snapCollider.transform.position);
                            if (distanceSquared < closest.SquaredDistance)
                            {
                                closest = new SnapCandidate(point, distanceSquared);
                            }
                        }

                        continue;
                    }
                }

                // turntables
                {
                    var turnTable = collider.GetComponentInParent<Turntable>();
                    if (turnTable)
                    {
                        var track = turnTable.Track;
                        var radius = Vector3.Distance(track.Curve[0].position, track.Curve.Last().position) / 2;
                        var directionLocal = track.transform.InverseTransformDirection(snapCollider.transform.position - track.transform.position).normalized;

                        //flatten
                        var vectorLocal = new Vector3(directionLocal.x, 0, directionLocal.z) * radius;
                        var closestPositionOnSnapRing = track.transform.TransformPoint(vectorLocal);

                        var distanceSquared = Vector3.SqrMagnitude(closestPositionOnSnapRing - snapCollider.transform.position);
                        if (distanceSquared <= SNAP_RANGE_SQUARED && distanceSquared < closest.SquaredDistance)
                        {
                            closest = new SnapCandidate(turnTable, distanceSquared, closestPositionOnSnapRing);
                        }

                        continue;
                    }
                }

                // TrackSnappables
                {
                    var snappable = collider.GetComponentInParent<TrackSnappable>();
                    if (snappable)
                    {
                        var distanceSquared = Vector3.SqrMagnitude(snappable.transform.position - snapCollider.transform.position);
                        if (distanceSquared < closest.SquaredDistance)
                        {
                            closest = new SnapCandidate(snappable, distanceSquared);
                        }
                        continue;
                    }
                }
            }

            if (closest.Type == SnapType.None)
            {
                UnSnapPoint(first);
            }
            else {
                SnapPoint(first, closest, shouldMove);
            }
        }

        private void SnapPoint(bool first, SnapCandidate candidate, bool move)
        {
            if (first)
            {
                isInSnapped = true;
            }
            else
            {
                isOutSnapped = true;
            }

            if (candidate.Type == SnapType.Track)
            {
                var otherTrack = candidate.Point.GetTrack();
                otherTrack.Snapped(candidate.Point);

                // remember what track we snapped to
                if (first)
                {
                    snappedTrackBefore = new SnappedTrack(otherTrack, candidate.Point);
                }
                else
                {
                    snappedTrackAfter = new SnappedTrack(otherTrack, candidate.Point);
                }
            }
            else
            {
                // no need to remember because only tracks have the "Disconnected" indicator
                if (first)
                {
                    snappedTrackBefore = null;
                }
                else
                {
                    snappedTrackAfter = null;
                }
            }

            if (move)
            {
                var mySnapPoint = first ? Curve[0] : Curve.Last();
                mySnapPoint.transform.position = candidate.SnapPosition;
                //TODO transform the whole track #40
            }
        }

        private void UnSnapPoint(bool first)
        {
            if (first)
            {
                snappedTrackBefore?.UnSnapped();
                snappedTrackBefore = null;

                isInSnapped = false;
            }
            else
            {
                snappedTrackAfter?.UnSnapped();
                snappedTrackAfter = null;

                isOutSnapped = false;
            }
        }

        private static void DrawDisconnectedIcon(Vector3 position)
        {
            Handles.color = Color.red;
            Handles.Label(position, "Disconnected", EditorStyles.whiteBoldLabel);
            const float size = 0.25f;
            Transform cameraTransform = Camera.current.transform;
            Quaternion rotation = Quaternion.LookRotation(cameraTransform.forward, cameraTransform.up);
            Handles.DrawLine(position - rotation * Vector3.one * size, position + rotation * Vector3.one * size);
            Handles.DrawLine(position - rotation * new Vector3(size, -size, 0f), position + rotation * new Vector3(size, -size, 0f));
        }

        private void DrawLoadingGauge()
        {
            Gizmos.color = Curve.drawColor;
            MapInfo mapInfo = EditorAssets.FindAsset<MapInfo>();
            for (int i = 0; i < Curve.pointCount - 1; ++i)
            {
                BezierPoint p1 = Curve[i];
                BezierPoint p2 = Curve[i + 1];
                int resolution = BezierCurve.GetNumPoints(p1, p2, Curve.resolution);
                Vector3[] vector3Array = BezierCurve.Interpolate(p1.position, p1.globalHandle2, p2.position, p2.globalHandle1, resolution);
                Vector3 from = vector3Array[0];
                for (int index = 1; index < vector3Array.Length; ++index)
                {
                    Vector3 to = vector3Array[index];
                    Vector3 center = Vector3.Lerp(from, to, 0.5f);
                    center.y += mapInfo.loadingGaugeHeight / 2;
                    Vector3 direction = to - from;
                    Quaternion rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
                    Gizmos.matrix = Matrix4x4.TRS(center, rotation, Vector3.one);
                    Gizmos.DrawWireCube(Vector3.zero, new Vector3(mapInfo.loadingGaugeWidth, mapInfo.loadingGaugeHeight, Mathf.Abs(direction.magnitude)));
                    from = to;
                }
            }
        }

        internal void Snapped(BezierPoint point)
        {
            if (point == Curve[0])
                isInSnapped = true;
            if (point == Curve.Last())
                isOutSnapped = true;
        }

        internal void InSnapped()
        {
            isInSnapped = true;
        }

        internal void UnSnapped(BezierPoint point)
        {
            if (point == Curve[0])
                isInSnapped = false;
            if (point == Curve.Last())
                isOutSnapped = false;
        }
#endif

        public static Track Find(string stationId, char yardId, byte trackId, TrackType trackType)
        {
            return FindObjectsOfType<Track>().FirstOrDefault(t => t.stationId == stationId && t.yardId == yardId && t.trackId == trackId && t.trackType == trackType);
        }
    }
}
