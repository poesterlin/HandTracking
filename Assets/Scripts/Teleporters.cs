using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public enum Hand
{
    left,
    right,
}

public enum TransporterState
{
    none,
    aborted,
    avaliable,
    confirmed,
}

public class TrackingInfo
{
    OVRSkeleton rightHand;
    OVRSkeleton leftHand;
    SortedList<string, Bone> fingerBones;
    Hand recognizedHand;
    Camera headset;

    public TrackingInfo(Camera centerCamera, OVRSkeleton right, OVRSkeleton left, SortedList<string, Bone> bones, Hand hand)
    {
        rightHand = right;
        leftHand = left;
        fingerBones = bones;
        recognizedHand = hand;
        headset = centerCamera;
    }

    public Bone getFinger(int id, Hand hand)
    {
        return fingerBones[id + "-" + hand];
    }

    public Bone getFinger(int id)
    {
        return getFinger(id, recognizedHand);
    }

    public OVRSkeleton getCurrentHand()
    {
        if (recognizedHand == Hand.right)
        {
            return getRightHand();
        }
        return getLeftHand();
    }

    public Camera getHeadsetCamera()
    {
        return headset;
    }

    public OVRSkeleton getRightHand() => rightHand;
    public OVRSkeleton getLeftHand() => leftHand;
}

public class FingerTeleport : Teleporter
{
    private Bone anchorF;
    private OVRSkeleton anchorH;

    public FingerTeleport(float distance, LineRenderer line, GameObject targetReticle) : base(distance, line, targetReticle)
    {
        maxIndex = 1;
    }

    public override void init(TrackingInfo track)
    {
        anchorF = track.getFinger(8);
        anchorH = track.getCurrentHand();
        base.init(track);
    }

    public override void abort()
    {
        base.abort();
        anchorF = null;
        anchorH = null;
    }

    public override void update()
    {
        Vector3 start = anchorH.transform.position;

        if (Physics.Raycast(start, anchorF.Finger.Transform.TransformDirection(Vector3.right), out RaycastHit hit, maxDistance, layerMask))
        {
            UpdateTarget(hit.point, 0.3f);

            line.enabled = true;
            line.SetPosition(0, start);
            line.SetPosition(1, target);

            reticle.activate();
            updateState(index == 1 ? TransporterState.confirmed : TransporterState.avaliable);
        }
        else
        {
            base.update();
        }
    }
}

public class PalmTeleport : Teleporter
{
    TrackingInfo track;

    public PalmTeleport(float distance, LineRenderer line, GameObject targetReticle) : base(distance, line, targetReticle)
    {
        maxIndex = 1;
    }

    public override void init(TrackingInfo trackInfo)
    {
        track = trackInfo;
        base.init(trackInfo);
    }

    public override void abort()
    {
        base.abort();
    }

    private Plane updatePlane(Hand hand)
    {
        Vector3 indexF = pos(track.getFinger(20, hand));
        Vector3 handBase = pos(track.getFinger(1, hand));
        Vector3 ringF = pos(track.getFinger(23, hand));
        return new Plane(indexF, handBase, ringF);
    }

    private Vector3 pos(Bone bone)
    {
        return bone.GetTransform().position;
    }

    public override void update()
    {
        Plane pR = updatePlane(Hand.right);
        Vector3 start = pos(track.getFinger(9));

        if (index == 0)
        {
            if (Physics.Raycast(start, pR.normal, out RaycastHit hit, maxDistance, layerMask))
            {
                UpdateTarget(hit.point);

                line.enabled = true;
                line.SetPosition(0, start);
                line.SetPosition(1, target);

                reticle.activate();
                updateState(TransporterState.avaliable);
            }
            else
            {
                base.update();
            }
        }

        if (index == 1 && target != Vector3.zero)
        {
            updateState(TransporterState.confirmed);
        }
    }
}


public class TriangleTeleport : Teleporter
{
    TrackingInfo track;

    public TriangleTeleport(float distance, LineRenderer line, GameObject targetReticle) : base(distance, line, targetReticle)
    {
        maxIndex = 1;
    }

    public override void init(TrackingInfo trackInfo)
    {
        track = trackInfo;
        base.init(trackInfo);
    }

    public override void abort()
    {
        base.abort();
    }

    private Plane updatePlane(Hand hand, int[] fingers)
    {
        Vector3 index = pos(track.getFinger(fingers[0], hand));
        Vector3 thumb = pos(track.getFinger(fingers[1], hand));
        Vector3 handBase = pos(track.getFinger(fingers[2], hand));
        return new Plane(index, thumb, handBase);
    }

    private Vector3 pos(Bone bone)
    {
        return bone.GetTransform().position;
    }

    public override void update()
    {
        if (index == 0)
        {
            Plane pL = updatePlane(Hand.left, new int[] { 19, 20, 3 });
            Plane pR = updatePlane(Hand.right, new int[] { 19, 3, 20 });

            Vector3 start = Vector3.Lerp(pos(track.getFinger(6, Hand.left)), pos(track.getFinger(6, Hand.right)), 0.5f);

            if (Physics.Raycast(start, pR.normal, out RaycastHit hit, maxDistance, layerMask) && Physics.Raycast(start, pL.normal, out RaycastHit hit2, maxDistance, layerMask))
            {
                Vector3 newTarget = Vector3.Lerp(hit.point, hit2.point, 0.5f);
                UpdateTarget(newTarget);

                line.enabled = true;
                line.SetPosition(0, start);
                line.SetPosition(1, target);

                reticle.activate();
                updateState(TransporterState.avaliable);
            }
            else
            {
                base.update();
            }
        }

        if (index == 1 && target != Vector3.zero)
        {
            updateState(TransporterState.confirmed);
        }
    }
}