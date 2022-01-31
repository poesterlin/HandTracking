using System;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;
using UnityEngine.Events;
using System.Linq;

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

    public Transform GetTransform()
    {
        return Finger.Transform;
    }

    public Vector3 GetRelativePosition()
    {
        return Hand.transform.InverseTransformPoint(Finger.Transform.position);
    }

    public Vector3 GetAbsolutePosition()
    {
        return Finger.Transform.position;
    }

    public Bone Save()
    {
        position = GetRelativePosition();
        return this;
    }

    public string ToID()
    {
        return ((int)id) + "-" + (isLeftHand ? "left" : "right");
    }
}

public class GestureRecognizer : MonoBehaviour
{
    public OVRSkeleton skeletonLeft;
    public OVRSkeleton skeletonRight;
    public OVRHand left;
    public OVRHand right;

    public Camera CenterEye;
    public List<Gesture> SavedGestures;
    public float gestureThreshold = 0.04f;
    public float timeDelay = 0.3f;
    // public float TrackingThreshold = 1f;
    public float ignoredFingerPenalty = 0.015f;
    public Gesture forceGesture;
    public TeleportProvider tpProv;
    private SortedList<string, Bone> fingerBones;
    private GestureType? _allowedType;
    public GestureType AllowedType
    {
        set { _allowedType = value; }
    }

    public bool disabled;

    public Vector3 leftHandBase;
    public Vector3 rightHandBase;

    private static Gesture defaultGesture = new Gesture("default");
    private Gesture previousGestureDetected = defaultGesture;
    private NetworkAdapter network;

    private float TrackingLostTime;
    private bool isCalibrating = true;
    private float[] HandSizes = new float[100];
    private int CalibrationIdx = 0;


    private void Start()
    {
        fingerBones = new SortedList<string, Bone>();
        GetBones(fingerBones, skeletonRight, Hand.right);
        GetBones(fingerBones, skeletonLeft, Hand.left);

        network = new NetworkAdapter();
        StartCoroutine(network.UpdateForceGesture(this));
        QuestDebug.Instance.Log("waiting for gestures", true);

        tpProv.OnAbort.AddListener(() => AbortCurrentGesture());
    }

    private void Update()
    {
        // calibrate hand size
        if (isCalibrating && left.IsDataHighConfidence && right.IsDataHighConfidence)
        {
            AddToAverageSize(left.HandScale, right.HandScale);
            QuestDebug.Instance.Log("left: " + left.HandScale + " right: " + right.HandScale, true);
            return;
        }
        else if (isCalibrating && (!left.IsDataHighConfidence || !right.IsDataHighConfidence))
        {
            QuestDebug.Instance.Log("not confident enough for calibration", true);
        }

        if (SavedGestures == null || isCalibrating) { return; }

        // // at least one hand has to be recognized with a high confidence
        // if (!skeletonLeft.IsDataHighConfidence || !skeletonRight.IsDataHighConfidence)
        // {
        //     // TrackingLostTime += Time.deltaTime;
        //     // if (TrackingLostTime > TrackingThreshold)
        //     // {
        //     //     QuestDebug.Instance.Log("abort on low confidence", true);
        //     //     AbortCurrentGesture();
        //     // }
        //     return;
        // }

        // TrackingLostTime = 0f;

        // abort if the teleport method is in an error state
        if (tpProv.GetCurrentTeleporterState() == TransporterState.aborted)
        {
            QuestDebug.Instance.Log("abort to sync", true);
            AbortCurrentGesture();
        }

        // try to recognize the current gesture
        Gesture gesture = Recognize();

        // soft abort if default gesture is newly recognized
        if (tpProv.IsMethodSet() && gesture.type == defaultGesture.type && previousGestureDetected.type != gesture.type)
        {
            QuestDebug.Instance.Log("abort on default", true);
            AbortCurrentGesture(true);
            return;
        }

        // if (!tpProv.IsMethodSet() && gesture.type != defaultGesture.type)
        // {
        //     // select method
        //     tpProv.SelectMethod(gesture.type);

        //     // setup tracking information for the teleporters
        //     var info = new TrackingInfo(CenterEye, skeletonRight, skeletonLeft, fingerBones, gesture.ignoreLeft ? Hand.right : Hand.left);
        //     tpProv.InitTeleport(info);
        // }

        // gesture usable
        // is gesture of the same type
        if (previousGestureDetected.type == gesture.type)
        {
            // set gesture index
            bool teleportExecuted = tpProv.UpdateAndTryTeleport(gesture.gestureIndex);

            // reset to first method after teleport
            if (teleportExecuted)
            {
                QuestDebug.Instance.Log("teleport confirmed", true);
                previousGestureDetected = SavedGestures.Find((g) => g.type == gesture.type && g.gestureIndex == 0);
                previousGestureDetected.time = timeDelay;
                gesture.time = 0f;
                return;
            }

            // if (tpProv.GetCurrentTeleporterState() == TransporterState.aborted)
            // {
            //     AbortCurrentGesture();
            //     return;
            // }
        }
        else // gesture of different type
        {
            QuestDebug.Instance.Log("found " + gesture.name + " of type " + gesture.type, true);

            AbortCurrentGesture();

            // reset all gesture times
            defaultGesture.time = 0f;
            SavedGestures.ForEach(gesture => gesture.time = 0f);
            gesture.time = timeDelay;

            // select method
            tpProv.SelectMethod(gesture.type);

            // setup tracking information for the teleporters
            var info = new TrackingInfo(CenterEye, skeletonRight, skeletonLeft, fingerBones, gesture.ignoreLeft ? Hand.right : Hand.left);
            tpProv.InitTeleport(info);
        }
        previousGestureDetected = gesture;
    }

