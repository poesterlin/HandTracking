using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Networking.PlayerConnection;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;
using UnityEngine.UI;
using WebSocketSharp;

public class CallibrationObserver : GestureTarget, IStudyObserver
{
    public Setup setup;
    public int index = 0;
    public Gesture currentGesture;
    public HandCalibrator leftCal;
    public HandCalibrator rightCal;
    private SortedList<string, Bone> fingerBones;
    private List<JointCollection> snapshots = new List<JointCollection>();
    public int queueSize = 3;
    public UnityEvent<GestureType> OnTypeChange = new UnityEvent<GestureType>();
    public UnityEvent<int> OnIndexChange = new UnityEvent<int>();
    public Text textEl;

    public Animator LeftHand;
    public Animator RightHand;
    private WebSocket ws;
    private bool shouldSave = false;
    private float baseThreshold;
    private float time = 0;

    public override void Start()
    {
        base.Start();

        ws = ConnectWebsocket();
        Debug.Log(ws.Url);

        fingerBones = new SortedList<string, Bone>();
        GestureHelper.InputBones(fingerBones, rightCal.skeleton, rightCal, Hand.right);
        GestureHelper.InputBones(fingerBones, leftCal.skeleton, leftCal, Hand.left);
        textEl.gameObject.SetActive(true);
        leftCal.SetDebug();
        rightCal.SetDebug();
        OnLoaded.AddListener(() =>
        {
            Assert.IsTrue(savedGestures.Count > 0);

            var order = new GestureType[1];
            order[0] = savedGestures[0].type;
            SetOrder(order);
            QuestDebug.Instance.Log("base joints: " + currentGesture.baseJoints.Count, true);
        });

        baseThreshold = PlayerPrefs.GetFloat("threshold");
        setup.SettingsChanged.AddListener((SettingsDto s) =>
        {
            baseThreshold = s.threshold;
        });
    }

    private WebSocket ConnectWebsocket()
    {
        var webSocket = new WebSocket("wss://vr.oesterlin.dev:9004");
        webSocket.OnOpen += (object sender, EventArgs e) => QuestDebug.Instance.Log("socket opended", true);
        webSocket.OnMessage += MessageReceived;

        webSocket.OnClose += (object sender, CloseEventArgs e) =>
        {
            QuestDebug.Instance.Log("socket closing: " + e.Reason + "(" + e.Code + ")", true);
            ws = ConnectWebsocket();
        };

        webSocket.OnError += (object sender, ErrorEventArgs e) =>
         {
             QuestDebug.Instance.Log("socket error: " + e.Message, true);
             QuestDebug.Instance.Log(e.Exception.ToString(), true);
             ws.Close();
             ws = ConnectWebsocket();
         };

        webSocket.Connect();
        return ws;
    }

    private void MessageReceived(object sender, MessageEventArgs e)
    {
        shouldSave = true;
    }

