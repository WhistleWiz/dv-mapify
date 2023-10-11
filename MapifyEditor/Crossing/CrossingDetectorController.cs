using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Mapify.Editor
{
    [RequireComponent(typeof(TrainDetector))]
    public class CrossingDetectorController : MonoBehaviour
    {
        public bool UnlockOnExit = false;

        private TrainDetector _detector;
        private CrossingController _mainController;

        public CrossingController MainController => _mainController ?
            _mainController :
            _mainController = GetComponentInParent<CrossingController>();

        private void Start()
        {
            _detector = GetComponent<TrainDetector>();
            _detector.OnTrainStay += (x) => { MainController.Lock(); };

            if (UnlockOnExit)
            {
                _detector.OnTrainExit += (x) => { MainController.Unlock(); };
            }
        }
    }
}