    private void AddToAverageSize(float sizeLeft, float sizeRight)
    {
        HandSizes[CalibrationIdx] = (sizeLeft + sizeRight) / 2;
        CalibrationIdx += 1;

        if (CalibrationIdx == HandSizes.Length)
        {
            isCalibrating = false;
            float average = HandSizes.Aggregate((total, next) => total + next) / HandSizes.Length;
            StartCoroutine(network.GetGestures(this, average));
            QuestDebug.Instance.Log("hand size: " + average, true);
        }
    }

    private SortedList<string, Bone> GetBones(SortedList<string, Bone> list, OVRSkeleton hand, Hand side)
    {
        bool isLeft = side == Hand.left;
        foreach (var bone in hand.Bones)
        {
            var b = new Bone(hand, bone, isLeft);
            list.Add(b.ToID(), b);
        }

        return list;
    }
    public void AbortCurrentGesture(bool soft = false)
    {
        if (!soft)
        {
            previousGestureDetected.time = 0;
        }
        if (previousGestureDetected.type == GestureType.Default) { return; }

        tpProv.AbortTeleport();
        previousGestureDetected = defaultGesture;
    }

    private void SaveAsGesture()
    {
        Gesture g = new Gesture("new gesture");
        List<Bone> data = new List<Bone>();
        foreach (KeyValuePair<string, Bone> bone in fingerBones)
        {
            data.Add(bone.Value.Save());
        }

        g.fingerData = data;

        g.handPosLeft = GetPalmNormal(GetFingers(Hand.left));
        g.handPosRight = GetPalmNormal(GetFingers(Hand.right));
        SavedGestures.Add(g);

        StartCoroutine(network.Post(g.ToJson()));
        QuestDebug.Instance.Log("new gesture saved", true);
        Invoke("ReloadGestures", 1);
    }

    private void ReloadGestures()
    {
        StartCoroutine(network.GetGestures(this, 1f));
        QuestDebug.Instance.Log("reloaded", true);
    }

    private Gesture Recognize()
    {
        float minSumDistances = Mathf.Infinity;
        Gesture bestMatch = defaultGesture;

        if (disabled)
        {
            return bestMatch;
        }

        // TODO: Remove
        if (forceGesture != null)
        {
            var gest = SavedGestures.Find((g) =>
            {
                return forceGesture.name.Equals(g.name);
            });
            if (gest != null)
            {
                Debug.Log("forced gesture: " + gest.name);
                return gest;
            }
        }

        // filter by type if only one type is allowed
        var filtered = SavedGestures.FindAll((g) =>
        {
            bool couldBeNext = g.type == previousGestureDetected.type || g.gestureIndex == 0;
            bool allowed = _allowedType == null || g.type == _allowedType;
            return couldBeNext && allowed;
        });

        // detect what gesture is shown
        foreach (var gesture in filtered)
        {
            float dist = DistanceBetweenGestures(gesture, gestureThreshold);
            bool validResult = dist > 0 && dist < Mathf.Infinity;
            if (validResult && dist < minSumDistances)
            {
                minSumDistances = dist;
                bestMatch = gesture;
            }
        }

        QuestDebug.Instance.Log("detected: " + bestMatch.name);
        if (bestMatch.type == previousGestureDetected.type)
        {
            QuestDebug.Instance.Log("same type");
            return bestMatch;
        }

        // set time delay
        bestMatch.time += Time.deltaTime;

        if (bestMatch.time < timeDelay)
        {
            // gesture still needs time
            return previousGestureDetected;
        }

        QuestDebug.Instance.Log("first recognize");
        return bestMatch;
    }

    private Bone[] GetFingers(Hand hand)
    {
        if (Application.isEditor)
        {
            var b = new Bone(skeletonLeft, new OVRBone(OVRSkeleton.BoneId.Hand_Start, 0, transform), true);
            return new Bone[] { b, b, b };
        }

        string key = hand == Hand.left ? "left" : "right";
        return new Bone[]{
            fingerBones["0-" + key],
            fingerBones["6-" + key],
            fingerBones["16-" + key]
        };
    }

    private Vector3 GetPalmNormal(params Bone[] points)
    {
        return new Plane(
            points[0].GetTransform().position,
            points[1].GetTransform().position,
            points[2].GetTransform().position
        ).normal;
    }

    private float DistanceBetweenGestures(Gesture gesture, float maxFingerDist)
    {
        float sumDistances = 0;
        for (int i = 0; i < fingerBones.Count; i++)
        {
            var storedFinger = gesture.fingerData[i];
            var finger = fingerBones[storedFinger.ToID()];

            // is finger ignored
            if ((gesture.ignoreLeft && finger.isLeftHand) || (gesture.ignoreRight && !finger.isLeftHand))
            {
                sumDistances += ignoredFingerPenalty;
                continue;
            }

            Vector3 currentData = finger.GetRelativePosition();
            float distance = Vector3.Distance(currentData, storedFinger.position);
            if (distance > maxFingerDist)
            {
                gesture.time = 0f;
                QuestDebug.Instance.Log("--- abort gesture: " + gesture.name);
                QuestDebug.Instance.Log("distance above threshold (max " + maxFingerDist + ") :" + distance + " for finger " + storedFinger.id + (storedFinger.isLeftHand ? " (left)" : " (right)"));
                return Mathf.Infinity;
            }

            sumDistances += distance;
        }


        QuestDebug.Instance.Log("gesture: " + gesture.name + " dist: " + sumDistances);
        return sumDistances;
    }
}







