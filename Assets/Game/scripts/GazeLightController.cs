using UnityEngine;
using UnityEngine.Networking;
using Tobii.Research.Unity;

public class GazeLightController : NetworkBehaviour
{
    // internal

    DebugDesk _debug;           // external
    VREyeTracker _eyeTracker;   // singleton
    Camera _camera;
    Light _spotlight;           // child-internal
    Calibration _calibration;

    bool _headGaze;
    int _id;

    // overrides

    void Awake()
    {
        _calibration = FindObjectOfType<Calibration>();
    }

    void Start()
    {
        _debug = FindObjectOfType<DebugDesk>();

        _id = Random.Range(0, int.MaxValue);

        _spotlight = GetComponentInChildren<Light>();

        if (!isLocalPlayer)
            return;

        _eyeTracker = VREyeTracker.Instance;
        _camera = Camera.main;

        _headGaze = FindObjectOfType<GameFlow>().setup.mode == Setup.Mode.HeadGaze;
        if (!_headGaze)
        {
            _calibration.onCalibrationStatusChanged += onCalibrationStatusChanged;
        }
    }

    void Update()
    {
        if (!isLocalPlayer || !_eyeTracker)
            return;

        _spotlight.transform.position = _camera.transform.position;

        var gazeData = _eyeTracker.LatestGazeData;
        if (gazeData.CombinedGazeRayWorldValid && !_headGaze)
        {
            _spotlight.transform.forward = gazeData.CombinedGazeRayWorld.direction;
        }
        else
        {
            _spotlight.transform.forward = _camera.transform.forward;
        }

        CmdReportAngle(_spotlight.transform.localRotation, _id);
    }

    // internal methods


    private void onCalibrationStatusChanged(object sender, bool e)
    {
        Debug.Log($"is calibrating - {e}");
        gameObject.SetActive(!e);
    }

    [Command]
    void CmdReportAngle(Quaternion aAngles, int aClientID)
    {
        _spotlight.transform.localRotation = aAngles;
        RpcUpdateAngle(aAngles, aClientID);
    }

    [ClientRpc]
    void RpcUpdateAngle(Quaternion aAngles, int aClientID)
    {
        if (_id != aClientID && _spotlight)
        {
            _spotlight.transform.localRotation = aAngles;
        }
    }
}
