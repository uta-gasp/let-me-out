using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityStandardAssets.Characters.FirstPerson;
using Valve.VR;

public class GameFlow : NetworkBehaviour
{
    // visible in editor

    public Rect winArea;
    public KeyStatus KeyStatus;
    public Message message;
    public Light[] winLights;
    public Setup setup;

    public GameObject[] vrObjects;

    // readonly

    public bool isFinished { get { return _isFinished; } }

    // internal 

    const float HEALTH_DECREASE_PER_SECOND = 0.2f;

    DebugDesk _debug;               // external
    Sounds _sounds = null;          // internal
    Logger.LogDomain _log;
    Logger.LogDomain _logErrors;

    bool _isFinished = false;

    // overrides

    void Awake()
    {
        _debug = FindObjectOfType<DebugDesk>();

        Logger logger = FindObjectOfType<Logger>();
        if (logger)
        {
            _log = logger.register("game");
            _logErrors = logger.register("error");
        }

        if (OpenVR.IsHmdPresent())
        {
            UnityEngine.XR.InputTracking.disablePositionalTracking = true;
        }

        foreach (Light light in winLights)
        {
            light.enabled = false;
        }
    }

    public override void OnStartClient()
    {
        _sounds = GetComponent<Sounds>();

        foreach (var obj in vrObjects)
        {
            obj.SetActive(true);
        }

        base.OnStartClient();
    }

    public override void OnStartServer()
    {
        setup.gameObject.SetActive(false);
        Application.logMessageReceived += HandleLog;

        base.OnStartServer();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }

        if (!isServer || _isFinished)
            return;

        // check for win: everybody should be outside

        Transform[] players = FindObjectsOfType<FirstPersonController>().Select(obj => obj.transform).ToArray();
        if (players.Length == 0)
            return;

        bool isSomeoneIsStillInside = false;
        foreach (Transform player in players)
        {
            Vector2 playerPositionOnTheFloor = new Vector2(player.position.x, player.position.z);
            if (!winArea.Contains(playerPositionOnTheFloor))
            {
                isSomeoneIsStillInside = true;
                break;
            }
        }

        if (!isSomeoneIsStillInside)
        {
            _isFinished = true;
            End();
        }
    }

    // public methods

    // server-side
    public void CaptureKey(Key aKey, string aPlayerName)
    {
        string keyName = aKey.name;
        string doorName = aKey.door.name;

        if (_log != null)
            _log.add("open-door", doorName, aPlayerName);

        // on a server
        OpenDoor(aKey.door);
        UpdateKeyStatus(keyName);

        // on clients
        RpcPlayKeyCaptureSound();
        RpcUpdateKeyStatus(keyName);
        RpcOpenDoor(doorName);
    }

    // server-side
    public void HitPlayer(Transform aPlayer, float aWeight)
    {
        if (_isFinished)
            return;

        Player player = aPlayer.gameObject.GetComponent<Player>();
        if (!player.isAlive)
            return;
        
        player.decreaseHealth(HEALTH_DECREASE_PER_SECOND * Time.deltaTime * aWeight);  

        if (!player.isAlive)
        {
            player.respawn();
        }
    }

    // internal methods

    // server-side
    void End()
    {
        if (!isServer)
            return;

        if (_log != null)
            _log.add("finished");

        RpcLightsOn();
        RpcEndGame();
    }

    // client-side
    void ShowCompleteMessage()
    {
        message.show("Completed!");
    }

    void OpenDoor(Door aDoor)
    {
        aDoor.Open();
    }

    void UpdateKeyStatus(string aName)
    {
        KeyStatus.collect(aName);
    }

    void Exit()
    {
        Application.Quit();
    }

    void HandleLog(string logString, string stackTrace, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception)
        {
            if (_logErrors != null)
                _logErrors.add(logString);
            _debug.print($"ERROR: {logString} [{stackTrace}]");
        }
    }

    [ClientRpc]
    void RpcPlayKeyCaptureSound()
    {
        if (_sounds != null)
            _sounds.getKey();
    }

    [ClientRpc]
    void RpcOpenDoor(string aName)
    {
        Door door = FindObjectsOfType<Door>().Single(obj => obj.tag == "door" && obj.name == aName);
        OpenDoor(door);
    }

    [ClientRpc]
    void RpcUpdateKeyStatus(string aName)
    {
        UpdateKeyStatus(aName);
    }

    [ClientRpc]
    void RpcLightsOn()
    {
        if (_log != null)
            _log.add("lights-on");

        foreach (Light light in winLights)
        {
            light.gameObject.SetActive(true);
            light.enabled = true;
        }
    }

    [ClientRpc]
    void RpcEndGame()
    {
        if (_log != null)
            _log.add("finished");

        Invoke("ShowCompleteMessage", 2);
        Invoke("Exit", 4);
    }
}
