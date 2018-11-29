using UnityEngine;

public class Door : MonoBehaviour
{
    public bool isOpenedInitially = false;

    Logger.LogDomain _log;

    void Start()
    {
        if (isOpenedInitially)
            Open();

        foreach (Transition tr in GetComponentsInChildren<Transition>())
        {
            tr.Collided += OnTransitionCollided;
        }

        Logger logger = FindObjectOfType<Logger>();
        _log = logger.register("door", name.ToString());
    }

    private void OnTransitionCollided(object aSender, Transition.CollidedEventArgs aArgs)
    {
        _log.add("collided", aArgs.room, aArgs.player);
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
