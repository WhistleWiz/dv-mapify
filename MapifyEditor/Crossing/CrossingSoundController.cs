using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Mapify.Editor
{
    [RequireComponent(typeof(AudioSource))]
    public class CrossingSoundController : MonoBehaviour
    {
        public float[] Times;

        private float _lastTime = 0.0f;
        private int _lastIndex = -1;
        private AudioSource _audioSource;
        private CrossingController _mainController;

        public CrossingController MainController => _mainController ?
            _mainController :
            _mainController = GetComponentInParent<CrossingController>();

        private void Start()
        {
            _audioSource = GetComponent<AudioSource>();
        }

        private void Update()
        {
            if (MainController.IsLocked)
            {
                if (Times.Length < 1)
                {
                    Debug.LogWarning($"No times in {name}!");
                    return;
                }

                _lastTime += Time.deltaTime;

                if (_lastIndex == -1 || _lastTime >= Times[_lastIndex])
                {
                    NextClip();
                }
            }
            else
            {
                _lastTime = 0;
                _lastIndex = -1;
            }
        }

        private void NextClip()
        {
            _lastTime = 0;
            _lastIndex = (_lastIndex + 1) % Times.Length;
            _audioSource.Play();
        }
    }
}
