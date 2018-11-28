using UnityEngine;

public class StatusUI : MonoBehaviour
{
    [SerializeField] Message _message;
    [SerializeField] TextMesh _notification;
    [SerializeField] MeshRenderer _flash;

    public Message message { get { return _message; } }

    // internal members

    const float FLASH_DELTA = 0.05f;

    float _flashState = 0.0f;
    float _flashDelta = FLASH_DELTA;

    // public methods

    public void notify(string aNotification)
    {
        _notification.text = aNotification;
    }

    public void clearNotification()
    {
        _notification.text = "";
    }

    public void flash()
    {
        CancelInvoke("FlashUpdate");

        _flashDelta = FLASH_DELTA;
        Invoke("FlashUpdate", Time.deltaTime);
    }

    // overrides

    void Start()
    {
        transform.gameObject.SetActive(false);
        Invoke("Stick", 1);

        _flash.material.SetColor("_EmissionColor", new Color(0.3f * _flashState, 0.1f * _flashState, 0.1f * _flashState));
    }

    // internal methods

    void Stick()
    {
        transform.parent = Camera.main.transform;
        transform.localRotation = Quaternion.Euler(0, 90, 0);
        transform.localPosition = new Vector3(0f, 0.07f, 0.2f);
        transform.gameObject.SetActive(true);
    }

    void FlashUpdate()
    {
        _flashState = Mathf.Max(0f, Mathf.Min(1.0f, _flashState + _flashDelta));
        if (_flashState == 1.0f)
        {
            _flashDelta = -_flashDelta;
        }

        _flash.material.SetColor("_EmissionColor", new Color(0.3f * _flashState, 0.1f * _flashState, 0.1f * _flashState));

        if (_flashState > 0f)
        {
            Invoke("FlashUpdate", Time.deltaTime);
        }
    }
}
