using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KeyManager : MonoBehaviour
{
    public Setup.Mode mode;

    void Start()
    {
        gameObject.SetActive( FindObjectOfType<Setup>().mode == mode );
    }

}
