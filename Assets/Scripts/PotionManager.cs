using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PotionManager : MonoBehaviour
{
    public Camera player;
    public Mortar mortar;
    public Vector3[] positions = new Vector3[8];
    public GameObject potionPrefab;
    public GameObject flask;
    public ForrestStudyObserver observer;
    public OVRCameraRig cameraRig;
    private NetworkAdapter network;


    private int count = 0;

    void Start()
    {
        Shuffle(positions);
        network = new NetworkAdapter();
    }

    public void CreateFlask(GestureType type)
    {
        if (type == GestureType.Default || flask != null)
        {
            return;
        }
        flask = Instantiate(potionPrefab, transform.TransformPoint(positions[count]), Quaternion.identity, transform);
        var script = flask.GetComponent<Flask>();
        script.mortar = mortar;
        script.player = player;
        script.parent = transform;
        script.cameraRig = cameraRig;
        script.snapSpeed = 8f;
        script.t = new Vector4(1, 1, 1, 1);

        var grabbable = flask.GetComponent<HandTrackingGrabbable>();
        grabbable.mortar = mortar;

        grabbable.wasGrabbed.AddListener(observer.DisableTeleport);
        grabbable.wasReleased.AddListener(observer.EnableTeleport);
        count = (count + 1) % positions.Length - 1;
        StartCoroutine(network.Set("/stats/flaskPosition", "posX", flask.transform.position.x, "posY", flask.transform.position.z));
    }


    void Shuffle<T>(T[] a)
    {
        for (int i = a.Length; i > 0; i--)
        {
            int rnd = Random.Range(0, i);
            T temp = a[i - 1];
            a[i - 1] = a[rnd];
            a[rnd] = temp;
        }
    }
}
