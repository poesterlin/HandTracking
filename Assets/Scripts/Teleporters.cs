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
    HandCalibrator rightHand;
    HandCalibrator leftHand;
    SortedList<string, Bone> fingerBones;
    Hand recognizedHand;
    Camera headset;

    public TrackingInfo(Camera centerCamera, HandCalibrator right, HandCalibrator left, SortedList<string, Bone> bones, Hand hand)
    {
        rightHand = right;
        leftHand = left;
        fingerBones = bones;
        recognizedHand = hand;
        headset = centerCamera;
    }

    public Bone getFinger(int id, Hand hand)
    {
        if (fingerBones.ContainsKey(id + "-" + hand))
        {
            return fingerBones[id + "-" + hand];
        }
        var fakeBone = new Bone(new OVRBone((OVRSkeleton.BoneId)id, 0, headset.transform), leftHand, true);
        return fakeBone;
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

    public Hand GetRecognizedHand()
    {
        return recognizedHand;
    }

    public Camera getHeadsetCamera()
    {
        return headset;
    }

    public OVRSkeleton getRightHand() => rightHand.skeleton;
    public OVRSkeleton getLeftHand() => leftHand.skeleton;
}

public class FingerTeleport : Teleporter
{
    private Bone anchorF;
    private OVRSkeleton anchorH;

    public FingerTeleport(float distance, ProjectileSimulator sim, LineRenderer line, GameObject targetReticle) : base(distance, sim, line, targetReticle)
    {
        maxIndex = 1;
    }

    public override void Init(TrackingInfo track)
    {
        anchorF = track.getFinger(8);
        anchorH = track.getCurrentHand();
        base.Init(track);
    }

    public override void Abort()
    {
        base.Abort();
        anchorF = null;
        anchorH = null;
    }

    public override void Update()
    {
        Vector3 start = anchorH.transform.position;
        Vector3 dir = anchorF.Finger.Transform.TransformDirection(Vector3.right);
        if (Physics.Raycast(start, dir, out RaycastHit hit, maxDistance, layerMask))
        {
            UpdateTarget(hit.point, 0.3f);

            line.enabled = true;
            line.positionCount = 2;
            line.SetPosition(0, start);
            line.SetPosition(1, target);

            reticle.activate();
            UpdateState(TransporterState.avaliable);
        }
        else if (SimulateProjectile(start, dir))
        {
            line.enabled = true;
            reticle.activate();
            UpdateState(TransporterState.avaliable);
        }
        else
        {
            base.Update();
        }

        if (index == 1 && State >= TransporterState.avaliable && target != Vector3.zero)
        {
            UpdateState(TransporterState.confirmed);
        }
    }
}

public class PalmTeleport : Teleporter
{
    TrackingInfo track;

    public PalmTeleport(float distance, ProjectileSimulator sim, LineRenderer line, GameObject targetReticle) : base(distance, sim, line, targetReticle)

    {
        maxIndex = 1;
    }

    public override void Init(TrackingInfo trackInfo)
    {
        track = trackInfo;
        base.Init(trackInfo);
    }

    public override void Abort()
    {
        base.Abort();
    }

    private Plane updatePlane(Hand hand)
    {
        Vector3 indexF = pos(track.getFinger(20));
        Vector3 handBase = pos(track.getFinger(1));
        Vector3 ringF = pos(track.getFinger(23));
        if (hand == Hand.left)
        {
            return new Plane(indexF, handBase, ringF);
        }
        return new Plane(ringF, indexF, handBase);
    }

    private Vector3 pos(Bone bone)
    {
        return bone.GetTransform().position;
    }

    public override void Update()
    {
        Plane pR = updatePlane(track.GetRecognizedHand());
        Vector3 start = pos(track.getFinger(9));

        if (index == 0)
        {
            if (Physics.Raycast(start, pR.normal, out RaycastHit hit, maxDistance, layerMask))
            {
                UpdateTarget(hit.point);

                line.enabled = true;
                line.positionCount = 2;
                line.SetPosition(0, start);
                line.SetPosition(1, target);

                reticle.activate();
                UpdateState(TransporterState.avaliable);
            }
            else if (SimulateProjectile(start, pR.normal))
            {
                line.enabled = true;
                reticle.activate();
                UpdateState(TransporterState.avaliable);
            }
            else
            {
                base.Update();
            }
        }

        if (index == 1 && State >= TransporterState.avaliable && target != Vector3.zero)
        {
            UpdateState(TransporterState.confirmed);
        }
    }
}


public class TriangleTeleport : Teleporter
{
    TrackingInfo track;

    public TriangleTeleport(float distance, ProjectileSimulator sim, LineRenderer line, GameObject targetReticle) : base(distance, sim, line, targetReticle)
    {
        maxIndex = 1;
    }

    public override void Init(TrackingInfo trackInfo)
    {
        track = trackInfo;
        base.Init(trackInfo);
    }

    public override void Abort()
    {
        base.Abort();
    }

    private Plane updatePlane()
    {
        Vector3 indexL = pos(track.getFinger(20, Hand.left));
        Vector3 indexR = pos(track.getFinger(20, Hand.right));
        Vector3 handBaseL = pos(track.getFinger(3, Hand.left));
        Vector3 handBaseR = pos(track.getFinger(3, Hand.right));
        return new Plane(handBaseL, handBaseR, Vector3.Lerp(indexL, indexR, 0.5f));
    }

    private Vector3 pos(Bone bone)
    {
        return bone.GetTransform().position;
    }

    public override void Update()
    {
        if (index == 0)
        {
            Plane p = updatePlane();

            Vector3 start = Vector3.Lerp(pos(track.getFinger(6, Hand.left)), pos(track.getFinger(6, Hand.right)), 0.5f);

            if (Physics.Raycast(start, p.normal, out RaycastHit hit, maxDistance, layerMask))
            {
                UpdateTarget(hit.point);

                line.enabled = true;
                line.positionCount = 2;
                line.SetPosition(0, start);
                line.SetPosition(1, target);

                reticle.activate();
                UpdateState(TransporterState.avaliable);
            }
            else if (SimulateProjectile(start, p.normal))
            {
                line.enabled = true;
                reticle.activate();
                UpdateState(TransporterState.avaliable);
            }
            else
            {
                base.Update();
            }
        }

        if (index == 1 && State >= TransporterState.avaliable && target != Vector3.zero)
        {
            UpdateState(TransporterState.confirmed);
        }
    }
}