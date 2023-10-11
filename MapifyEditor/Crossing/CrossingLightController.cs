using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Mapify.Editor
{
    public class CrossingLightController : MonoBehaviour
    {
        private CrossingController _mainController;

        public CrossingController MainController => _mainController ?
            _mainController :
            _mainController = GetComponentInParent<CrossingController>();
    }
}
