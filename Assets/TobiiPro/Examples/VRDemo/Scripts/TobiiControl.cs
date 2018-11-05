//-----------------------------------------------------------------------
// Copyright © 2017 Tobii AB. All rights reserved.
//-----------------------------------------------------------------------

using UnityEngine;

namespace Tobii.Research.Unity.Examples
{
    public sealed class ActiveObject
    {
        // The active GameObject.
        public GameObject HighlightedObject;

        // The previous material.
        public Material OriginalObjectMaterial;

        public ActiveObject()
        {
            HighlightedObject = null;
            OriginalObjectMaterial = null;
        }
    }

    public class TobiiControl : MonoBehaviour
    {
        public GameObject _textCalibration;
        public GameObject _textBackground;
        public GameObject _background;

        public event System.EventHandler<bool> onCalibrated = delegate { };


        bool _calibratedSuccessfully = false;

        VREyeTracker _eyeTracker;

        ViveController _viveControllerLeft;
        ViveController _viveControllerRight;

        bool ShowText
        {
            get
            {
                return _textCalibration.activeSelf;
            }

            set
            {
                _textCalibration.SetActive(value);
                _textBackground.SetActive(value);
            }
        }

        bool ShowBoard
        {
            get
            {
                return _background ? _background.activeSelf : false;
            }

            set
            {
                if (_background)
                {
                    _background.SetActive(value);
                }
            }
        }

        void Start()
        {
            _eyeTracker = VREyeTracker.Instance;
            if (_eyeTracker == null)
            {
                Debug.Log("Failed to find eye tracker, has it been added to scene?");
            }

            _viveControllerLeft = GameObject.Find("Controller (left)")?.GetComponent<ViveController>();
            _viveControllerRight = GameObject.Find("Controller (right)")?.GetComponent<ViveController>();

            if (_viveControllerLeft)
            {
                _viveControllerLeft.interactUIToggled += onViveIUIToggled;
            }

            if (_viveControllerRight)
            {
                _viveControllerRight.interactUIToggled += onViveIUIToggled;
            }
        }

        void onViveIUIToggled(object sender, bool pressed)
        {
            if (!pressed)
                return;

            if (!_calibratedSuccessfully)
            {
                if (ShowText)
                {
                    Calibrate();
                }
            }
            else if (!ShowBoard)
            {
                ShowBoard = true;
                Calibrate();
            }
        }

        bool Calibrate()
        {
            if (_eyeTracker.Connected)
            {
                return RunCalibration();
            }

            return false;
        }

        bool RunCalibration()
        {
            if (_eyeTracker.EyeTrackerInterface.UpdateLensConfiguration())
            {
                Debug.Log("Updated lens configuration");
            }

            ShowText = false;

            var calibrationStartResult = VRCalibration.Instance.StartCalibration(
                resultCallback: (calibrationResult) =>
                {
                    _calibratedSuccessfully = calibrationResult;

                    // Show text again if not done.
                    if (!_calibratedSuccessfully)
                    {
                        ShowText = true;
                    }
                    else
                    {
                        ShowBoard = false;
                    }

                    onCalibrated(this, _calibratedSuccessfully);
                });

            return calibrationStartResult;
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1))
            {
                Calibrate();
            }
        }
    }
}