using UnityEngine;
using UnityEngine.Networking;

public class LightBeam : NetworkBehaviour
{
    // internal

    DebugDesk _debug;       // external
    Light _light;           // child-internal

    MonsterController _lastMonsterHit = null;
    string _avatarName;

    // overrides

    void Start()
    {
        _debug = FindObjectOfType<DebugDesk>();

        _light = GetComponentInChildren<Light>();
        _avatarName = GetComponent<PlayerAvatar>().avatarName;
    }

    void Update()
    {
        if (!isServer)
            return;

        RaycastHit hit;
        Physics.Raycast(_light.transform.position, _light.transform.forward, out hit, 20);

        if (hit.collider != null)
        {
            MonsterController monster = hit.collider.GetComponent<MonsterController>();
            bool isSameMonster = _lastMonsterHit == monster;

            if (!isSameMonster && _lastMonsterHit)
            {
                _lastMonsterHit.StopSpotting(_avatarName);
            }

            if (monster)
            {
                monster.Spot(_avatarName, isSameMonster);
            }

            _lastMonsterHit = monster;
        }
    }
}
