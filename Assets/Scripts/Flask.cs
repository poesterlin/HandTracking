using UnityEngine;

public class Flask : MonoBehaviour
{
    public Camera player;
    public Vector3 hipsOffset = new Vector3(-0.3f, -0.6f, -0.3f);
    public bool wasGrabbed = false;
    public bool isGrabbed = false;
    public float snapSpeed;
    public Mortar mortar;
    public Rigidbody body;
    public Transform parent;
    public OVRCameraRig cameraRig;
    public Vector4 t = new Vector4(1, 1, 1, 1);
    void Reset()
    {
        wasGrabbed = false;
        isGrabbed = false;
        body.velocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
    }

    void Start()
    {
        mortar.OnPotion.AddListener(Reset);
        Reset();
    }

    void Update()
    {
        if (wasGrabbed && !isGrabbed)
        {
            body.useGravity = false;
            var step = snapSpeed * Time.deltaTime;
            transform.position = Vector3.MoveTowards(transform.position, cameraRig.centerEyeAnchor.position + hipsOffset, step);
            var quat = cameraRig.centerEyeAnchor.rotation;
            transform.rotation = quat; // new Quaternion(quat.x * t.x, quat.y * t.z, quat.z * t.z, quat.w * t.w);
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
        transform.SetParent(parent);
    }
}
