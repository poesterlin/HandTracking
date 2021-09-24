using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class HandTrackingGrabbable : OVRGrabbable
{
    public Mortar mortar;
    public float distance = 2f;

    public UnityEvent wasGrabbed = new UnityEvent();
    public UnityEvent wasReleased = new UnityEvent();

    protected override void Start()
    {
        base.Start();
    }

    public override void GrabBegin(OVRGrabber hand, Collider grabPoint)
    {
        base.GrabBegin(hand, grabPoint);
        wasGrabbed.Invoke();
    }

    public override void GrabEnd(Vector3 linearVelocity, Vector3 angularVelocity)
    {
        base.GrabEnd(linearVelocity, angularVelocity);
        var mortarPos = mortar.gameObject.transform.position;

        if (Vector3.Distance(mortarPos, gameObject.transform.position) < distance)
        {
            mortar.AddPotion();
            Destroy(gameObject);
        }
        else
        {
            wasReleased.Invoke();
        }
    }
}