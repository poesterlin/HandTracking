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
    PalmGesture,
    TriangleGesture,
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

    public Vector3 handPosLeft = Vector3.zero;
    public Vector3 handPosRight = Vector3.zero;

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

    public Bone getReference()
    {
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

    public Vector3 getAbsolutePosition()
    {
        return Finger.Transform.position;
    }

    public Bone save()
    {
        position = getRelativePosition();
        return this;
    }
}

[System.Serializable]
public class GestureRecognizer : MonoBehaviour
{

    public OVRSkeleton skeletonLeft;
    public OVRSkeleton skeletonRight;
    public Camera CenterEye;
    public List<Gesture> SavedGestures;

    private SortedList<string, Bone> fingerBones;

    public float threshold = 0.03f;
    public float delay = 0.3f;

    public float maxHandPosDist = 0.5f;

    public TeleportProvider tpProv;

    [Header("Debugging")]
    public bool debugMode = true;
    private GestureType? _allowedType;

    public GestureType AllowedType
    {
        set { _allowedType = value; }
    }
    private static Gesture defaultGesture = new Gesture("default");
    private Gesture previousGestureDetected = defaultGesture;

    private NetworkAdapter network;

    private SortedList<string, Bone> getBones(SortedList<string, Bone> list, OVRSkeleton Hand, bool isLeft)
    {

        foreach (var bone in Hand.Bones)
        {
            list.Add(((int)bone.Id) + "-" + (isLeft ? "left" : "right"), new Bone(Hand, bone, isLeft));
        }

        return list;
    }

    private void Start()
    {
        fingerBones = new SortedList<string, Bone>();
        getBones(fingerBones, skeletonRight, false);
        getBones(fingerBones, skeletonLeft, true);

        string server = PlayerPrefs.GetString("server");
        network = new NetworkAdapter(server);
        StartCoroutine(network.Get(this));
        QuestDebug.Instance.Log("waiting for gestures");
    }

    private void Update()
    {

        if (SavedGestures == null)
        {
            QuestDebug.Instance.Log("no gestures");
            return;
        }

        if (!skeletonLeft.IsDataHighConfidence && !skeletonRight.IsDataHighConfidence)
        {
            tpProv.abortTeleport();
            previousGestureDetected = defaultGesture;
            return;
        }

        if (/* debugMode && */ OVRInput.GetDown(OVRInput.Button.Start))
        {
            QuestDebug.Instance.Log("make a gesture to save in 2 seconds");
            Invoke("SaveAsGesture", 2);
            return;
        }

        Debug.Log("recognize starting");
        Gesture gesture = Recognize();
        Debug.Log("recognize done");
        bool ignore = gesture == null || gesture.Equals(defaultGesture);
        if (ignore)
        {
            // only abort if the gesture is detected
            if (!defaultGesture.Equals(gesture))
            {
                tpProv.abortTeleport();
                previousGestureDetected.time = 0;
                previousGestureDetected = defaultGesture;
            }
            return;
        }

        Debug.Log("types: prev " + previousGestureDetected.type + "  ----  next " + gesture.type);
        if (previousGestureDetected.type == gesture.type)
        {
            bool teleportExecuted = tpProv.updateAndTryTeleport(gesture.gestureIndex);

            // reset to first method after teleport
            if (teleportExecuted)
            {
                QuestDebug.Instance.Log("teleport confirmed");
                previousGestureDetected = SavedGestures.Find((g) => g.type == gesture.type && g.gestureIndex == 0);
                previousGestureDetected.time = delay + 0.1f;
                gesture.time = 0f;
            }
        }
        else
        {
            previousGestureDetected.time = 0f;
            QuestDebug.Instance.Log("found " + gesture.name + " of type " + gesture.type);

            tpProv.abortTeleport();
            tpProv.selectMethod(gesture.type);
            tpProv.initTeleport(new TrackingInfo(CenterEye, skeletonRight, skeletonLeft, fingerBones, Hand.right));
        }
        previousGestureDetected = gesture;


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

        g.handPosLeft = getPalmNormal(getFingers(true));
        g.handPosRight = getPalmNormal(getFingers(false));
        SavedGestures.Add(g);

        StartCoroutine(network.Post(g.ToJson()));
        QuestDebug.Instance.Log("new gesture saved");
        Invoke("reload", 2);
    }

