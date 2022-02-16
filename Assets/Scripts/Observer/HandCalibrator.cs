using System.Collections.Generic;
using System.Linq;
using UnityEngine;
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


    void Start()
    {
        network = new NetworkAdapter();

        // fingers.Add(GetFingerBones(BoneId.Hand_Start, BoneId.Hand_WristRoot, BoneId.Hand_Index1, BoneId.Hand_Index2, BoneId.Hand_Index3));
        fingers.Add(GetFingerBones(BoneId.Hand_Start, BoneId.Hand_WristRoot, BoneId.Hand_Middle1, BoneId.Hand_Middle2, BoneId.Hand_Middle3));
        // fingers.Add(GetFingerBones(BoneId.Hand_Start, BoneId.Hand_WristRoot, BoneId.Hand_Ring1, BoneId.Hand_Ring2, BoneId.Hand_Ring3));
        // fingers.Add(GetFingerBones(BoneId.Hand_Start, BoneId.Hand_WristRoot, BoneId.Hand_Pinky1, BoneId.Hand_Pinky2, BoneId.Hand_Pinky3));

        joints = new Queue<GameObject>(queueSize);
    }

    // Update is called once per frame
    void Update()
    {
        if (Application.isEditor)
        {
            AddToAverageSize(Random.Range(0.9f, 1.2f));
            return;
        }

        if (hand.IsTracked && hand.IsDataHighConfidence)
        {
            // AddToAverageSize(hand.HandScale);
            float sum = 0;
            foreach (var finger in fingers)
            {
                sum += GetFingerLength(finger);
            }
            AddToAverageSize(sum);
        }

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
            // DebugPosition(pos, Color.blue);
            last = pos;
        }
        return sum;
    }

    public void DebugPosition(Vector3 pos, Color col)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);

        joints.Enqueue(obj);
        if (joints.Count > queueSize)
        {
            Destroy(joints.Dequeue());
        }

        obj.GetComponent<Renderer>().material.color = col;
        obj.transform.position = pos;
        obj.transform.localScale = 0.01f * Vector3.one;
        obj.GetComponent<Collider>().enabled = false;
    }
}
