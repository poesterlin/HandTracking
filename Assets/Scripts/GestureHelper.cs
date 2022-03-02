using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class GestureHelper
{
    public static void InputBones(SortedList<string, Bone> fingerBones, OVRSkeleton hand, HandCalibrator calibrator, Hand side)
    {
        bool isLeft = side == Hand.left;
        foreach (var bone in hand.Bones)
        {
            var b = new Bone(bone, calibrator, isLeft);
            fingerBones.Add(b.ToID(), b);
        }
    }

    public static float CalculateOptionError(SortedList<string, Bone> fingerBones, HandCalibrator leftCal, HandCalibrator rightCal, Bone[] joints)
    {
        float error = 0;
        for (int i = 0; i < joints.Length; i++)
        {
            var storedFinger = joints[i];
            var id = storedFinger.ToID();
            Assert.IsTrue(fingerBones.ContainsKey(id));
            var finger = fingerBones[id];
            Assert.IsNotNull(finger);

            HandCalibrator cal = finger.isLeft ? leftCal : rightCal;
            var hand = cal.hand;

            Vector3 currentData = finger.GetRelativePosition();
            cal.WeightDict.TryGetValue(storedFinger.boneId, out float weight);

            var point = storedFinger.position * cal.AverageSize;
            // cal.DebugPosition(hand.transform.TransformPoint(point), Color.green, weight * 0.005f);
            error += Vector3.Distance(currentData, point) / weight;
        }

        return error;
    }
}
