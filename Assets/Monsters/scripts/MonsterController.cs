using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

[RequireComponent(typeof(AudioSource))]
public class MonsterController : NetworkBehaviour
{
    // visible in editor

    public float sensitivityDistance = 7f;
    public float turnSpeed = 0.5f;
    public float moveSpeed = 0.5f;
    public float sleepTime = 7f;

    public AudioClip wakeUpSound;

    public float hitWeight = 1f;
    public float hitRadius = 0.4f;

    [SerializeField] Transform _healthBar;
    [SerializeField] MeshRenderer _zone;    // another way to set the area... sensitivityArea should then be removed
    [SerializeField] SkinnedMeshRenderer _mainMesh;

    // internal

    const float HEALTH_BAR_SIZE_Y = 0.2f;
    const float HEALTH_BAR_SIZE_XZ = 0.07f;
    const float HEALTH_DECREASE_PER_SECOND = 0.2f;   // health decrease per second
    const float FREEZE_TIME = 10;                   // seconds
    const float TRANSFORM_THRESHOLD = 0.05f;
    const float PAUSE_BEFORE_MOVING_HOME = 8f;
    const float PAUSE_BEFORE_MOVING_HOME_AFTER_FROZEN = 1f;
    const float FLASH_WEIGHT = 0.25f;
    const float FLASH_DELTA = 0.05f;

    DebugDesk _debug;           // external
    Animator _animator;         // internal
    AudioSource _audio;         // internal
    GameFlow _gameFlow;         // external
    Logger.LogDomain _log = null;

    [SyncVar]
    bool _isMonster = false;
    bool _hasPlayedWakeupSound = false;

    [SyncVar(hook = "onChangeHealth")]
    float _health = 1f;

    float _lostPlayerTime = 0;
    Transform _lastPlayerHit = null;
    Rect _sensitivityArea;
    Vector3 _homePoint;
    Quaternion _homeRotation;

    bool _isMovingHome = false;
    bool _isRotatingHome = false;

    float _flashState = 0.0f;
    float _flashDelta = FLASH_DELTA;
    bool _isFlashing = false;

    // overrides

    void Start()
    {
        _debug = FindObjectOfType<DebugDesk>();

        Logger logger = FindObjectOfType<Logger>();
        if (logger)
        {
            _log = logger.register($"monster\t{name}");
        }

        _animator = GetComponent<Animator>();
        _audio = GetComponent<AudioSource>();
        _gameFlow = FindObjectOfType<GameFlow>();

        _healthBar.gameObject.SetActive(false);

        Bounds bounds = _zone.bounds;
        _sensitivityArea = new Rect(bounds.min.x, bounds.min.z, bounds.size.x, bounds.size.z);

        _homePoint = transform.position;
        _homeRotation = transform.rotation;

        _mainMesh.material.EnableKeyword("_EMISSION");
    }

    void Update()
    {
        if (!isServer || _gameFlow.isFinished || _health == 0f)
            return;

        Transform[] players = FindObjectsOfType<Player>().
            Where(player => player.isAlive).
            Select(obj => obj.transform).
            ToArray();

        bool isTouchingPlayer = false;

        Transform nearbyPlayer = GetPlayerToMoveTo(players, ref isTouchingPlayer);

        if (!_isMonster && nearbyPlayer)
        {
            _log.add("wakeup");

            WakeUp();
        }
        else if (_isMonster && !nearbyPlayer)
        {
            if (_lostPlayerTime == 0)
            {
                _lostPlayerTime = Time.time;
            }
            else if (Time.time - _lostPlayerTime < sleepTime)
            {
                _log.add("snooze");
                Snooze();
            }
        }

        if (_log != null && _lastPlayerHit && (!isTouchingPlayer || _lastPlayerHit != nearbyPlayer))
        {
            string playerName = _lastPlayerHit.GetComponent<Player>().avatarName;
            _log.add($"{playerName}\tlost");
        }

        if (_isMonster && nearbyPlayer)
        {
            Vector3 playerAnchor = new Vector3(nearbyPlayer.position.x, transform.position.y, nearbyPlayer.position.z);
            transform.LookAt(Vector3.Slerp(playerAnchor, transform.position + transform.forward, 1f - Time.deltaTime * turnSpeed));
            transform.position = Vector3.MoveTowards(transform.position, playerAnchor, Time.deltaTime * moveSpeed * Mathf.Sqrt(_health));

            if (isTouchingPlayer)
            {
                if (_log != null && _lastPlayerHit != nearbyPlayer)
                {
                    string playerName = nearbyPlayer.GetComponent<Player>().avatarName;
                    _log.add($"{playerName}\thit");
                }

                _gameFlow.HitPlayer(nearbyPlayer, hitWeight);
            }
        }
        else if (_isMovingHome)
        {
            Vector3 currentPos = transform.position;

            transform.LookAt(Vector3.Slerp(_homePoint, transform.position + transform.forward, 1f - Time.deltaTime * turnSpeed));
            transform.position = Vector3.MoveTowards(transform.position, _homePoint, Time.deltaTime * moveSpeed);

            if (Vector3.Distance(_homePoint, transform.position) < TRANSFORM_THRESHOLD)
            {
                _isMovingHome = false;
                _isRotatingHome = true;
            }
        }
        else if (_isRotatingHome)
        {
            Quaternion currentRot = transform.rotation;
            transform.rotation = Quaternion.Slerp(_homeRotation, transform.rotation, 1f - Time.deltaTime * turnSpeed);

            if (Mathf.Abs(Quaternion.Angle(_homeRotation, transform.rotation)) < TRANSFORM_THRESHOLD)
            {
                _isRotatingHome = false;
            }
        }

        _lastPlayerHit = isTouchingPlayer ? nearbyPlayer : null;
    }

