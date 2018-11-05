using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class KeyStatus : MonoBehaviour
{
    public Material collectedMaterial;

    Renderer[] _keys;

    void Start()
    {
        _keys = GetComponentsInChildren<Renderer>().Where(obj => obj.name.StartsWith("key")).ToArray();
    }

    public void collect(string name)
    {
        Renderer key = _keys.Single(k => k.name == name);
        if (key != null)
        {
            key.material = collectedMaterial;
        }
    }
}
