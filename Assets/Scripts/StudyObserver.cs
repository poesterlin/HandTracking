using UnityEngine;

public class StudyObserver : MonoBehaviour
{
    public string server = "https://bpi.oesterlin.dev";
    public TeleportProvider teleportProvider;
    public OVRPlayerController player;
    private NetworkAdapter network;
    private Vector3 startPos;

    private int state = 0;
    private GestureType[] typeArray = { GestureType.FingerGesture, GestureType.PalmGesture, GestureType.TriangleGesture };

    void Start()
    {
        network = new NetworkAdapter(server);

        teleportProvider.OnTeleport.AddListener(UpdateState);
        startPos = player.transform.position;
        Shuffle(typeArray);
        SetTeleporterState();
    }

    void UpdateState(Vector3 newPos)
    {
        SendDistance(Vector3.Distance(startPos, newPos));
        startPos = newPos;
        state += 1;
        SetTeleporterState();
    }

    void SetTeleporterState()
    {
        var allowedArr = new GestureType[1];
        allowedArr[0] = typeArray[state];
        teleportProvider.AllowedTypes = allowedArr;
    }

    void SendDistance(float distance)
    {
        StartCoroutine(network.Set("/stats/distance", "distance", distance, "type", (int)typeArray[state]));
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
