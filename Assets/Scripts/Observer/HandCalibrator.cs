using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class HandCalibrator : MonoBehaviour
{

    public float AverageSize = 1f;
    public OVRHand hand;
    private float[] HandSizes = new float[100];
    private int CalibrationIdx = 0;
    private NetworkAdapter network;

    public UnityEvent<float> CalibrationDone = new UnityEvent<float>();

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
            AverageSize = HandSizes.Aggregate((total, next) => total + next) / HandSizes.Length;
            CalibrationDone.Invoke(AverageSize);
            network.Set("/stats/any", "size", AverageSize);
            QuestDebug.Instance.Log("hand size: " + AverageSize, true);
        }
    }
}
