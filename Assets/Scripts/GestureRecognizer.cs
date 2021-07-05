using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public enum GestureType
{
    Default,
    FingerGesture,
    TriangleGesture,
    PortalGesture,
}


[Serializable]
public class GestureList
{
    public Gesture[] Gestures;
}


[Serializable]
public class Gesture
{
    public string name;
    public List<Bone> fingerData; // Relative to hand
    public UnityEvent onRecognized;
    public bool ignoreLeft = false;
    public bool ignoreRight = false;
    public GestureType type = GestureType.Default;
    public int gestureIndex = 0;

    [HideInInspector]
    public float time = 0.0f;

    public Gesture(string gestureName)
    {
        name = gestureName;
        fingerData = null;
        onRecognized = new UnityEvent();
    }

    public string ToJson()
    {
        return JsonUtility.ToJson(this);
    }

    public static GestureList CreateFromJSON(string jsonString)
    {
        return JsonUtility.FromJson<GestureList>(jsonString);
    }

    public Bone getReference(){
        return fingerData.Find(finger => finger.id == OVRSkeleton.BoneId.Hand_Index2 && !finger.isLeftHand);
    }
}


[Serializable]
public class Bone
{
    [NonSerialized]
    OVRSkeleton Hand;
    [NonSerialized]
    public OVRBone Finger;

    public bool isLeftHand;
    public OVRSkeleton.BoneId id;
    public Vector3 position;

    private Transform cached;

    public Bone(OVRSkeleton Hand, OVRBone OVRFinger, bool isLeft)
    {
        this.Hand = Hand;
        Finger = OVRFinger;
        isLeftHand = isLeft;
        id = Finger.Id;
    }

    public Transform getTransform()
    {
        return Finger.Transform;
    }

    public Vector3 getRelativePosition()
    {
        return Hand.transform.InverseTransformPoint(Finger.Transform.position);
    }

    public Vector3 getAbsolutePosition(){
        return Finger.Transform.position;
    }

    public Bone save(){
        position = getRelativePosition();
        return this;
    }
}

[System.Serializable]
public class GestureRecognizer : MonoBehaviour
{

    public string server = "http://192.168.1.100:3000";
    public OVRSkeleton skeletonLeft;
    public OVRSkeleton skeletonRight;
    public Camera CenterEye;
    public List<Gesture> SavedGestures;

    private SortedList<string, Bone> fingerBones;

    private float threshold = 0.03f;
    private float delay = 0.3f;

    public TeleportProvider tpProv;

    [Header("Debugging")]
    public bool debugMode = true;
    private static Gesture defaultGesture = new Gesture("default");
    private Gesture previousGestureDetected = defaultGesture;

    private NetworkAdapter network;

    private SortedList<string, Bone> getBones(SortedList<string, Bone> list, OVRSkeleton Hand, bool isLeft){
        
        foreach (var bone in Hand.Bones)
        {
            list.Add(((int)bone.Id) + "-" + (isLeft ? "left" : "right"),  new Bone(Hand, bone, isLeft));
        }

        return list;
    }

    private void Start()
    {
        fingerBones = new SortedList<string, Bone>();
        getBones(fingerBones, skeletonRight, false);
        getBones(fingerBones, skeletonLeft, true);

        network = new NetworkAdapter(server);
        StartCoroutine(network.Get(this));
        QuestDebug.Instance.Log("waiting for gestures");
    }

