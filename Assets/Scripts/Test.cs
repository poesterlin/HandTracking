using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour
{
    private ICP_Tester tester;
    private Queue<GameObject> joints;
    public int queueSize = 100;

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
        obj.transform.localScale = 0.2f * Vector3.one;
        obj.GetComponent<Collider>().enabled = false;
    }

    // Start is called before the first frame update
    void Start()
    {
        joints = new Queue<GameObject>(queueSize);
        tester = new ICP_Tester();
        tester.setup();
    }

    void Update()
    {
        tester.test();
        for (int i = 0; i < tester.staticPointCloud.Length; i++)
        {
            DebugPosition(tester.staticPointCloud[i], Color.green);
        }

        for (int i = 0; i < tester.dynamicPointCloud.Length; i++)
        {
            DebugPosition(tester.dynamicPointCloud[i], Color.red);
        }
    }

}
