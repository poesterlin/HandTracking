using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
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
    public Vector3 playerStartPosition = new Vector3(-11.8f, 1f, 18.25f);
    public UnityEvent<GestureType> OnStateChange = new UnityEvent<GestureType>();
    private NetworkAdapter network;
    private Vector3 startPos;
    private int state = 0;
    private GestureType[] typeArray = { GestureType.FingerGesture, GestureType.PalmGesture, GestureType.TriangleGesture };
    private readonly List<TeleportRecord> records = new List<TeleportRecord>();

    void Start()
    {
        network = new NetworkAdapter();

        // mortar.OnPotion.AddListener(UpdateState);
        // teleportProvider.OnTeleport.AddListener(AddRecord);

        Shuffle(typeArray);
        Debug.Log(String.Join(", ", typeArray));
        SetTeleporterState(typeArray[state]);

        startPos = player.transform.position;
        AddRecord(startPos);
    }

    void Update()
    {
        if (records.Count > 0)
        {
            records.First().delayToNext += Time.deltaTime;
        }
    }
    void AddRecord(Vector3 newPos)
    {
        if (records.Count > 0)
        {
            SendStats();
            var prev = records.First();
            prev.distance = Vector3.Distance(prev.position, newPos);
        }
        records.Add(new TeleportRecord(typeArray[state], newPos));

    }

    void UpdateState()
    {
        // reset player position
        player.transform.position = playerStartPosition;
        player.transform.rotation = Quaternion.Euler(new Vector3(0, 0, 0));
        if (state + 1 == typeArray.Length)
        {
            SetTeleporterState(GestureType.Default);
            return;
        }
        state += 1;
        SetTeleporterState(typeArray[state]);
    }

    void SetTeleporterState(GestureType type)
    {
        // OnStateChange.Invoke(type);
        // recognizer.AllowedType = type;
    }

    void SendStats()
    {
        var json = JsonUtility.ToJson(records.First());
        Debug.Log(json);
        StartCoroutine(network.Set("/stats/teleportRecord", "teleport", json));
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
