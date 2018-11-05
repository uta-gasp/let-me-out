using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sounds : MonoBehaviour
{
    // visible in editor

    public AudioClip getKeySound;

    // internal

    AudioSource _audio;     // internal

    // overrides

    void Start()
    {
        _audio = GetComponent<AudioSource>();
    }

    // public methods

    public void getKey()
    {
        _audio.clip = getKeySound;
        _audio.Play();
    }
}
