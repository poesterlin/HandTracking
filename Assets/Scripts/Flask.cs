using System.Collections;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;

public class Flask : MonoBehaviour
{
    public OVRPlayerController player;
    public HandTrackingGrabbable grabbable;
    public Vector3 hipsOffset = new Vector3(-0.3f, -0.6f, -0.3f);
    public bool wasGrabbed = false;
    public bool isGrabbed = false;
    public float snapSpeed;

    public Mortar mortar;
    public Vector3[] positions;
    private int count = 0;
    private Rigidbody body;

    void Reset()
    {
        wasGrabbed = false;
        isGrabbed = false;
        grabbable.Disable();
        count += 1;
        // transform.localPosition = positions[count];
    }

    // Start is called before the first frame update
    void Start()
    {
        Shuffle(positions);
        mortar.OnPotion.AddListener(Reset);
        grabbable.wasGrabbed.AddListener(Grab);
        grabbable.wasReleased.AddListener(Released);
        body = gameObject.GetComponent<Rigidbody>();
        Reset();
    }

    // Update is called once per frame
    void Update()
    {
        if (wasGrabbed && !isGrabbed)
        {
            body.useGravity = false;
            var step = snapSpeed * Time.deltaTime;
            transform.position = Vector3.MoveTowards(transform.position, player.transform.position + hipsOffset, step);
        }
        else
        {
            body.useGravity = true;
        }
    }

    public void Grab()
    {
        wasGrabbed = true;
        isGrabbed = true;
    }

    public void Released()
    {
        isGrabbed = false;
    }

    void Shuffle<T>(T[] a)
    {
        for (int i = a.Length - 1; i > 0; i--)
        {
            int rnd = UnityEngine.Random.Range(0, i);
            T temp = a[i];
            a[i] = a[rnd];
            a[rnd] = temp;
        }
    }
}