    private void Update()
    {
        if(SavedGestures == null){
            QuestDebug.Instance.Log("no gestures");
            return;
        }

        if(!skeletonLeft.IsDataHighConfidence || !skeletonRight.IsDataHighConfidence){
            tpProv.abortTeleport();
            previousGestureDetected = defaultGesture;
            return;
        }

        Gesture gesture = Recognize();
        bool hasRecognized = gesture != null && !gesture.Equals(defaultGesture);
        if(hasRecognized){
            Debug.Log("types: prev " + previousGestureDetected.type + "  ----  next " + gesture.type);
            if(previousGestureDetected.type == gesture.type){
                if(previousGestureDetected.gestureIndex != gesture.gestureIndex){
                   Debug.Log("update index: " + gesture.gestureIndex);
                }
                tpProv.updateTeleport(gesture.gestureIndex);
            } else {
                previousGestureDetected.time = 0f;
                QuestDebug.Instance.Log("found " + gesture.name + " of type " + gesture.type);

                tpProv.abortTeleport();
                tpProv.selectMethod(gesture.type);
                tpProv.initTeleport(new TrackingInfo(CenterEye, skeletonRight, skeletonLeft, fingerBones, Hand.right));
            }
            previousGestureDetected = gesture;
        } else {
            // only abort if the gesture is detected
            if(gesture.Equals(defaultGesture)){
                tpProv.abortTeleport();
                previousGestureDetected = defaultGesture;
            }
        }

        if(/* debugMode && */ OVRInput.GetDown(OVRInput.Button.Start)){
            QuestDebug.Instance.Log("make a gesture to save in 2 seconds");
            Invoke("SaveAsGesture", 2);
        }
    }

    public void SaveAsGesture()
    {
        Gesture g = new Gesture("new gesture");
        List<Bone> data = new List<Bone>();
        foreach (KeyValuePair<string, Bone> bone in fingerBones)
        {
            data.Add(bone.Value.save());
        }

        g.fingerData = data;
        SavedGestures.Add(g);

        StartCoroutine(network.Post(g.ToJson()));
        QuestDebug.Instance.Log("new gesture saved");
    }

    private Gesture Recognize(){
        float BONUS_DISTANCE = 1f;
        float BONUS_THRESHOLD = 1f;
        float minSumDistances = Mathf.Infinity;
        Gesture currentGesture = defaultGesture;

        // test gestures 
        foreach(var gesture in SavedGestures){
            bool couldBeNext = false;

            bool sameType = gesture.type == previousGestureDetected.type;
            if(sameType && gesture.gestureIndex > previousGestureDetected.gestureIndex){
                couldBeNext = true;
            } else {
                // the gesture is not the first of its type and different to the current
                if(gesture.gestureIndex != 0){
                    continue;
                }
            }

            float dist = distanceBetweenGestures(gesture, couldBeNext ? threshold * BONUS_THRESHOLD : threshold);
            var keepGesture = dist > 0 && dist < Mathf.Infinity;

            if(dist < minSumDistances && keepGesture){
                minSumDistances = couldBeNext ? dist * BONUS_DISTANCE : dist;
                currentGesture = gesture;
            }
        }

        Debug.Log("detected: " + currentGesture.name);
        if(currentGesture.type == previousGestureDetected.type && currentGesture.type != GestureType.Default){
            Debug.Log("same type");
            return currentGesture;
        }

        // set time delay
        currentGesture.time += Time.deltaTime;

        float timeDelay = currentGesture.type == GestureType.Default ? 3f : delay;
        bool needsTime = currentGesture.time < timeDelay;
        if (needsTime){
            Debug.Log("needs time");
            currentGesture = null;
        }
        
        Debug.Log("first recognize");
        return currentGesture;
    }

    private float distanceBetweenGestures(Gesture gesture, float maxFingerDist){
        float sumDistances = 0;
        for (int i = 0; i < fingerBones.Count; i++)
        {   
            var storedFinger = gesture.fingerData[i];
            string key = ((int)storedFinger.id) + "-" + (storedFinger.isLeftHand ? "left" : "right");

            var finger = fingerBones[key];

            if(gesture.ignoreLeft && finger.isLeftHand){
                continue;
            }

            if(gesture.ignoreRight && !finger.isLeftHand){
                continue;
            }

            Vector3 currentData = finger.getRelativePosition();
            float distance = Vector3.Distance(currentData, storedFinger.position);
            if(distance > maxFingerDist){
                gesture.time = 0f;
                Debug.Log("--- abort gesture: " + gesture.name);
                Debug.Log("distance above threshold (max " + maxFingerDist + ") :" + distance + " for finger " + storedFinger.id + (storedFinger.isLeftHand ? " (left)" : " (right)"));
               return Mathf.Infinity;
            }

            sumDistances += distance;
        }

        
        Debug.Log("gesture: " + gesture.name + " dist: " + sumDistances);
        return sumDistances;
    }
}







