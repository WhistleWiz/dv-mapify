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
        [Tooltip("The maximum speed expected at this crossing")]
        public float MaxSpeedAtCrossing = 60.0f;
        [Tooltip("The time taken for all gates to close")]
        public float TimeToActivate = 5.0f;

        private float _activeTime = -1;

        public bool IsLocked => _activeTime >= 0.0f;

        private void FixedUpdate()
        {
            _activeTime -= Time.fixedDeltaTime;

            // Failsafe in case the train never crosses.
            if (_activeTime < 0)
            {
                Unlock();
            }
        }

        private void OnValidate()
        {
            // Max locked time needs to be at least the unlocked time.
            MaxLockedTime = Mathf.Max(MaxLockedTime, UnlockTime);

            float time = 1;
            CrossingGateController[] gates = GetComponentsInChildren<CrossingGateController>();

            // Time to activate is the minimum total time a gate needs to close.
            for (int i = 0; i < gates.Length; i++)
            {
                time = Mathf.Max(gates[i].TotalTimeToClose, time);
            }

            TimeToActivate = Mathf.Max(TimeToActivate, time);
        }

        public void Lock()
        {
            _activeTime = MaxLockedTime;
        }

        public void Unlock()
        {
            // Set it to unlock after a slight delay.
            _activeTime = Mathf.Min(_activeTime, UnlockTime);
        }


#if UNITY_EDITOR
        [Header("Editor Visualization")]
        [SerializeField]
        [Tooltip("This range is the minimum distance needed (in a straight line) for the gates to close " +
            "before a train reaches the crossing")]
        private bool _visualizeMinimumRange = true;

        private void OnDrawGizmos()
        {
            if (_visualizeMinimumRange)
            {
                // Straight line range to be able to close the crossing on time.
                Handles.DrawWireDisc(CentreDetector.transform.position, Vector3.up,
                    TimeToActivate * MaxSpeedAtCrossing);
            }
        }
#endif
    }
}
