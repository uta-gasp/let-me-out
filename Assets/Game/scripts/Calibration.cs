using UnityEngine;
using Tobii.Research.Unity;

public class Calibration : MonoBehaviour
{
    // public members

    public GameObject _textCalibration;
    public GameObject _textBackground;
    public GameObject _background;

    public event System.EventHandler<bool> onCalibrationStatusChanged = delegate { };
    public event System.EventHandler<bool> onCalibrated = delegate { };

    // internal members

    bool _isCalibrating = false;
    bool _isCalibratedSuccessfully = false;

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

    // public

    public void hide()
    {
        ShowText = false;
        ShowBoard = false;
    }
    // overriden

    void Start()
    {
        _eyeTracker = VREyeTracker.Instance;
        if (_eyeTracker == null)
        {
            Debug.Log("Failed to find VREyeTracker instance, has it been added to scene?");
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

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            Calibrate();
        }
    }

    // internal

    void onViveIUIToggled(object sender, bool pressed)
    {
        if (!pressed)
            return;

        if (!_isCalibratedSuccessfully)
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

        _isCalibrating = VRCalibration.Instance.StartCalibration(
            resultCallback: (calibrationResult) =>
            {
                _isCalibratedSuccessfully = calibrationResult;
                _isCalibrating = false;

                // Show text again if not done.
                if (!_isCalibratedSuccessfully)
                {
                    ShowText = true;
                }
                else
                {
                    ShowBoard = false;
                }
                
                onCalibrated(this, _isCalibratedSuccessfully);
                onCalibrationStatusChanged(this, false);
            });

        if (_isCalibrating)
        {
            onCalibrationStatusChanged(this, true);
        }

        return _isCalibrating;
    }
}