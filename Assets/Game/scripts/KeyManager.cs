using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KeyManager : MonoBehaviour
{
    public Setup.Mode mode;

    DebugDesk _debug;

    void Start()
    {
        _debug = FindObjectOfType<DebugDesk>();
    }

    public void Activate(Setup.Mode aMode)
    {
        // _debug.print(aMode.ToString());
        gameObject.SetActive(aMode == mode);
    }
}
