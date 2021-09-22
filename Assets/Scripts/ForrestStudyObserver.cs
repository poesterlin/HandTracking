using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
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

public class ForrestStudyObserver : MonoBehaviour
{
    public TeleportProvider teleportProvider;
    public GestureRecognizer recognizer;
    public OVRPlayerController player;
    public Mortar mortar;
    public Text canvasText;
    private NetworkAdapter network;
    private Vector3 startPos;
    private int state = 0;
    private GestureType[] typeArray = { GestureType.FingerGesture, GestureType.PalmGesture, GestureType.TriangleGesture };
    private readonly List<TeleportRecord> records = new List<TeleportRecord>();

    void Start()
    {
        var server = PlayerPrefs.GetString("server");
        network = new NetworkAdapter(server);

        mortar.OnPotion.AddListener(UpdateState);
        teleportProvider.OnTeleport.AddListener(AddRecord);

        Shuffle(typeArray);
        SetTeleporterState();

        startPos = player.transform.position;
        AddRecord(startPos);
    }

    void Update()
    {
        records.First().delayToNext += Time.deltaTime;
    }

    void UpdateState()
    {
        SendStats();
        if (state + 1 == typeArray.Length)
        {
            StudyDone();
            return;
        }
        state += 1;
        SetTeleporterState();
    }

    void AddRecord(Vector3 newPos)
    {
        if (records.Count > 0)
        {
            var prev = records.First();
            prev.distance = Vector3.Distance(prev.position, newPos);
        }
        records.Add(new TeleportRecord(typeArray[state], newPos));

    }

    void SetTeleporterState()
    {
        recognizer.AllowedType = typeArray[state];
    }

    void SendStats()
    {
        var json = JsonUtility.ToJson(records.First());
        Debug.Log(json);
        StartCoroutine(network.Set("/stats/teleportRecord", "teleport", json));
    }

    void StudyDone()
    {
        Debug.Log("The study is completed");
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
