using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;
using static OVRSkeleton;


public class HandCalibrator : MonoBehaviour
{

    public float AverageSize = 1f;
    public OVRHand hand;
    public OVRSkeleton skeleton;
    private float[] HandSizes = new float[75 * 5]; // record for 5s at 75 frames
    private int CalibrationIdx = 0;
    private NetworkAdapter network;
    private List<OVRBone[]> fingers = new List<OVRBone[]>();

    public UnityEvent<float> CalibrationDone = new UnityEvent<float>();
    private Queue<GameObject> joints;
    public int queueSize = 100;
    private bool debug = false;

    public Dictionary<BoneId, float> WeightDict { get; private set; }

    void Start()
    {
        network = new NetworkAdapter();
        fingers.Add(GetFingerBones(BoneId.Hand_Start, BoneId.Hand_WristRoot, BoneId.Hand_Middle1, BoneId.Hand_Middle2, BoneId.Hand_Middle3, BoneId.Hand_MiddleTip));
        fingers.Add(GetFingerBones(BoneId.Hand_Start, BoneId.Hand_Index3, BoneId.Hand_Ring3));
        fingers.Add(GetFingerBones(BoneId.Hand_Start, BoneId.Hand_WristRoot, BoneId.Hand_Thumb0));
        joints = new Queue<GameObject>(queueSize);

        InitDict();
    }

    // Update is called once per frame
    void Update()
    {
        if (Application.isEditor)
        {
            AddToAverageSize(UnityEngine.Random.Range(0.5f, 0.7f));
            return;
        }

        if (hand.IsTracked && hand.IsDataHighConfidence)
        {
            // AddToAverageSize(hand.HandScale);
            var sum = GetFingerLength(fingers[0]);
            AddToAverageSize(sum);
        }
    }

    public void SetDebug()
    {
        debug = !debug;
    }


    private void AddToAverageSize(float size)
    {
        HandSizes[CalibrationIdx] = size;
        CalibrationIdx += 1;

        if (CalibrationIdx == HandSizes.Length)
        {
            CalibrationIdx = 0;
            AverageSize = HandSizes.Aggregate((total, next) => total + next) / HandSizes.Length;
            CalibrationDone.Invoke(AverageSize);
            CalibrationDone.RemoveAllListeners();
            StartCoroutine(network.Set("/stats/handsize", "size", AverageSize));
            // Destroy(GetComponent<HandCalibrator>());
        }
    }

    private OVRBone[] GetFingerBones(params OVRSkeleton.BoneId[] bones)
    {
        return skeleton.Bones.Where((b) => bones.Contains(b.Id)).ToArray();
    }

    private float GetFingerLength(OVRBone[] bones)
    {
        float sum = 0;
        Vector3 last = bones[0].Transform.position;
        // DebugPosition(last, Color.blue);

        foreach (var bone in bones.Skip(1))
        {
            Vector3 pos = bone.Transform.position;
            sum += Vector3.Distance(last, pos);
            DebugPosition(pos, Color.blue);
            last = pos;
        }
        return sum;
    }

    public void DebugPosition(Vector3 pos, Color col, float size = 0.01f)
    {
        if (!debug) return;
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);

        joints.Enqueue(obj);
        if (joints.Count > queueSize)
        {
            Destroy(joints.Dequeue());
        }

        col.a = 0.2f;
        var material = obj.GetComponent<Renderer>().material;
        material.color = col;

        material.SetOverrideTag("RenderType", "Transparent");
        material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.One);
        material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetFloat("_ZWrite", 0.0f);
        material.DisableKeyword("_ALPHATEST_ON");
        material.DisableKeyword("_ALPHABLEND_ON");
        material.EnableKeyword("_ALPHAPREMULTIPLY_ON");

        obj.transform.position = pos;
        obj.transform.localScale = Math.Min(size, 0.1f) * Vector3.one;
        obj.GetComponent<Collider>().enabled = false;
    }

    public void InitDict()
    {
        WeightDict = new Dictionary<BoneId, float>();

        float full = 1, med = 1.3f, little = 1.6f, none = 10f;

        WeightDict.Add(BoneId.Hand_WristRoot, little);
        WeightDict.Add(BoneId.Hand_ForearmStub, none);
        WeightDict.Add(BoneId.Hand_Thumb0, med);
        WeightDict.Add(BoneId.Hand_Thumb1, full);
        WeightDict.Add(BoneId.Hand_Thumb2, full);
        WeightDict.Add(BoneId.Hand_Thumb3, full);
        WeightDict.Add(BoneId.Hand_Index1, full);
        WeightDict.Add(BoneId.Hand_Index2, full);
        WeightDict.Add(BoneId.Hand_Index3, full);
        WeightDict.Add(BoneId.Hand_Middle1, full);
        WeightDict.Add(BoneId.Hand_Middle2, med);
        WeightDict.Add(BoneId.Hand_Middle3, full);
        WeightDict.Add(BoneId.Hand_Ring1, full);
        WeightDict.Add(BoneId.Hand_Ring2, full);
        WeightDict.Add(BoneId.Hand_Ring3, full);
        WeightDict.Add(BoneId.Hand_Pinky0, med);
        WeightDict.Add(BoneId.Hand_Pinky1, med);
        WeightDict.Add(BoneId.Hand_Pinky2, med);
        WeightDict.Add(BoneId.Hand_Pinky3, little);
        WeightDict.Add(BoneId.Hand_ThumbTip, full);
        WeightDict.Add(BoneId.Hand_IndexTip, med);
        WeightDict.Add(BoneId.Hand_MiddleTip, med);
        WeightDict.Add(BoneId.Hand_RingTip, med);
        WeightDict.Add(BoneId.Hand_PinkyTip, med);
    }
}
