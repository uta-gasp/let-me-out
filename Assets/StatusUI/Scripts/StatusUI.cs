using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StatusUI : MonoBehaviour
{
    public Message message { get { return _message; } }

    Message _message;

    void Start()
    {
        _message = GetComponentInChildren<Message>();

        transform.gameObject.SetActive(false);
        Invoke("Stick", 1);
    }

    private void Stick()
    {
        transform.parent = Camera.main.transform;
        transform.localRotation = Quaternion.Euler(0, 90, 0);
        transform.localPosition = new Vector3(0f, 0.108f, 0.2f);
        transform.gameObject.SetActive(true);
    }
}
