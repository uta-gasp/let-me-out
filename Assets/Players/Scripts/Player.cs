using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;
using UnityStandardAssets.Characters.FirstPerson;

public class Player : NetworkBehaviour
{
    // visible in editor

    public GameObject[] avatars = new GameObject[3];
    public float offset = -1.3f;

    // readonly

    public bool isAlive { get { return _health > 0; } }
    public string avatarName { get { return _name; } }

    // internal

    static int _index = 0;
    static Mutex _mutex = new Mutex();

    DebugDesk _debug;                   // external
    GameObject _avatar;                 // instantiated
    // Animator _animator;                 // _avatar internal
    Light _light;                       // child-internal

    FirstPersonController _fpc;         // internal
    HealthStatus _healthStatus;         // external
    StatusUI _statusUI;                  // external

    Logger.LogDomain _log;
    Transform _camera;
    Calibration _calibDisplay;

    ViveController _viveControllerLeft;
    ViveController _viveControllerRight;

    string _name;
    bool _isWalking = false;

    [SyncVar(hook = "OnChangeHealth")]
    float _health = 1f;

    // public methods

    // server-side
    public float decreaseHealth(float aAmount)
    {
        _statusUI.flash();

        _health = Mathf.Max(0f, _health - aAmount);
        _log.add("health", _health.ToString());

        return _health;
    }

    // server-side
    public void respawn()
    {
        _log.add("respawned");

        Invoke("RestoreProps", 1.5f);

        RpcRespawn();
    }

    public void hitsDoor(string aName)
    {
        if (!isServer)
            return;

        _log.add("hits-door", aName);
    }

    public static Player getLocalPlayer()
    {
        var players = FindObjectsOfType<Player>();
        return players.Count() > 0 ? FindObjectsOfType<Player>().Single(player => player.isLocalPlayer) : null;
    }

    // overrides

    void Start()
    {
        _debug = FindObjectOfType<DebugDesk>();

        _light = GetComponentInChildren<Light>();
        _fpc = GetComponent<FirstPersonController>();
        _healthStatus = FindObjectOfType<HealthStatus>();
        _statusUI = FindObjectOfType<StatusUI>();

        int index;
        lock (_mutex)
        {
            index = _index++;
            if (_index >= avatars.Length)
                _index = 0;
        }

        CreateAvatar(index);

        if (!isLocalPlayer)
            return;

        _camera = Camera.main.transform;

        _calibDisplay = FindObjectOfType<Calibration>();
        _calibDisplay.transform.parent = _camera;
        _calibDisplay.transform.localPosition = new Vector3(-0.44f, offset, 0.5f);
        _calibDisplay.transform.localRotation = Quaternion.Euler(0, 0, 0);

        _viveControllerLeft = GameObject.Find("Controller (left)")?.GetComponent<ViveController>();
        _viveControllerRight = GameObject.Find("Controller (right)")?.GetComponent<ViveController>();

        if (_viveControllerLeft)
        {
            _viveControllerLeft.trackpadTouched += OnViveTrackpadTouched;
            _viveControllerLeft.IsTouchingTrackpad += OnViveTouchingTrackpad;
            _viveControllerLeft.pinchToggled += OnVivePinchToggled;
        }

        if (_viveControllerRight)
        {
            _viveControllerRight.trackpadTouched += OnViveTrackpadTouched;
            _viveControllerRight.IsTouchingTrackpad += OnViveTouchingTrackpad;
            _viveControllerRight.pinchToggled += OnVivePinchToggled;
        }

        Setup setup = FindObjectOfType<GameFlow>().setup;
        if (setup.mode == Setup.Mode.HeadGaze)
        {
            FindObjectOfType<Calibration>().hide();
        }
        setup.hide();

        FindObjectOfType<NetworkManagerHUD>().showGUI = false;
    }

    void Update()
    {
        if (!isLocalPlayer)
            return;

        _camera.position = transform.position;
        transform.rotation = _camera.rotation;

        Vector3 fwd = new Vector3(_camera.forward.x, 0, _camera.forward.z);
        transform.forward = fwd.normalized;

        if (!_isWalking && _fpc.isWalking)
        {
            //_animator.SetBool("isWalking", true);
        }
        else if (_isWalking && !_fpc.isWalking)
        {
            //_animator.SetBool("isWalking", false);
        }

        _isWalking = _fpc.isWalking;
    }

    // internal mehtods

    void CreateAvatar(int aIndex)
    {
        _name = $"player-{aIndex}";

        Logger logger = FindObjectOfType<Logger>();
        _log = logger.register("player", aIndex.ToString());

        _avatar = Instantiate(avatars[aIndex]);
        _avatar.transform.parent = transform;
        _avatar.transform.localPosition = new Vector3(0, offset, -0.3f);
        _avatar.transform.localRotation = new Quaternion(0, 0, 0, 0);

        // _animator = _avatar.GetComponent<Animator>();
    }

    // client-side
    void Die()
    {
        if (!isLocalPlayer)
            return;

        _fpc.enabled = false;
        _light.enabled = false;
    }

    [ClientRpc]
    void RpcRespawn()
    {
        if (isLocalPlayer)
        {
            _statusUI.message.show("Respawning...", 2.0f, 0.5f);

            Invoke("RespawnAtOrigin", 1.5f);
            Invoke("EnableAfterRespawn", 3);
        }
        else
        {
            _statusUI.notify("Respawning the partner...");
            Invoke("ClearNotification", 3);
        }
    }

    // server-side
    void RespawnAtOrigin()
    {
        NetworkStartPosition[] spawnPoints = FindObjectsOfType<NetworkStartPosition>().ToArray();
        int randomSpawnPointIndex = (int)Mathf.Round(UnityEngine.Random.Range(0, spawnPoints.Length - 1));
        NetworkStartPosition spawnPoint = spawnPoints[randomSpawnPointIndex];

        transform.position = spawnPoint.transform.position;
    }

    // client-side
    void EnableAfterRespawn()
    {
        _fpc.enabled = true;
        _light.enabled = true;
    }

    // client-side
    void ClearNotification()
    {
        _statusUI.clearNotification();
    }

    // server-side
    void RestoreProps()
    {
        _health = 1f;
        _log.add("health", _health.ToString());
    }

    // callbacks

    void OnChangeHealth(float aValue)
    {
        if (aValue == 0f)
        {
            Die();
        }

        if (isLocalPlayer)
        {
            _healthStatus.value = aValue;
        }
    }

    void OnViveTrackpadTouched(object sender, bool e)
    {
        if (!e)
        {
            _fpc.viveControllerVertAxe = 0f;
            _fpc.viveControllerHorzAxe = 0f;
        }
    }

    void OnViveTouchingTrackpad(object sender, bool e)
    {
        if (e)
        {
            _fpc.viveControllerVertAxe = _viveControllerRight.touchPos.y;
            _fpc.viveControllerHorzAxe = _viveControllerRight.touchPos.x;
        }
    }

    void OnVivePinchToggled(object sender, bool e)
    {
        if (e)
        {
            _fpc.Jump();
        }
    }
}
