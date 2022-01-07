using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class test : MonoBehaviour
{

    public LineRenderer line;
    public ProjectileSimulator simulator;
    public Vector3 start;
    public Vector3 force;
    public Vector3 skew = Vector3.left;

    // Update is called once per frame
    void Update()
    {
        var prediction = simulator.Predict(start, force, out bool hit);
        var target = prediction.Last() + skew;
        var trajectory = simulator.Skew(prediction, target);

        Debug.Log("hit: " + hit);

        line.positionCount = 0;
        line.positionCount = trajectory.Count;
        for (int i = 0; i < trajectory.Count; i++)
        {
            line.SetPosition(i, trajectory[i]);
        }

    }
}
