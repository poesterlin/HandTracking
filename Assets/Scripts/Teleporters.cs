using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public enum Hand {
    left,
    right,
}

public class TrackingInfo {
    OVRSkeleton rightHand;
    OVRSkeleton leftHand;
    SortedList<string, Bone> fingerBones;
    Hand recognizedHand;
    
    public TrackingInfo(OVRSkeleton right, OVRSkeleton left, SortedList<string, Bone> bones, Hand hand){
        rightHand = right;
        leftHand = left;
        fingerBones = bones;
        recognizedHand = hand;
    }

    public Bone getFinger(int id, Hand hand){
        return fingerBones[id + "-" + hand];
    }

    public Bone getFinger(int id){
        return getFinger(id, recognizedHand);
    }

    public OVRSkeleton getCurrentHand(){
        if(recognizedHand == Hand.right){
            return getRightHand();
        }
        return getLeftHand();
    }

    public OVRSkeleton getRightHand() => rightHand;
    public OVRSkeleton getLeftHand() => leftHand;
}

public class FingerTeleport: Teleporter {
    private Bone anchorF;
    private OVRSkeleton anchorH;

    public FingerTeleport(float distance, LineRenderer line, GameObject targetReticle): base(distance, line, targetReticle) { }

    public override void init(TrackingInfo track){
        anchorF = track.getFinger(7);
        anchorH = track.getCurrentHand();
        base.init(track);
    }

    public override void abort(){
        base.abort();
        anchorF = null;
        anchorH = null;
    }

    public override void update(){
        RaycastHit hit;
        Vector3 start = anchorH.transform.position;
        debugPosition(start);

        if (Physics.Raycast(start, anchorF.Finger.Transform.TransformDirection(Vector3.right), out hit, maxDistance /*, layerMask */)) {
            target = hit.point;
            updateState("avaliable");

            line.SetPosition(0, start);
            line.SetPosition(1, target);

            reticle.activate();
            reticle.transform.position = target;    
            line.enabled = true;
        } else {
           base.update();
        }
    }
}

public class TriangleTeleport: Teleporter {
    TrackingInfo track;

    public TriangleTeleport(float distance, LineRenderer line, GameObject targetReticle): base(distance, line, targetReticle) { }

    public override void init(TrackingInfo trackInfo){
        track = trackInfo;
        base.init(trackInfo);
    }

    public override void abort(){
        base.abort();
    }

    private Plane updatePlane(Hand hand){
        Vector3 index = pos(track.getFinger(20, hand));
        Vector3 thumb = pos(track.getFinger(19, hand));
        Vector3 handBase = pos(track.getFinger(3, hand));
        return new Plane(index, thumb, handBase);
    }

    private Vector3 pos(Bone bone){
        return bone.Finger.Transform.TransformDirection(Vector3.right);
    }

    public override void update(){
        Plane pL = updatePlane(Hand.left);
        Plane pR = updatePlane(Hand.right);

        Vector3 direction = Vector3.Lerp(pL.normal, pR.normal, 0.5f);
        Vector3 origin = Vector3.Lerp(track.getRightHand().transform.position, track.getLeftHand().transform.position, 0.5f);

        RaycastHit hit;
        if (Physics.Raycast(origin, direction, out hit, maxDistance /*, layerMask */)) {
            target = hit.point;
            updateState("avaliable");

            line.SetPosition(0, origin);
            line.SetPosition(1, target);

            reticle.activate();
            reticle.transform.position = target;    
            line.enabled = true;
        } else {
           base.update();
        }
    }
}