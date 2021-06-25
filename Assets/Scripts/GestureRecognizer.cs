using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public enum GestureType
{
    FingerGesture = 0,
    TriangleGesture = 1,
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
    public GestureType type = GestureType.FingerGesture;

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
    public List<Gesture> SavedGestures;
    private SortedList<string, Bone> fingerBones;

    private float threshold = 0.04f;
    private float delay = 0.2f;

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
            return;
        }

        Gesture gesture = Recognize();
        bool hasRecognized = gesture != null && !gesture.Equals(defaultGesture);
        if(hasRecognized){
            if(gesture.Equals(previousGestureDetected)){
                // Debug.Log("same gesture");
                tpProv.updateTeleport();
            } else {
                QuestDebug.Instance.Log("found " + gesture.name + " of type " + gesture.type);
                previousGestureDetected = gesture;

                tpProv.abortTeleport();
                tpProv.selectMethod(gesture.type);

                Bone refBoneIndex = fingerBones["7-right"];
                tpProv.initTeleport(skeletonRight, refBoneIndex);
            }
        } else {
            tpProv.abortTeleport();
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

    private Gesture Recognize()
    {
        float minSumDistances = Mathf.Infinity;
        Gesture currentGesture = defaultGesture;

        foreach( var gesture in SavedGestures){
            bool discardGesture = false;
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
                if(distance > threshold){
                    // Debug.Log("distance above threshold: " + distance + " for finger " + storedFinger.id + (storedFinger.isLeftHand ? " (left)" : " (right)"));
                    discardGesture = true;
                    break;
                }

                sumDistances += distance;
            }

            if(!discardGesture && sumDistances < minSumDistances){
                minSumDistances = sumDistances;
                currentGesture = gesture;
            }

        }

        if(currentGesture == previousGestureDetected){
            return currentGesture;
        }

        if (currentGesture != defaultGesture)
        {
            currentGesture.time += Time.deltaTime;

            if (currentGesture.time < delay)
                currentGesture = null;
        }

        return currentGesture;
    }
}







