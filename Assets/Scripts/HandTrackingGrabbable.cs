using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandTrackingGrabbable : OVRGrabbable
{
    // private Controller Controller;

    protected override void Start()
    {
        base.Start();
        // Controller = GetComponent<Controller>();
    }

    public override void GrabEnd(Vector3 linearVelocity, Vector3 angularVelocity)
    {
        base.GrabEnd(linearVelocity, angularVelocity);
        // GetComponent<Controller>().OnGrabEnd();
    }

}