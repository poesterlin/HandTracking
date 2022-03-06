using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandsDisplay : MonoBehaviour
{
    public HandCalibrator calibrator;
    public Transform basePos;
    public Vector3 offset;
    private SortedList<OVRSkeleton.BoneId, GameObject> joints = new SortedList<OVRSkeleton.BoneId, GameObject>();
    private bool isSetup;
    private float scale = 2.8f;
    private Quaternion baseRotation;

    void Start()
    {
        calibrator.CalibrationDone.AddListener(Setup);
        baseRotation = basePos.rotation;
    }

    // Update is called once per frame
    void Update()
    {
        if (!isSetup && calibrator.IsTrackedWell())
        {
            return;
        }

        foreach (var item in calibrator.skeleton.Bones)
        {
            var pos = basePos.TransformPoint(calibrator.hand.transform.InverseTransformPoint(item.Transform.position) * scale);
            joints[item.Id].transform.position = pos + offset;
        }

        var lookPos = calibrator.hand.transform.position - basePos.position;
        basePos.rotation = Quaternion.LookRotation(lookPos) * baseRotation;
    }

    private void Setup(float size)
    {
        foreach (var bone in calibrator.skeleton.Bones)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            var material = obj.GetComponent<Renderer>().material;
            material.color = Color.white;
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", Color.yellow);
            obj.transform.localScale = 0.02f * Vector3.one;
            obj.GetComponent<Collider>().enabled = false;
            joints.Add(bone.Id, obj);
        }
        isSetup = true;
    }
}
