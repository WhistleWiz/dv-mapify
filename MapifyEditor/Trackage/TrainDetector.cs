using System;
using UnityEngine;

namespace Mapify.Editor
{
    [RequireComponent(typeof(TrackSnappable))]
    public class TrainDetector : MonoBehaviour
    {
        [Tooltip("If true, will only detect a train moving in the arrow direction")]
        public bool Directional = true;
        public event Action<Rigidbody> OnTrainEnter;
        public event Action<Rigidbody> OnTrainStay;
        public event Action<Rigidbody> OnTrainExit;

        private void OnTriggerEnter(Collider other)
        {
            // Nothing to invoke, so don't bother testing.
            if (OnTrainEnter == null)
            {
                return;
            }

            if (other.tag == "MainTriggerCollider")
            {
                Rigidbody rb = other.attachedRigidbody;

                // If direction is enabled, check that rigidbody is moving through the controller,
                // and in the direction of the crossing.
                if (rb && (!Directional ||
                    (Directional && Vector3.Dot(rb.velocity, transform.forward) > 0)))
                {
                    OnTrainEnter?.Invoke(rb);
                }
            }
        }

        private void OnTriggerStay(Collider other)
        {
            if (OnTrainStay == null)
            {
                return;
            }

            if (other.tag == "MainTriggerCollider")
            {
                Rigidbody rb = other.attachedRigidbody;

                if (rb && (!Directional ||
                    (Directional && Vector3.Dot(rb.velocity, transform.forward) > 0)))
                {
                    OnTrainStay?.Invoke(rb);
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.tag == "MainTriggerCollider")
            {
                Rigidbody rb = other.attachedRigidbody;

                if (rb && (!Directional ||
                    (Directional && Vector3.Dot(rb.velocity, transform.forward) > 0)))
                {
                    OnTrainExit?.Invoke(rb);
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 pos = transform.position;
            Gizmos.color = new Color(0.4f, 0.9f, 0.6f);

            Gizmos.DrawLine(pos + transform.forward, pos + transform.right);
            Gizmos.DrawLine(pos + transform.forward, pos - transform.right);

            // Only draw arrows in the direction if directional.
            if (!Directional)
            {
                Gizmos.DrawLine(pos - transform.forward, pos + transform.right);
                Gizmos.DrawLine(pos - transform.forward, pos - transform.right);
            }
        }
    }
}
