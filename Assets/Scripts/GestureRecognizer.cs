using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Assertions;
using System.Linq;
using static OVRSkeleton;

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
public class JointCollection
{

    public string _id;

    public JointCollection() { }
    public JointCollection(Bone[] joints)
    {
        this.joints = joints;
    }
    public Bone[] joints;
}



[Serializable]
public class Variant
{
    public int index;
    public List<JointCollection> options;

    [NonSerialized]
    public float time = 0;
}


[Serializable]
public class Gesture
{
    public string name;
    public List<Variant> variants;
    public List<Bone> baseJoints;
    public Bone[] importantJoints;
    public bool ignoreLeft = false;
    public bool ignoreRight = false;
    public GestureType type = GestureType.Default;

    public Vector3 handPosLeft = Vector3.zero;
    public Vector3 handPosRight = Vector3.zero;

    [HideInInspector]
    public float time = 0.0f;
    public Gesture(string gestureName)
    {
        name = gestureName;
        variants = new List<Variant>();
    }

    public string ToJson()
    {
        return JsonUtility.ToJson(this);
    }

    public static GestureList CreateFromJSON(string jsonString)
    {
        return JsonUtility.FromJson<GestureList>(jsonString);
    }
}


[Serializable]
public class Bone
{
    [NonSerialized]
    public OVRSkeleton Hand;

    [NonSerialized]
    public HandCalibrator calibrator;

    [NonSerialized]
    public OVRBone Finger;

    public bool isLeft;
    public BoneId boneId;
    public Vector3 position;

    public Bone(OVRBone OVRFinger, HandCalibrator handCalibrator, bool isLeft)
    {
        Hand = handCalibrator.skeleton;
        Assert.IsNotNull(Hand, "Hand constructor");
        calibrator = handCalibrator;
        Finger = OVRFinger;
        this.isLeft = isLeft;
        boneId = Finger.Id;
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
        var b = new Bone(Finger, calibrator, isLeft);
        b.position = GetScaledPosition();
        return b;
    }

    public string ToID()
    {
        return MakeID(boneId, isLeft);
    }

    public static Bone FromID(string id, HandCalibrator calLeft, HandCalibrator calRight)
    {
        var isLeft = id.EndsWith("left");
        OVRSkeleton.BoneId boneId = (OVRSkeleton.BoneId)int.Parse(id.Split('-')[0]);
        var serializedBone = new Bone(new OVRBone(boneId, 0, null), isLeft ? calLeft : calRight, isLeft);
        Assert.IsTrue(id == serializedBone.ToID());
        return serializedBone;
    }

    public static string MakeID(OVRSkeleton.BoneId boneId, bool isLeft)
    {
        return ((int)boneId) + "-" + (isLeft ? "left" : "right");
    }

    public Vector3 GetScaledPosition()
    {
        return GetRelativePosition() / calibrator.AverageSize;
    }
}

public class GestureTarget : MonoBehaviour
{
    public UnityEvent OnLoaded = new UnityEvent();
    public List<Gesture> savedGestures;

    public SortedList<string, bool> bestPredictingOptions = new SortedList<string, bool>();
    protected NetworkAdapter network;


    public virtual void Start()
    {
        network = new NetworkAdapter();
        ReloadGestures();
    }

    public void SetSavedGestures(GestureList list)
    {
        Assert.IsNotNull(list);
        Assert.IsFalse(list.Gestures.Length == 0);
        savedGestures = new List<Gesture>(list.Gestures);
        OnLoaded.Invoke();
    }

    protected void ReloadGestures()
    {
        StartCoroutine(network.GetGestures(this));
        StartCoroutine(network.GetBestPredictingOptions(this));
    }
}

public class GestureRecognizer : GestureTarget
{
    public Setup setup;
    public HandCalibrator leftCal;
    public HandCalibrator rightCal;
    public Camera CenterEye;
    public float timeDelay = 0.3f;
    public Gesture forceGesture;
    public TeleportProvider tpProv;
    public bool disabled;
    private float baseThreshold = 2f;
    private SortedList<string, Bone> fingerBones;
    private static Variant defaultVariant = new Variant();
    private Variant previousVariantDetected = defaultVariant;
    private Gesture currentGesture;
    private SettingsDto settings;

