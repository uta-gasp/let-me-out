using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;
using UnityStandardAssets.Characters.FirstPerson;

public class PlayerAvatar : NetworkBehaviour
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
    Animator _animator;                 // _avatar internal
    Light _light;                       // child-internal
    CharacterController _character;     // internal
    FirstPersonController _fpc;         // internal
    HealthStatus _healthStatus;         // external
    Message _message;                   // external

    Logger.LogDomain _log;
    Transform _camera;
    Calibration _calibDisplay;

    ViveController _viveControllerLeft;
    ViveController _viveControllerRight;

    string _name;
    bool _isWalking = false;

    [SyncVar(hook = "onChangeHealth")]
    float _health = 1f;

    float _fallSpeed = 1f;
    Vector3 _fallAxe = new Vector3(0, 0, 1);

    // public methods

    // server-side
    public float decreaseHealth(float aAmount)
    {
        _health = Mathf.Max(0f, _health - aAmount);
        return _health;
    }

    // server-side
    public void respawn()
    {
        Invoke("RespawnAtOrigin", 1.5f);
        Invoke("EnableAfterRespawn", 3);

        RpcRespawn();
    }

    [ClientRpc]
    public void RpcRespawn()
    {
        if (!isLocalPlayer)
            return;

        _message.show("Respawning...", 2.0f, 0.5f);
    }

    /*
    [ClientRpc]
    public void RpcDisable()
    {
        _fpc.enabled = false;
    }*/

    public static PlayerAvatar getLocalPlayer()
    {
        var players = FindObjectsOfType<PlayerAvatar>();
        return players.Count() > 0 ? FindObjectsOfType<PlayerAvatar>().Single(player => player.isLocalPlayer) : null;
    }

    // overrides

    void Start()
    {
        _debug = FindObjectOfType<DebugDesk>();

        _light = GetComponentInChildren<Light>();
        _character = GetComponent<CharacterController>();
        _fpc = GetComponent<FirstPersonController>();
        _healthStatus = FindObjectOfType<HealthStatus>();
        _message = FindObjectOfType<StatusUI>().message;

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
            _viveControllerLeft.trackpadTouched += onViveTrackpadTouched;
            _viveControllerLeft.IsTouchingTrackpad += onViveTouchingTrackpad;
            _viveControllerLeft.pinchToggled += onVivePinchToggled;
        }

        if (_viveControllerRight)
        {
            _viveControllerRight.trackpadTouched += onViveTrackpadTouched;
            _viveControllerRight.IsTouchingTrackpad += onViveTouchingTrackpad;
            _viveControllerRight.pinchToggled += onVivePinchToggled;
        }

        if (FindObjectOfType<Setup>().mode == Setup.Mode.HeadGaze)
        {
            FindObjectOfType<Calibration>().hide();
        }

        FindObjectOfType<NetworkManagerHUD>().showGUI = false;
        FindObjectOfType<Setup>().hide();
    }

    void Update()
    {
        _camera.position = transform.position;
        transform.rotation = _camera.rotation;

        Vector3 fwd = new Vector3(_camera.forward.x, 0, _camera.forward.z);
        transform.forward = fwd.normalized;

        if (!isLocalPlayer)
            return;

        if (!_isWalking && _fpc.isWalking)
        {
            if (_log != null)
                _log.add("move");
            //_animator.SetBool("isWalking", true);
        }
        else if (_isWalking && !_fpc.isWalking)
        {
            if (_log != null)
                _log.add("stop");
            //_animator.SetBool("isWalking", false);
        }

        _isWalking = _fpc.isWalking;
    }

    // internal mehtods

    void CreateAvatar(int aIndex)
    {
        _name = $"player-{aIndex}";

        Logger logger = FindObjectOfType<Logger>();
        if (logger)
            _log = logger.register($"player\t{aIndex}");

        _avatar = Instantiate(avatars[aIndex]);
        _avatar.transform.parent = transform;
        _avatar.transform.localPosition = new Vector3(0, offset, -0.3f);
        _avatar.transform.localRotation = new Quaternion(0, 0, 0, 0);

        _animator = _avatar.GetComponent<Animator>();
    }

    void Die()
    {
        if (_log != null)
            _log.add("dead");

        _fpc.enabled = false;
        _light.enabled = false;

        //Invoke("FallDown", Time.deltaTime);
    }

    /*
    void FallDown()
    {
        if (_character == null)
            return;

        if (_character.transform.eulerAngles.z < 90 || _character.transform.eulerAngles.z > 270)
        {
            _character.transform.Rotate(_fallAxe, _fallSpeed);
            _fallSpeed *= 1.05f;

            Invoke("FallDown", Time.deltaTime);
        }
    }*/

    void RespawnAtOrigin()
    {
        NetworkStartPosition[] spawnPoints = FindObjectsOfType<NetworkStartPosition>().ToArray();
        int randomSpawnPointIndex = (int)Mathf.Round(UnityEngine.Random.Range(0, spawnPoints.Length - 1));
        NetworkStartPosition spawnPoint = spawnPoints[randomSpawnPointIndex];

        transform.position = spawnPoint.transform.position;
    }

    void EnableAfterRespawn()
    {
        _fpc.enabled = true;
        _light.enabled = true;
        _health = 1f;
    }

    private void onViveTrackpadTouched(object sender, bool e)
    {
        if (!e)
        {
            _fpc.viveControllerVertAxe = 0f;
            _fpc.viveControllerHorzAxe = 0f;
        }
    }

    private void onViveTouchingTrackpad(object sender, bool e)
    {
        if (e)
        {
            _fpc.viveControllerVertAxe = _viveControllerRight.touchPos.y;
            _fpc.viveControllerHorzAxe = _viveControllerRight.touchPos.x;
        }
    }

    private void onVivePinchToggled(object sender, bool e)
    {
        if (e)
        {
            _fpc.Jump();
        }
    }

    void onChangeHealth(float aValue)
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
}