    void Update()
    {
        var parent = RightHand.transform.parent.transform;
        parent.Rotate(new Vector3(0, 0.1f, 0));

        LeftHand.transform.position = parent.TransformPoint(new Vector3(0.55f, -0.03f, 0));

        if (currentGesture == null)
        {
            return;
        }
        time += Time.deltaTime;

        if (currentGesture.baseJoints.Count == 0 || !leftCal.IsTrackedWell() || !rightCal.IsTrackedWell())
        {
            return;
        }

        var baseDist = GestureHelper.CalculateOptionError(fingerBones, leftCal, rightCal, currentGesture.baseJoints.ToArray());
        var averageDist = baseDist / currentGesture.baseJoints.Count;
        StartCoroutine(network.Set("/stats/threshold", "value", averageDist));

        if (shouldSave && averageDist < baseThreshold)
        {
            shouldSave = false;
            SaveOption();

            index += 1;
            if (index == 2)
            {
                index = 0;
                var order = new GestureType[1];
                order[0] = (GestureType)(((int)currentGesture.type % 3) + 1);
                SetOrder(order);
            }
            QuestDebug.Instance.Log("set index to: " + index, true);
            OnIndexChange.Invoke(index);
            SetAnimationType();
        }
        else
        {
            SetAnimationType(false);
        }

        // var bones = fingerBones.Select((f) => f.Value.Save()).ToArray();

        // snapshots.Add(new JointCollection(bones));
        // if (snapshots.Count > queueSize)
        // {
        //     var result = FindImportantBones(snapshots.ToArray());
        //     foreach (var bone in result.Item1)
        //     {
        //         var finger = fingerBones[bone.ToID()];
        //         if (bone.isLeft)
        //         {
        //             leftCal.DebugPosition(finger.GetAbsolutePosition(), Color.green);
        //             continue;
        //         }
        //         rightCal.DebugPosition(finger.GetAbsolutePosition(), Color.green);
        //     }
        //     snapshots = new List<JointCollection>();
        //     delay = 5f;
        // }

        // var info = LeftHand.GetCurrentAnimatorStateInfo(0);
        // RightHand.Play(info.fullPathHash, 0, info.normalizedTime);
        // RightHand.SetLayerWeight(0, );
    }

    private void SetAnimationType(bool changed = true)
    {
        var clipInfo = RightHand.GetCurrentAnimatorClipInfo(0);
        var info = RightHand.GetCurrentAnimatorStateInfo(0);

        if (clipInfo.Length == 0) { return; }

        bool init = info.length > time;
        bool isAtStart = (info.normalizedTime % info.length) < 0.1f && !init;
        bool isAtEnd = !isAtStart && info.length - (info.normalizedTime % info.length) < 0.1;
        bool enableLeft = currentGesture.type == GestureType.TriangleGesture;

        if ((index == 0 && isAtStart) || (index == 1 && isAtEnd))
        {
            RightHand.enabled = init;
            LeftHand.enabled = init;
            return;
        }

        if (changed)
        {
            // QuestDebug.Instance.Log("init: " + init + " isAtStart: " + isAtStart + ", isAtEnd: " + isAtEnd + " norm time: " + info.normalizedTime + " len: " + info.length, true);
            LeftHand.gameObject.SetActive(enableLeft);
            RightHand.enabled = true;
            LeftHand.enabled = enableLeft;
            RightHand.SetInteger("state", (int)currentGesture.type);
            LeftHand.SetInteger("state", (int)currentGesture.type);

            var anim = "Base Layer." + currentGesture.name;
            if (isAtEnd || init)
            {
                RightHand.Play(anim, 0, 0);
                LeftHand.Play(anim, 0, 0);
            }
        }
    }

    private void SaveOption()
    {
        if (Application.isEditor)
        {
            return;
        }
        QuestDebug.Instance.Log("important joints: " + string.Join(", ", currentGesture.importantJoints.Select(s => s.ToID())), true);
        JointCollection col = new JointCollection(new Bone[currentGesture.importantJoints.Length]);
        for (int i = 0; i < currentGesture.importantJoints.Length; i++)
        {
            var importantBone = currentGesture.importantJoints[i];
            var bone = fingerBones[importantBone.ToID()].Save();
            col.joints[i] = bone;
            QuestDebug.Instance.Log(importantBone.boneId + ": " + bone.position.ToString(), true);
            // bone.calibrator.DebugPosition(bone.Hand.transform.TransformPoint(bone.position), Color.green, 0.005f);
        }
        var variant = currentGesture.variants.Find(v => v.index == index);
        Assert.IsNotNull(variant);
        LogDifferenceToExcistingOptions(variant.options, col);
        variant.options.Add(col);
        var url = "/gesture/addOption/" + (int)currentGesture.type + "/" + index;
        var json = JsonUtility.ToJson(col);
        StartCoroutine(network.Post(json, url));
        QuestDebug.Instance.Log("new variant saved", true);
    }

