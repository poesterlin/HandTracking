using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PotionManager : MonoBehaviour
{
    public Camera player;
    public Mortar mortar;
    public Vector3[] positions = new Vector3[8];
    public GameObject potionPrefab;

    private int count = 0;

    void Start()
    {
        Shuffle(positions);
    }

    public void CreateFlask()
    {
        var flask = Instantiate(potionPrefab, transform.TransformPoint(positions[count]), Quaternion.identity, transform);
        var script = flask.GetComponent<Flask>();
        script.mortar = mortar;
        script.player = player;
        script.parent = transform;
        
        var grabbable = flask.GetComponent<HandTrackingGrabbable>();
        grabbable.mortar = mortar;
        count += 1;
    }


    void Shuffle<T>(T[] a)
    {
        for (int i = a.Length - 1; i > 0; i--)
        {
            int rnd = Random.Range(0, i);
            T temp = a[i];
            a[i] = a[rnd];
            a[rnd] = temp;
        }
    }
}
