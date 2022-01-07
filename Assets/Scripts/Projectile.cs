using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    private bool collisionDetected = false;
    public bool CollisionDetected { get => collisionDetected; }

    void OnCollisionEnter(Collision collision)
    {
        collisionDetected = true;
    }
}
