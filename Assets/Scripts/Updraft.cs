using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AGDDPlatformer
{
    public class Updraft : MonoBehaviour
    {
        public float Force;

        void OnTriggerEnter2D(Collider2D other)
        {
            PlayerController playerController = other.GetComponentInParent<PlayerController>();
            if (playerController != null)
                playerController.SetGravityHard(-Force);
        }

        void OnTriggerExit2D(Collider2D other)
        {
            PlayerController playerController = other.GetComponentInParent<PlayerController>();
            if (playerController != null)
                playerController.ResetGravityHard();
        }
    }
}
