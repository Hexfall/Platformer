using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace AGDDPlatformer
{
    public class JumpWall : MonoBehaviour
    {
        void OnTriggerEnter2D(Collider2D other)
        {
            PlayerController playerController = other.GetComponentInParent<PlayerController>();
            if (playerController != null)
            {
                playerController.ResetDash();
                playerController.Stick();
            }
        }
        
        void OnTriggerExit2D(Collider2D other)
        {
            PlayerController playerController = other.GetComponentInParent<PlayerController>();
            if (playerController != null)
                playerController.Unstick();
        }
    }
}