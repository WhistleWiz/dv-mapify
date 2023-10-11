using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Mapify.Editor
{
    public class CrossingController : MonoBehaviour
    {
        [Tooltip("The maximum time the crossing will stay closed while no train is on it")]
        public float MaxLockedTime = 60.0f;
        [Tooltip("The time it takes to unlock the crossing and start opening after a train has passed")]
        public float UnlockTime = 1.0f;
        [Tooltip("The detector at point the track crosses the road/path")]
        public CrossingDetectorController CentreDetector;

        private float _activeTime = float.PositiveInfinity;

        public bool IsLocked => _activeTime <= MaxLockedTime;

        private void Update()
        {
            _activeTime += Time.deltaTime;

            // Failsafe in case the train never crosses.
            if (_activeTime > MaxLockedTime)
            {
                Unlock();
            }
        }

        public void Lock()
        {
            _activeTime = 0;
        }

        public void Unlock()
        {
            // Set it to unlock after a slight delay.
            _activeTime = Mathf.Max(_activeTime, MaxLockedTime - UnlockTime);
        }


#if UNITY_EDITOR
        [Header("Editor Visualization")]
        [SerializeField]
        [Tooltip("This range is the minimum distance needed (in a straight line) for the gates to close " +
            "before a train reaches the crossing")]
        private bool _visualizeMinimumRange = true;
        [SerializeField]
        [Tooltip("The maximum speed expected at this crossing")]
        private float _maxSpeedAtCrossing = 60.0f;
        [SerializeField]
        [Tooltip("The time taken for all gates to close")]
        private float _timeToActivate = 5.0f;

        // This is only needed in the editor so it's been moved here.
        private void OnValidate()
        {
            float time = 1;
            CrossingGateController[] gates = GetComponentsInChildren<CrossingGateController>();

            // Time to activate is the minimum total time a gate needs to close.
            for (int i = 0; i < gates.Length; i++)
            {
                time = Mathf.Max(gates[i].TotalTimeToClose, time);
            }

            _timeToActivate = Mathf.Max(_timeToActivate, time);
        }

        private void OnDrawGizmos()
        {
            if (_visualizeMinimumRange)
            {
                // Straight line range to be able to close the crossing on time.
                Handles.DrawWireDisc(CentreDetector.transform.position, Vector3.up,
                    _timeToActivate * _maxSpeedAtCrossing);
            }
        }
#endif
    }
}
