using UnityEngine;

public class OpenedDoor : MonoBehaviour
{
    // overrides

    void Start()
    {
        GetComponent<Animator>().SetBool("open", true);
    }
}