    public override void Start()
    {
        base.Start();
        fingerBones = new SortedList<string, Bone>();
        GestureHelper.InputBones(fingerBones, rightCal.skeleton, rightCal, Hand.right);
        GestureHelper.InputBones(fingerBones, leftCal.skeleton, leftCal, Hand.left);

        ReloadGestures();
        tpProv.OnAbort.AddListener(() =>
        {
            StartCoroutine(network.Set("/stats/abort"));
            AbortCurrentGesture();
        });
        baseThreshold = PlayerPrefs.GetFloat("threshold");

        Assert.IsNotNull(setup);
        setup.SettingsChanged.AddListener((SettingsDto s) =>
        {
            baseThreshold = s.threshold;
            this.settings = s;
        });
    }

    private void Update()
    {
        if (OVRInput.GetDown(OVRInput.Button.Start))
        {
            ReloadGestures();
            leftCal.SetDebug();
            rightCal.SetDebug();
        }

        if (currentGesture == null) { return; }

        // try to recognize the current gesture
        Variant variant = Recognize();

        // is gesture of the same type
        if (variant == defaultVariant)
        {
            AbortCurrentGesture();
            return;
        }

        // gesture usable
        // set gesture index
        tpProv.UpdateAndTryTeleport(variant.index);

        if (tpProv.GetCurrentTeleporterState() == TransporterState.aborted)
        {
            AbortCurrentGesture();
        }
        previousVariantDetected = variant;
    }

    public void AbortCurrentGesture(bool soft = false)
    {
        if (!soft)
        {
            previousVariantDetected.time = 0;
        }
        if (previousVariantDetected == defaultVariant) { return; }

        tpProv.AbortTeleport();
        previousVariantDetected = defaultVariant;
        SetAllowedType(currentGesture.type);
    }

    public void SetAllowedType(GestureType type)
    {
        if (savedGestures == null)
        {
            return;
        }
        tpProv.SelectMethod(type);
        var value = savedGestures.Find(g => g.type == type);
        if (value == null)
        {
            return;
        }

        // setup tracking information for the teleporters
        var info = new TrackingInfo(CenterEye, rightCal, leftCal, fingerBones, value.ignoreLeft ? Hand.right : Hand.left);
        tpProv.InitTeleport(info);

        currentGesture = value;
    }

    private Variant Recognize()
    {
        if (disabled)
        {
            return defaultVariant;
        }

        var baseDistance = GestureHelper.CalculateOptionError(fingerBones, leftCal, rightCal, currentGesture.baseJoints.ToArray());
        QuestDebug.Instance.Log("dist: " + baseDistance / currentGesture.baseJoints.Count);
        if (baseDistance / currentGesture.baseJoints.Count > baseThreshold)
        {
            return defaultVariant;
        }

        // detect what variant is shown
        Variant bestMatch = defaultVariant;
        float minDist = Mathf.Infinity;
        foreach (var variant in currentGesture.variants)
        {
            float dist = GetBestOptionDistance(variant);
            bool validResult = dist > 0 && dist < Mathf.Infinity;
            if (validResult && minDist > dist)
            {
                minDist = dist;
                bestMatch = variant;
            }
        }

        QuestDebug.Instance.Log("detected: " + bestMatch.index);

        // set time delay
        bestMatch.time += Time.deltaTime;

        // reduce time delay after default gesture 
        bool isSameType = defaultVariant.index == bestMatch.index;
        bool isDefault = bestMatch == defaultVariant;
        float delay = isSameType ? 0 : isDefault ? timeDelay / 2 : timeDelay;
        if (bestMatch.time < delay)
        {
            // gesture still needs time
            return previousVariantDetected;
        }

        return bestMatch;
    }

    private float GetBestOptionDistance(Variant variant)
    {
        float bestDistance = float.PositiveInfinity;
        float averageDist = 0;
        int used = 0;
        for (int v = 0; v < variant.options.Count; v++)
        {
            var option = variant.options[v];
            if (settings.optimizeOptions && !bestPredictingOptions.ContainsKey(option._id) && bestPredictingOptions.Count >= 4)
            {
                continue;
            }

            used += 1;
            float sumDistances = GestureHelper.CalculateOptionError(fingerBones, leftCal, rightCal, option.joints);
            averageDist += sumDistances;
            if (sumDistances < bestDistance)
            {
                bestDistance = sumDistances;
            }
        }
        if (settings.averageMethod)
        {
            return averageDist / used;
        }
        return bestDistance;
    }
}







