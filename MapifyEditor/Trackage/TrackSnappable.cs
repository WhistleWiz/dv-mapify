using System;
using System.Linq;
using Mapify.Editor.Utils;
using UnityEditor;
using UnityEngine;

namespace Mapify.Editor
{
    [ExecuteInEditMode]
    public class TrackSnappable : MonoBehaviour
    {
        [Tooltip("The transform to use as a reference when snapping. Will use self if not set")]
        public Transform referencePoint;

#if UNITY_EDITOR

        [SerializeField] [HideInInspector]
        private SphereCollider snapCollider;
        private SnappedTrack snappedToTrack;
        private readonly Collider[] colliderResults = new Collider[10];
        private bool snapShouldUpdate;

        private void Start()
        {
            snapShouldUpdate = true;
        }

        private void OnDrawGizmos()
        {
            if (transform.hasChanged)
            {
                snapShouldUpdate = true;
                transform.hasChanged = false;
            }

            if (!snapShouldUpdate || transform.SqrDistanceToSceneCamera() > Track.SNAP_UPDATE_RANGE_SQR) return;

            TrySnap();
            snapShouldUpdate = false;
        }

        private void TrySnap()
        {
            SetupSnapCollider();

            var resultCount = Physics.OverlapSphereNonAlloc(snapCollider.transform.position, snapCollider.radius, colliderResults);

            var resultsByDistance = colliderResults.Take(resultCount)
                .OrderBy(collider => Vector3.SqrMagnitude(collider.transform.position - snapCollider.transform.position))
                .ToArray();

            foreach (var collider in resultsByDistance)
            {
                var point = collider.GetComponent<BezierPoint>();
                if (!point) continue;

                var track = point.GetTrack();
                if (!track || track.IsSwitch || track.IsTurntable) continue;

                SnapToPoint(point, track);
                return;
            }

            //not snapped to anything, unsnap if we were snapped to something before
            UnSnap();
        }

        private void UnSnap()
        {
            snappedToTrack?.UnSnapped();
            snappedToTrack = null;
        }

        private void SnapToPoint(BezierPoint point, Track track)
        {
            if (track != snappedToTrack?.Track)
            {
                UnSnap();
                track.Snapped(point);
                snappedToTrack = new SnappedTrack(track, point);
            }

            if (Selection.gameObjects.Contains(gameObject))
            {
                transform.position = point.position + (transform.position - snapCollider.transform.position);
            }
        }

        private void SetupSnapCollider()
        {
            if (!snapCollider)
            {
                snapCollider = Track.CreateSnapCollider(referencePoint.gameObject);
            }
            else if(snapCollider.transform.parent != referencePoint)
            {
                DestroyImmediate(snapCollider);
                snapCollider = Track.CreateSnapCollider(referencePoint.gameObject);
            }
        }

        #endif
    }
}