    private void LogDifferenceToExcistingOptions(List<JointCollection> options, JointCollection col)
    {
        var sorted = new SortedList<string, Bone>();
        foreach (var bone in col.joints)
        {
            sorted.Add(bone.ToID(), bone);
        }
        float minDist = float.PositiveInfinity;
        foreach (var item in options)
        {
            var error = GestureHelper.CalculateOptionError(sorted, leftCal, rightCal, item.joints);
            minDist = Math.Min(minDist, error);
        }
        QuestDebug.Instance.Log("Distance to existing options: " + minDist, true);
    }

    private Tuple<Bone[], Bone[]> FindImportantBones(JointCollection[] options)
    {
        Dictionary<string, List<Vector3>> positions = new Dictionary<string, List<Vector3>>();
        foreach (var opt in options)
        {
            foreach (var joint in opt.joints)
            {
                if (currentGesture.ignoreLeft && joint.isLeft)
                {
                    continue;
                }
                if (currentGesture.ignoreRight && !joint.isLeft)
                {
                    continue;
                }
                string id = joint.ToID();
                List<Vector3> pos;
                if (positions.ContainsKey(id))
                {
                    positions.TryGetValue(id, out pos);
                }
                else
                {
                    pos = new List<Vector3>();
                    positions.Add(id, pos);
                }

                pos.Add(joint.position);
            }
        }

        var distances = new Dictionary<string, float>();
        foreach (var joint in positions)
        {
            float dist = 0f;
            var pos = joint.Value;
            var boneId = Bone.FromID(joint.Key, leftCal, rightCal).boneId;
            leftCal.WeightDict.TryGetValue(boneId, out float weight);
            for (int i = 1; i < pos.Count; i++)
            {
                Assert.IsFalse(pos[i - 1] == pos[i]);
                dist += Vector3.Distance(pos[i - 1], pos[i]);
            }
            distances.Add(joint.Key, dist);
        }

        var cutoff = distances.ToArray().OrderBy((v) => v.Value).Last().Value * 0.2f;

        var important = new List<Bone>();
        var baseJoints = new List<Bone>();
        foreach (var joint in distances)
        {
            var bone = Bone.FromID(joint.Key, leftCal, rightCal);
            if (joint.Value > cutoff)
            {
                important.Add(bone);
            }
            else
            {
                var val = positions[joint.Key];
                bone.position = ComputeMean(val.ToArray());
                baseJoints.Add(bone);
            }
        }

        var impJoints = new JointCollection(important.ToArray());
        var bJoints = new JointCollection(baseJoints.ToArray());
        var json = JsonUtility.ToJson(impJoints).Replace("joints", "importantJoints").TrimStart('{').Replace(",\"position\":{\"x\":0.0,\"y\":0.0,\"z\":0.0}", "");
        json = json.Substring(0, json.Length - 2);
        json += ",";
        json += JsonUtility.ToJson(bJoints).Replace("joints", "baseJoints").TrimStart('{');
        json = json.Substring(0, json.Length - 1);
        json += "\n\n\n";
        QuestDebug.Instance.Log(json, true);
        return new Tuple<Bone[], Bone[]>(important.ToArray(), baseJoints.ToArray());
    }

    private Vector3 ComputeMean(Vector3[] cloud)
    {
        var mean = Vector3.zero;

        for (int i = 0; i < cloud.Length; i++)
        {
            mean += cloud[i];
        }

        return mean / cloud.Length;
    }

    public void SetOrder(GestureType[] order)
    {
        if (order.Length == 0 || order[0] == GestureType.Default)
        {
            return;
        }
        currentGesture = savedGestures.Find(g => g.type == order[0]);
        time = 0;
        SetAnimationType();
        Assert.IsNotNull(currentGesture, string.Join(", ", savedGestures.Select(s => s.type)) + " ==> " + order[0]);
        QuestDebug.Instance.Log("set gesture to: " + currentGesture.name, true);
        OnTypeChange.Invoke(currentGesture.type);
    }

    public GestureType GetCurrentGesture()
    {
        if (currentGesture == null)
        {
            return GestureType.Default;
        }

        return currentGesture.type;
    }
}

