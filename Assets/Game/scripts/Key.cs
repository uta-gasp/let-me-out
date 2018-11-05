using UnityEngine;
using UnityEngine.Networking;

public class Key : NetworkBehaviour
{
    // visible in editor

    public GameObject door;
    public float speed = 0.5f;

    // internal methods

    static string PLAYER_TAG = "player";

    DebugDesk _debug;       // external
    GameFlow _game;         // external

    // overrides

    void Start()
    {
        _debug = FindObjectOfType<DebugDesk>();

        _game = FindObjectOfType<GameFlow>();
    }

    void Update()
    {
        transform.Rotate(new Vector3(0, 0, Time.deltaTime * 360 * speed));
    }

    void OnTriggerEnter(Collider other)
    {
        if (!isServer)
            return;

        if (other.tag == PLAYER_TAG)
        {
            //_debug.print($"opening door {door.name}", isServer, isLocalPlayer, $"KEY {name}");

            _game.CaptureKey(this);

            Destroy(gameObject);
        }
    }
}
