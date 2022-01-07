using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[Serializable]
public class TeleportRecord
{
    public float delayToNext = 0;
    public float distance;
    public DateTime recorded = new DateTime();
    public GestureType type;
    public Vector3 position;

    public TeleportRecord(GestureType type, Vector3 pos)
    {
        this.type = type;
        position = pos;
    }
}

[Serializable]
public class CounterBallanceRecord
{
    public GestureType[] Order;

    public static CounterBallanceRecord CreateFromJSON(string jsonString)
    {
        return JsonUtility.FromJson<CounterBallanceRecord>(jsonString);
    }
}

public class ForrestStudyObserver : StudyObserver
{
    public TeleportProvider teleportProvider;
    public GestureRecognizer recognizer;
    public OVRPlayerController player;
    public Mortar mortar;
    public Vector3 playerStartPosition = new Vector3(-11.8f, 0f, 18.25f);
    public UnityEvent<GestureType> OnStateChange = new UnityEvent<GestureType>();
    public GestureType[] typeArray;
    private NetworkAdapter network;
    private Vector3 startPos;
    public Quaternion startDir;
    private int state = 0;
    private readonly List<TeleportRecord> records = new List<TeleportRecord>();

    void Start()
    {
        network = new NetworkAdapter();

        mortar.OnPotion.AddListener(UpdateState);
        teleportProvider.OnTeleport.AddListener(AddRecord);

        StartCoroutine(network.GetOrder(this));
        startPos = player.transform.position;
        AddRecord(startPos);
        ResetPosition();
    }

    public override void SetOrder(GestureType[] order)
    {
        typeArray = order;
        SetTeleporterState(typeArray[state]);
    }

    void Update()
    {
        if (records.Count > 0)
        {
            records.Last().delayToNext += Time.deltaTime;
        }

        if (OVRInput.GetDown(OVRInput.Button.Start))
        {
            SceneManager.LoadScene("PathIntegration");
        }
    }

    void AddRecord(Vector3 newPos)
    {
        if (records.Count > 0)
        {
            SendStats();
            var prev = records.Last();
            prev.distance = Vector3.Distance(prev.position, newPos);
        }
        records.Add(new TeleportRecord(typeArray[state], newPos));
        QuestDebug.Instance.Log(String.Join(",", records.ConvertAll((r) => "dist: " + r.distance + " delay: " + r.delayToNext)), true);
    }

    public void EnableTeleport()
    {
        recognizer.disabled = false;
    }
    public void DisableTeleport()
    {
        recognizer.disabled = true;
    }

    public void UpdateState()
    {
        teleportProvider.enabled = false;
        Invoke("ResetPosition", 1.5f);
        if (state + 1 == typeArray.Length)
        {
            SetTeleporterState(GestureType.Default);
            StartCoroutine(network.Set("/stats/cb-order"));
            return;
        }
        state += 1;
        SetTeleporterState(typeArray[state]);
    }

    void ResetPosition()
    {
        player.transform.position = playerStartPosition;
        player.transform.rotation = startDir;
        teleportProvider.enabled = true;
    }

    void SetTeleporterState(GestureType type)
    {
        OnStateChange.Invoke(type);
        recognizer.AllowedType = type;
    }

    void SendStats()
    {
        var json = JsonUtility.ToJson(records.Last());
        QuestDebug.Instance.Log(json);
        StartCoroutine(network.Set("/stats/teleportRecord", "teleport", json));
    }
}
