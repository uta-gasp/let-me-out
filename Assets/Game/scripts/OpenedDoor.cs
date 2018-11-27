using UnityEngine;

public class OpenedDoor : MonoBehaviour
{
    // overrides

    void Start()
    {
        GetComponent<Door>().Open();
    }
}
