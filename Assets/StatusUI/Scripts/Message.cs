using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Message : MonoBehaviour
{
    TextMesh _text;

    string _invokedMessage;
    float _invokedDuration;

    void Start()
    {
        _text = GetComponentInChildren<TextMesh>();
        gameObject.SetActive(false);
    }

    public void show(string aMessage)
    {
        _text.text = aMessage;
        gameObject.SetActive(true);
    }

    public void show(string aMessage, float aDuration, float aDelay = 0f)
    {
        if (aDelay > 0)
        {
            _invokedMessage = aMessage;
            _invokedDuration = aDuration;
            Invoke("ShowInvoked", aDelay);
        }
        else
        {
            _text.text = aMessage;
            gameObject.SetActive(true);

            if (aDuration > 0)
            {
                Invoke("hide", aDuration);
            }
        }
    }

    public void hide()
    {
        gameObject.SetActive(false);
    }

    private void ShowInvoked()
    {
        _text.text = _invokedMessage;
        gameObject.SetActive(true);

        if (_invokedDuration > 0)
        {
            Invoke("hide", _invokedDuration);
        }
    }
}
