using System.Collections.Generic;
using System.Linq;
using Tobii.Research;
using UnityEngine;

// this module must be ective if VREyeTracker.Instance is used anywhere in the app:
// it keeps the last gaze point up-to-date

public class TobiiPro : MonoBehaviour
{
    // public members

    public IEyeTracker tracker { get { return _eyeTracker; } }

    public event System.EventHandler<HMDGazeDataEventArgs> GazeData = delegate { };

    // private members

    private IEyeTracker _eyeTracker;
    private Queue<HMDGazeDataEventArgs> _queue = new Queue<HMDGazeDataEventArgs>();

    // overriden

    void Awake()
    {
        var trackers = EyeTrackingOperations.FindAllEyeTrackers();

        _eyeTracker = trackers.FirstOrDefault(s => (s.DeviceCapabilities & Capabilities.HasHMDGazeData) != 0);
        if (_eyeTracker == null)
        {
            Debug.Log("No HMD eye tracker detected!");
        }
    }

    void Update()
    {
        PumpGazeData();
    }

    void OnEnable()
    {
        if (_eyeTracker != null)
        {
            _eyeTracker.HMDGazeDataReceived += onGazeData;
        }
    }

    void OnDisable()
    {
        if (_eyeTracker != null)
        {
            _eyeTracker.HMDGazeDataReceived -= onGazeData;
        }
    }

    void OnDestroy()
    {
        EyeTrackingOperations.Terminate();
    }

    // internal

    private HMDGazeDataEventArgs GetNextGazeData()
    {
        lock (_queue)
        {
            return _queue.Count > 0 ? _queue.Dequeue() : null;
        }
    }

    private void PumpGazeData()
    {
        var next = GetNextGazeData();
        while (next != null)
        {
            GazeData(this, next);
            next = GetNextGazeData();
        }
    }

    // This method will be called on a thread belonging to the SDK, and can not safely change values
    // that will be read from the main thread.
    private void onGazeData(object sender, HMDGazeDataEventArgs e)
    {
        lock (_queue)
        {
            _queue.Enqueue(e);
        }
    }
}
