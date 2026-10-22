using System;
using System.Collections.Generic;
using System.Linq;
using Mapify.Editor.Utils;
using UnityEditor;
using UnityEngine;

namespace Mapify.Editor
{
    [ExecuteInEditMode] // this is necessary for snapping to work
    public abstract class SwitchBase: MonoBehaviour
    {
        public abstract Track[] GetTracks();

        public abstract BezierPoint GetJointPoint();
        public abstract BezierPoint[] GetPoints();
        public abstract int GetPointCount();

#if UNITY_EDITOR
        private bool snapShouldUpdate = true;

        private Vector3[] previousPositionsPoints;
        private SnappedTrack[] snappedTracks = Array.Empty<SnappedTrack>();

        [SerializeField] [HideInInspector]
        private SphereCollider[] snapColliders = Array.Empty<SphereCollider>();

        private void OnEnable()
        {
            snapShouldUpdate = true;
        }

        private void OnDisable()
        {
            UnsnapConnectedTracks();
        }

        private void OnDestroy()
        {
            UnsnapConnectedTracks();
        }

        private void Update()
        {
            if (transform.SqrDistanceToSceneCamera() > Track.SNAP_UPDATE_RANGE_SQR)
            {
                return;
            }

            TrySnap();
        }

        private void CheckSwitchMoved()
        {
            var positionPoints = GetPoints().Select(point => point.position).ToArray();

            if (previousPositionsPoints is null || positionPoints.Length != previousPositionsPoints.Length)
            {
                snapShouldUpdate = true;
                previousPositionsPoints = positionPoints;
                return;
            }

            for (int index = 0; index < positionPoints.Length; index++)
            {
                if (positionPoints[index] == previousPositionsPoints[index]) continue;

                snapShouldUpdate = true;
                previousPositionsPoints[index] = positionPoints[index];
            }
        }

        private void UnsnapConnectedTracks()
        {
            foreach (var snapped in snappedTracks)
            {
                snapped?.UnSnapped();
            }
        }

        public void TrySnap()
        {
            CheckSwitchMoved();

            var switchPointsCount = GetPointCount();
            if (snapColliders.Length != switchPointsCount)
            {
                SetupSnapColliders();
                snapShouldUpdate = true;
            }

            if (!snapShouldUpdate) return;

            if (snappedTracks.Length != switchPointsCount)
            {
                snappedTracks = new SnappedTrack[switchPointsCount];
            }

            bool isSelected = Selection.gameObjects.Contains(gameObject);

            var points = GetPoints();
            for (var pointIndex = 0; pointIndex < points.Length; pointIndex++)
            {
                TrySnapPoint(points[pointIndex], pointIndex, isSelected);
            }

            // prevent duplicate "disconnected" text on the join point
            var switchTracks = GetTracks();
            for (int i = 1; i < switchTracks.Length; i++)
            {
                switchTracks[i].InSnapped();
            }

            snapShouldUpdate = false;
        }

        private void SetupSnapColliders()
        {
            foreach (var old in snapColliders)
            {
                DestroyImmediate(old);
            }

            var points = GetPoints();
            snapColliders = new SphereCollider[points.Length];

            for (int i = 0; i < points.Length; i++)
            {
                snapColliders[i] = Track.CreateSnapCollider(points[i].gameObject);
            }
        }

        private readonly Collider[] colliderResults = new Collider[10];

        private void TrySnapPoint(BezierPoint point, int pointIndex, bool shouldMove)
        {
            var snapCollider = snapColliders[pointIndex];

            var resultCount = Physics.OverlapSphereNonAlloc(snapCollider.transform.position, snapCollider.radius, colliderResults);

            var resultsByDistance = colliderResults.Take(resultCount)
                .OrderBy(collider => Vector3.SqrMagnitude(collider.transform.position - point.transform.position))
                .ToArray();

            foreach (var collider in resultsByDistance)
            {
                var foundPoint = collider.GetComponent<BezierPoint>();
                if (!foundPoint) continue;

                var foundTrack = foundPoint.GetComponentInParent<Track>();
                //switches cannot attach directly to other switches
                if (!foundTrack || foundTrack.IsSwitch || foundTrack.IsTurntable) continue;

                SnapToPoint(foundPoint, foundTrack, point, pointIndex, shouldMove);
                return;
            }

            UnSnapPoint(pointIndex, point);
        }

        private void UnSnapPoint(int pointIndex, BezierPoint point)
        {
            if(snappedTracks[pointIndex] is null) { return; }

            snappedTracks[pointIndex].UnSnapped();
            snappedTracks[pointIndex] = null;

            point.GetTrack().UnSnapped(point);
        }

        private void SnapToPoint(BezierPoint otherPoint, Track otherTrack, BezierPoint ownPoint, int ownPointIndex, bool shouldMove)
        {
            if (otherTrack != snappedTracks[ownPointIndex]?.Track)
            {
                UnSnapPoint(ownPointIndex, ownPoint);

                otherTrack.Snapped(otherPoint);
                ownPoint.GetTrack().Snapped(ownPoint);

                snappedTracks[ownPointIndex] = new SnappedTrack(otherTrack, otherPoint);
            }

            if (shouldMove)
            {
                transform.position += otherPoint.position - ownPoint.transform.position;
            }
        }

#endif

    }
}
