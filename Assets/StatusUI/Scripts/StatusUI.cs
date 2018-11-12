using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StatusUI : MonoBehaviour
{
    public Message message;
    public Text notification;

    public void notify(string aNotification)
    {
        notification.text = aNotification;
    }

    public void clearNotification()
    {
        notification.text = "";
    }

    void Start()
    {
        transform.gameObject.SetActive(false);
        Invoke("Stick", 1);
    }

    private void Stick()
    {
        transform.parent = Camera.main.transform;
        transform.localRotation = Quaternion.Euler(0, 90, 0);
        transform.localPosition = new Vector3(0f, 0.1f, 0.2f);
        transform.gameObject.SetActive(true);
    }
}
