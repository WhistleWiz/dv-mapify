using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Mapify.Editor
{
    [RequireComponent(typeof(AudioSource))]
    public class CrossingSoundController : MonoBehaviour
    {
        private AudioSource _audioSource;

        private CrossingController _mainController;

        public CrossingController MainController => _mainController ?
            _mainController :
            _mainController = GetComponentInParent<CrossingController>();

        private void Start()
        {
            _audioSource = GetComponent<AudioSource>();
        }
    }
}