    private void reload()
    {
        StartCoroutine(network.Get(this));
        QuestDebug.Instance.Log("reloaded");
    }

    private Gesture Recognize()
    {
        float BONUS_DISTANCE = 1f;
        float BONUS_THRESHOLD = 1f;
        float minSumDistances = Mathf.Infinity;
        Gesture currentGesture = defaultGesture;

        // test gestures 
        var allowed = SavedGestures;
        if (_allowedType != null)
        {
            allowed = allowed.FindAll((g) => g.type == _allowedType);
        }
        foreach (var gesture in allowed)
        {
            bool couldBeNext = false;

            bool sameType = gesture.type == previousGestureDetected.type;
            if (sameType && gesture.gestureIndex > previousGestureDetected.gestureIndex)
            {
                couldBeNext = true;
            }
            else
            {
                // the gesture is not the first of its type and different to the current
                if (gesture.gestureIndex != 0)
                {
                    continue;
                }
            }

            var threshold = 0.3f;

            // check the hand positioning
            // var left = true;
            // if (!gesture.ignoreLeft && Vector3.Distance(getPalmNormal(getFingers(left)), gesture.handPosLeft) > threshold)
            // {
            //     continue;
            // }

            // if (!gesture.ignoreRight && Vector3.Distance(getPalmNormal(getFingers(!left)), gesture.handPosRight) > threshold)
            // {
            //     continue;
            // }

            float dist = distanceBetweenGestures(gesture, couldBeNext ? threshold * BONUS_THRESHOLD : threshold);

            dist = couldBeNext ? dist * BONUS_DISTANCE : dist;

            var keepGesture = dist > 0 && dist < Mathf.Infinity;

            if (dist < minSumDistances && keepGesture)
            {
                minSumDistances = dist;
                currentGesture = gesture;
            }
        }

        Debug.Log("detected: " + currentGesture.name);
        if (currentGesture.type == previousGestureDetected.type && currentGesture.type != GestureType.Default)
        {
            Debug.Log("same type");
            return currentGesture;
        }

        // set time delay
        currentGesture.time += Time.deltaTime;
        // Debug.Log("time: " + currentGesture.time);

        float timeDelay = currentGesture.type == GestureType.Default ? 2f : delay;
        bool needsTime = currentGesture.time < timeDelay;
        if (needsTime)
        {
            Debug.Log("needs time");
            return null;
        }

        Debug.Log("first recognize");
        return currentGesture;
    }

    private Bone[] getFingers(bool isLeftHand)
    {
        string hand = isLeftHand ? "left" : "right";
        return new Bone[]{
            fingerBones["0-" + hand],
            fingerBones["6-" + hand],
            fingerBones["16-" + hand]
        };
    }
    private Vector3 getPalmNormal(params Bone[] points)
    {
        var p = new Plane(
            points[0].getTransform().position,
            points[1].getTransform().position,
            points[2].getTransform().position
        );

        return p.normal;
    }

    private float distanceBetweenGestures(Gesture gesture, float maxFingerDist)
    {
        float ignoredFingerPenalty = 0.03f;
        float sumDistances = 0;
        for (int i = 0; i < fingerBones.Count; i++)
        {
            var storedFinger = gesture.fingerData[i];
            string key = ((int)storedFinger.id) + "-" + (storedFinger.isLeftHand ? "left" : "right");

            var finger = fingerBones[key];

            // is finger ignored
            if ((gesture.ignoreLeft && finger.isLeftHand) || (gesture.ignoreRight && !finger.isLeftHand))
            {
                sumDistances += ignoredFingerPenalty;
                continue;
            }

            Vector3 currentData = finger.getRelativePosition();
            float distance = Vector3.Distance(currentData, storedFinger.position);
            if (distance > maxFingerDist)
            {
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