    // public methods

    [Server]
    public void Spot(string aPlayerName, bool aContinious)
    {
        if (!_isMonster || _health == 0f)
            return;

        if (!aContinious)
        {
            _log.add($"{aPlayerName}\tgaze-on");
        }

        _health = Mathf.Max(0f, _health - HEALTH_DECREASE_PER_SECOND * Time.deltaTime);

        if (_health == 0f)
        {
            Freeze();
            Invoke("Unfreeze", FREEZE_TIME);
        }
        else if (!_isFlashing)
        {
            _isFlashing = true;
            RpcFlashStart();
        }
    }

    [Server]
    public void StopSpotting(string aPlayerName)
    {
        if (_isMonster)
        {
            _log.add($"{aPlayerName}\tgaze-off");
        }

        if (_isFlashing)
        {
            _isFlashing = false;
            RpcFlashStop();
        }
    }

    // internal methods

    [Server]
    void Freeze()
    {
        _log.add("frozen");
        Snooze();

        if (_isFlashing)
        {
            _isFlashing = false;
            RpcFlashStop();
        }
    }

    [Server]
    void Unfreeze()
    {
        _log.add("alive");
        _health = 1f;

        Invoke("GoHome", PAUSE_BEFORE_MOVING_HOME_AFTER_FROZEN);
    }

    [Server]
    Transform GetPlayerToMoveTo(Transform[] aPlayers, ref bool aIsTouching)
    {
        Vector2 monsterPositionOnTheFloor = new Vector2(transform.position.x, transform.position.z);
        float distToPlayer = float.MaxValue;

        Transform nearbyPlayer = null;

        foreach (var player in aPlayers)
        {
            Vector2 playerPositionOnTheFloor = new Vector2(player.position.x, player.position.z);
            distToPlayer = (playerPositionOnTheFloor - monsterPositionOnTheFloor).magnitude;

            bool isSensiningPlayer = _sensitivityArea.Contains(playerPositionOnTheFloor) && distToPlayer < sensitivityDistance;
            if (isSensiningPlayer)
            {
                nearbyPlayer = player;
                aIsTouching = distToPlayer < hitRadius;
                break;
            }
        }

        return nearbyPlayer;
    }

    [Server]
    void WakeUp()
    {
        _isMonster = true;
        _lostPlayerTime = 0;

        _isMovingHome = false;
        _isRotatingHome = false;
        CancelInvoke("GoHome");

        RpcSetIsCloseToPlayer(true);
    }

    [Server]
    void Snooze()
    {
        _isMonster = false;
        _lostPlayerTime = 0;

        RpcSetIsCloseToPlayer(false);

        Invoke("GoHome", PAUSE_BEFORE_MOVING_HOME);
    }

    [Server]
    void GoHome()
    {
        _isMovingHome = true;
    }

    [ClientRpc]
    void RpcSetIsCloseToPlayer(bool aIsCloseToPlayer)
    {
        if (_log != null)
        {
            if (_health == .0f)
                _log.add("frozen");
            else
                _log.add(aIsCloseToPlayer ? "wakeup" : "snooze");
        }

        _animator.SetBool("IsCloseToPlayer", aIsCloseToPlayer);
        _healthBar.gameObject.SetActive(aIsCloseToPlayer);

        if (aIsCloseToPlayer)
        {
            if (wakeUpSound != null && !_hasPlayedWakeupSound)
            {
                _hasPlayedWakeupSound = true;
                _audio.clip = wakeUpSound;
                _audio.Play();
            }
        }
        else
        {
            _audio.Stop();
        }
    }

    [ClientRpc]
    void RpcFlashStart()
    {
        CancelInvoke("FlashUpdate");

        _isFlashing = true;
        _flashDelta = FLASH_DELTA;
        Invoke("FlashUpdate", Time.deltaTime);
    }

    [ClientRpc]
    void RpcFlashStop()
    {
        CancelInvoke("FlashUpdate");

        _isFlashing = false;
        _flashDelta = -FLASH_DELTA;
        Invoke("FlashUpdate", Time.deltaTime);
    }

    // client-side
    void FlashUpdate()
    {
        _flashState = Mathf.Max(0f, Mathf.Min(1.0f, _flashState + _flashDelta));
        if (_flashState == 1.0f || (_flashState == 0.0f && _isFlashing))
        {
            _flashDelta = -_flashDelta;
        }

        _mainMesh.material.SetColor("_EmissionColor", new Color(
            _flashState * FLASH_WEIGHT, _flashState * FLASH_WEIGHT, _flashState * FLASH_WEIGHT, 1f));

        if (_flashState > 0f || _isFlashing)
        {
            Invoke("FlashUpdate", Time.deltaTime);
        }
    }

    // client-side
    void onChangeHealth(float aValue)
    {
        if (!isClient)
            return;

        _healthBar.localScale = new Vector3(HEALTH_BAR_SIZE_XZ, HEALTH_BAR_SIZE_Y * aValue, HEALTH_BAR_SIZE_XZ);
    }
}
