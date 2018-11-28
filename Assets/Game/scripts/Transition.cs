using System;
using System.Collections.Generic;
using UnityEngine;

public class Transition : MonoBehaviour
{
    [SerializeField] string room;

    public class CollidedEventArgs : EventArgs
    {
        public readonly string room;
        public readonly string player;
        public CollidedEventArgs(string aRoom, string aPlayer)
        {
            room = aRoom;
            player = aPlayer;
        }
    }

    public delegate void CollidedEventHandler(object aSender, CollidedEventArgs aArgs);
    public event CollidedEventHandler Collided = delegate { };

    private void OnTriggerEnter(Collider other)
    {
        Player player = other.GetComponent<Player>();
        if (player)
        {
            Collided(this, new CollidedEventArgs(room, player.avatarName));
        }
    }
}
