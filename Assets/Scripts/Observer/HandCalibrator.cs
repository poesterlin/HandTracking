using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class HandCalibrator : MonoBehaviour
{

    public OVRHand hand;
    private float[] HandSizes = new float[100];
    private int CalibrationIdx = 0;
    private NetworkAdapter network;

    void Start()
    {
        network = new NetworkAdapter();
    }

    // Update is called once per frame
    void Update()
    {
        if (hand.IsTracked && hand.IsDataHighConfidence)
        {
            AddToAverageSize(hand.HandScale);
        }

    }

    private void AddToAverageSize(float size)
    {
        HandSizes[CalibrationIdx] = size;
        CalibrationIdx += 1;

        if (CalibrationIdx == HandSizes.Length)
        {
            CalibrationIdx = 0;
            float average = HandSizes.Aggregate((total, next) => total + next) / HandSizes.Length;
            network.Set("/stats/any", "size", average);
            QuestDebug.Instance.Log("hand size: " + average, true);
        }
    }
}
