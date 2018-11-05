using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

[RequireComponent(typeof(AudioSource))]
public class MonsterController : NetworkBehaviour
{
    // visible in editor

    public Rect sensitivityArea;
    public float sensitivityDistance = 7f;
    public float turnSpeed = 0.5f;
    public float moveSpeed = 0.5f;
    public float sleepTime = 7f;

    public AudioClip wakeUpSound;

    public float hitWeight = 1f;
    public float hitRadius = 0.4f;

    // internal

    const float HEALTH_DECREASE_PER_SECOND = 0.2f;   // health decrease per second
    const float FREEZE_TIME = 10;       // seconds

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
    }

    void Update()
    {
        if (!isServer || _gameFlow.isFinished || _health == 0f)
            return;

        Transform[] players = FindObjectsOfType<PlayerAvatar>().
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
            string playerName = _lastPlayerHit.GetComponent<PlayerAvatar>().avatarName;
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
                    string playerName = nearbyPlayer.GetComponent<PlayerAvatar>().avatarName;
                    _log.add($"{playerName}\thit");
                }

                _gameFlow.HitPlayer(nearbyPlayer, hitWeight);
            }
        }

        _lastPlayerHit = isTouchingPlayer ? nearbyPlayer : null;
    }

    // public methods

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
            _log.add("frozen");
            Snooze();

            if (isServer)
            {
                Invoke("Unfreeze", FREEZE_TIME);
            }
        }
    }

    public void StopSpotting(string aPlayerName)
    {
        _log.add($"{aPlayerName}\tgaze-off");
    }

    // internal methods

    void Unfreeze()
    {
        _log.add("alive");
        _health = 1f;
    }

    Transform GetPlayerToMoveTo(Transform[] aPlayers, ref bool aIsTouching)
    {
        Vector2 monsterPositionOnTheFloor = new Vector2(transform.position.x, transform.position.z);
        float distToPlayer = float.MaxValue;

        Transform nearbyPlayer = null;

        foreach (var player in aPlayers)
        {
            Vector2 playerPositionOnTheFloor = new Vector2(player.position.x, player.position.z);
            distToPlayer = (playerPositionOnTheFloor - monsterPositionOnTheFloor).magnitude;

            bool isSensiningPlayer = sensitivityArea.Contains(playerPositionOnTheFloor) && distToPlayer < sensitivityDistance;
            if (isSensiningPlayer)
            {
                nearbyPlayer = player;
                aIsTouching = distToPlayer < hitRadius;
                break;
            }
        }

        return nearbyPlayer;
    }

    void WakeUp()
    {
        _isMonster = true;
        _lostPlayerTime = 0;

        RpcSetIsCloseToPlayer(true);
    }

    void Snooze()
    {
        _isMonster = false;
        _lostPlayerTime = 0;

        RpcSetIsCloseToPlayer(false);
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

    void onChangeHealth(float aValue)
    {
        /*
        if (!isClient)
            return;

        if (_log != null)
            _log.add($"health {_health}");
        */
    }
}
