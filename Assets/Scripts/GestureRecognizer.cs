using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using WebSocketSharp;


[Serializable]
public enum GestureType
{
    Default,
    FingerGesture,
    PalmGesture,
    TriangleGesture,
}


[Serializable]
public class GestureList
{
    public Gesture[] Gestures;
}


[Serializable]
public class Gesture
{
    public string name;
    public List<Bone> fingerData; // Relative to hand
    public UnityEvent onRecognized;
    public bool ignoreLeft = false;
    public bool ignoreRight = false;
    public GestureType type = GestureType.Default;
    public int gestureIndex = 0;

    [HideInInspector]
    public float time = 0.0f;

    public Gesture(string gestureName)
    {
        name = gestureName;
        fingerData = null;
        onRecognized = new UnityEvent();
    }

    public string ToJson()
    {
        return JsonUtility.ToJson(this);
    }

    public static GestureList CreateFromJSON(string jsonString)
    {
        return JsonUtility.FromJson<GestureList>(jsonString);
    }

    public Bone getReference()
    {
        return fingerData.Find(finger => finger.id == OVRSkeleton.BoneId.Hand_Index2 && !finger.isLeftHand);
    }
}


[Serializable]
public class Bone
{
    [NonSerialized]
    OVRSkeleton Hand;
    [NonSerialized]
    public OVRBone Finger;

    public bool isLeftHand;
    public OVRSkeleton.BoneId id;
    public Vector3 position;

    public Bone(OVRSkeleton Hand, OVRBone OVRFinger, bool isLeft)
    {
        this.Hand = Hand;
        Finger = OVRFinger;
        isLeftHand = isLeft;
        id = Finger.Id;
    }

    public Transform getTransform()
    {
        return Finger.Transform;
    }

    public Vector3 getRelativePosition()
    {
        return Hand.transform.InverseTransformPoint(Finger.Transform.position);
    }

    public Vector3 getAbsolutePosition()
    {
        return Finger.Transform.position;
    }

    public Bone save()
    {
        position = getRelativePosition();
        return this;
    }
}

[System.Serializable]
public class GestureRecognizer : MonoBehaviour
{

    public string server = "server.oesterlin.dev";
    WebSocket ws;
    public OVRSkeleton skeletonLeft;
    public OVRSkeleton skeletonRight;
    private SortedList<string, Bone> fingerBones;

    private NetworkAdapter network;
    private bool shouldSave;

    private SortedList<string, Bone> getBones(SortedList<string, Bone> list, OVRSkeleton Hand, bool isLeft)
    {
        foreach (var bone in Hand.Bones)
        {
            list.Add(((int)bone.Id) + "-" + (isLeft ? "left" : "right"), new Bone(Hand, bone, isLeft));
        }

        return list;
    }

    private void Start()
    {
        fingerBones = new SortedList<string, Bone>();
        getBones(fingerBones, skeletonRight, false);
        getBones(fingerBones, skeletonLeft, true);

        network = new NetworkAdapter("https://" + server);

        ws = new WebSocket("wss://" + server + ":8080");
        ws.Connect();
        ws.OnMessage += (object sender, MessageEventArgs e) => MessageReceived(sender, e);
        ws.OnClose += (object sender, CloseEventArgs e) =>
        {
            Debug.Log("closing: " + e.Reason + "(" + e.Code + ")");
            Invoke("Reconnect", 1f);
        };

        ws.OnError += (object sender, ErrorEventArgs e) =>
        {
            Debug.Log("error: " + e.Message);
            Debug.Log(e.Exception);
            Invoke("Reconnect", 1f);
        };
    }

    private void Update(){
        if(shouldSave){
            shouldSave = false;
            SaveAsGesture();
        }
    }

    public void SaveAsGesture()
    {
        Gesture g = new Gesture("new gesture");
        List<Bone> data = new List<Bone>();
        foreach (KeyValuePair<string, Bone> bone in fingerBones)
        {
            data.Add(bone.Value.save());
        }

        g.fingerData = data;

        StartCoroutine(network.Post(g.ToJson()));
        QuestDebug.Instance.Log("new gesture saved");
        ws.Send("done");
    }

    private void MessageReceived(object sender, MessageEventArgs e)
    {
        string saveCmd = "save";
        if (saveCmd.Equals(e.Data))
        {
            shouldSave = true;
        }
    }


    private void Reconnect()
    {
        Debug.Log("reconnecting");
        ws.Close();
        ws.Connect();
    }
}







