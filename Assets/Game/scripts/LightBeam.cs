using UnityEngine;
using UnityEngine.Networking;

public class LightBeam : NetworkBehaviour
{
    // internal

    DebugDesk _debug;       // external
    Light _light;           // child-internal

    Monster _lastMonsterHit = null;
    string _avatarName;

    // overrides

    void Start()
    {
        _debug = FindObjectOfType<DebugDesk>();

        _light = GetComponentInChildren<Light>();
        _avatarName = GetComponent<Player>().avatarName;
    }

    void Update()
    {
        if (!isServer)
            return;

        RaycastHit hit;
        Physics.Raycast(_light.transform.position, _light.transform.forward, out hit, 20);

        if (hit.collider != null)
        {
            Monster monster = hit.collider.GetComponent<Monster>();
            if (monster && !monster.isActive)
            {
                monster = null;
            }

            bool isSameMonster = _lastMonsterHit == monster;

            if (!isSameMonster && _lastMonsterHit)
            {
                _lastMonsterHit.StopSpotting(_avatarName);
            }

            if (monster)
            {
                monster.Spot(_avatarName, !isSameMonster);
            }

            _lastMonsterHit = monster;
        }
    }
}
