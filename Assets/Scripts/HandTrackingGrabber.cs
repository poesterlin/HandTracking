using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandTrackingGrabber : OVRGrabber
{
    private OVRHand m_hand;
    private float pinchThreshold = 0.7f;

    protected override void Start()
    {
        m_parentTransform = gameObject.transform;
        base.Start();
        m_hand = GetComponent<OVRHand>();
    }

    protected override void Awake()
    {
        base.Awake();
        m_anchorOffsetPosition = transform.localPosition;
        m_anchorOffsetRotation = transform.localRotation;
    }

    // Update is called once per frame
    public override void Update()
    {
        base.Update();
        CheckIndexPinch();
    }

    void CheckIndexPinch()
    {
        float pinchStrength = m_hand.GetFingerPinchStrength(OVRHand.HandFinger.Index);

        if (!m_grabbedObj && pinchStrength > pinchThreshold && m_grabCandidates.Count > 0)
        {
            GrabBegin();
        }
        else if (m_grabbedObj && !(pinchStrength > pinchThreshold))
        {
            GrabEnd();
        }
    }
}