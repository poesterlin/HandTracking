using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
#if UNITY_EDITOR
using UnityEditor;
#endif

[Serializable]
public class Gesture
{
    public string name;
    public List<Vector3> fingerData; // Relative to hand
    public UnityEvent onRecognized;
    public bool ignoreLeft = false;
    public bool ignoreRight = false;


    // [HideInInspector]
    // public float time = 0.0f;

    public Gesture(string gestureName)
    {
        name = gestureName;
        fingerData = null;
        onRecognized = new UnityEvent();
    }
}

public class Bone
{
    OVRSkeleton Hand;
    OVRBone Finger;
    public bool isLeftHand;

    public Bone(OVRSkeleton Hand, OVRBone Finger, bool isLeft){
        this.Hand = Hand;
        this.Finger = Finger;
        isLeftHand = isLeft;
    }

    public Vector3 getRelativePosition(){
        return Hand.transform.InverseTransformPoint(Finger.Transform.position);
    }
}


[System.Serializable]
public class GestureRecognizer : MonoBehaviour
{

    public OVRSkeleton skeletonLeft;
    public OVRSkeleton skeletonRight;
    private List<Bone> fingerBones;

    private List<Gesture> savedGestures = new List<Gesture>();
    private float threshold = 0.05f;
    // private float delay = 0.2f;

    [Header("Debugging")]
    public bool debugMode = true;
    private static Gesture defaultGesture = new Gesture("default");
    private Gesture previousGestureDetected = defaultGesture;

    private List<Bone> getBones(OVRSkeleton Hand, bool isLeft){
        List<Bone> list = new List<Bone>();

        foreach (var bone in Hand.Bones)
        {
            list.Add(new Bone(Hand, bone, isLeft));
        }

        return list;
    }

    private void Start()
    {
        fingerBones = getBones(skeletonRight, false);
        fingerBones.AddRange(getBones(skeletonLeft, true));
    }

    private void Update()
    {
        Gesture gestureDetected = Recognize();
        bool hasRecognized = !gestureDetected.Equals(defaultGesture);
        if(hasRecognized && !gestureDetected.Equals(previousGestureDetected)){
            Debug.Log("found " + gestureDetected.name);
            
            gestureDetected.onRecognized.Invoke();
            previousGestureDetected = gestureDetected;
        }

        if(debugMode && Input.GetKeyDown(KeyCode.Space)){
            SaveAsGesture();
        }
    }

    public void SaveAsGesture()
    {
        Gesture g = new Gesture("new gesture");
        List<Vector3> data = new List<Vector3>();
        foreach (var bone in fingerBones)
        {
            data.Add(bone.getRelativePosition());
        }

        g.fingerData = data;
        savedGestures.Add(g);
    }

    private Gesture Recognize()
    {
        float minSumDistances = Mathf.Infinity;
        Gesture currentGesture = defaultGesture;

        foreach( var gesture in savedGestures){
            bool discardGesture = false;
            float sumDistances = 0;
            for (int i = 0; i < fingerBones.Count; i++)
            {   
                var finger = fingerBones[i];

                if(gesture.ignoreLeft && finger.isLeftHand){
                    continue;
                }

                if(gesture.ignoreRight && !finger.isLeftHand){
                    continue;
                }

                Vector3 currentData = finger.getRelativePosition();
                float distance = Vector3.Distance(currentData, gesture.fingerData[i]);
                if(distance > threshold){
                    discardGesture = true;
                    break;
                }
            }

            if(!discardGesture && sumDistances < minSumDistances){
                minSumDistances = sumDistances;
                currentGesture = gesture;
            }

        }

        // if (bestCandidate != null)
        // {
        //     bestCandidate.time += Time.deltaTime;

        //     if (bestCandidate.time < delay)
        //         bestCandidate = null;
        // }

        // If we've found something, we'll return it
        // If we haven't found anything, we return it anyway (newly created object)

        return currentGesture;
    }
}