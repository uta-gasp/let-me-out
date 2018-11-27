using UnityEngine;
using UnityEngine.Networking;

public class Key : NetworkBehaviour
{
    // visible in editor

    public Door door;
    public float speed = 0.5f;
    public string player;

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

    [ServerCallback]
    void OnTriggerEnter(Collider other)
    {
        if (other.tag != PLAYER_TAG)
            return;

        PlayerAvatar avatar = other.GetComponent<PlayerAvatar>();

        if (FindObjectsOfType<PlayerAvatar>().Length == 1 || avatar.avatarName == player)
        {
            _game.CaptureKey(this);

            Destroy(gameObject);
        }
    }
}
