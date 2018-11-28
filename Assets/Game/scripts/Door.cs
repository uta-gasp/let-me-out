using UnityEngine;

public class Door : MonoBehaviour
{
    public bool isOpenedInitially = false;

    void Start()
    {
        if (isOpenedInitially)
            Open();
    }

    public void Open()
    {
        GetComponent<BoxCollider>().isTrigger = true;
        GetComponent<Animator>().SetBool("open", true);
    }

    private void OnTriggerEnter(Collider other)
    {
        Player player = other.GetComponent<Player>();
        if (player)
        {
            player.hitsDoor(name);
        }
    }

}
