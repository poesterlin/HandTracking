using UnityEngine;

public class PathIntegrationStudyObserver : MonoBehaviour
{
    public TeleportProvider teleportProvider;
    public GestureRecognizer recognizer;
    public OVRPlayerController player;
    private NetworkAdapter network;
    private Vector3 startPos;

    private int state = 0;
    private GestureType[] typeArray = { GestureType.FingerGesture, GestureType.PalmGesture, GestureType.TriangleGesture };

    void Start()
    {
        var server = PlayerPrefs.GetString("server");
        network = new NetworkAdapter(server);

        teleportProvider.OnTeleport.AddListener(UpdateState);
        startPos = player.transform.position;
        Shuffle(typeArray);
        SetTeleporterState();
    }

    void UpdateState(Vector3 newPos)
    {
        SendDistance(Vector3.Distance(startPos, newPos));
        if (state + 1 == typeArray.Length)
        {
            StudyDone();
            return;
        }
        startPos = newPos;
        state += 1;
        SetTeleporterState();
    }

    void SetTeleporterState()
    {
        // recognizer.AllowedType = typeArray[state];
    }

    void SendDistance(float distance)
    {
        StartCoroutine(network.Set("/stats/distance", "distance", distance, "type", (int)typeArray[state]));
    }
    void StudyDone()
    { 
        Debug.Log("The study is completed");
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
