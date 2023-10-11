using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Mapify.Editor
{
    public class CrossingGateController : MonoBehaviour
    {
        [Tooltip("The time to close the gate")]
        public float TimeToClose = 3.0f;
        [Tooltip("The angle between the open and closed positions")]
        public float OpenAngle = 85.0f;
        [Tooltip("The rotating part of the gate")]
        public GameObject Gate;

        private float _openPercent = 0.0f;
        private CrossingController _mainController;

        public float TotalTimeToClose => TimeToClose;

        public CrossingController MainController => _mainController ?
            _mainController :
            _mainController = GetComponentInParent<CrossingController>();

        private void Update()
        {
            // Close if locked, open otherwise.
            if (MainController.IsLocked)
            {
                _openPercent -= Time.deltaTime / TimeToClose;
            }
            else
            {
                _openPercent += Time.deltaTime / TimeToClose;
            }

            _openPercent = Mathf.Clamp01(_openPercent);
            Gate.transform.localRotation = Quaternion.Euler(Mathf.Lerp(0, OpenAngle, _openPercent), 0, 0);
        }

        private void OnValidate()
        {
            if (Gate)
            {
                // Display the open position, as the closed one is displayed with the gizmo.
                Gate.transform.localRotation = Quaternion.Euler(OpenAngle, 0, 0);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.black;
            // Closed position.
            Gizmos.DrawLine(transform.position, transform.position - transform.forward * 5);
            // Pivot.
            Gizmos.DrawWireSphere(transform.position, 0.2f);
        }
    }
}
