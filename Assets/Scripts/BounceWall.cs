using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BounceWall : MonoBehaviour
{
    public float JumpForce;

    void OnTriggerEnter2D(Collider2D other)
    {
        Rigidbody2D RB = other.attachedRigidbody;
        RB.velocity = new Vector2(RB.velocity.x, (RB.velocity.y < 0 ? -1 : 1) * JumpForce);
        print("works");
    }
}
