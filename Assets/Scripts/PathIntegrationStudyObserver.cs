using UnityEngine;
using UnityEngine.Events;

public class PathIntegrationStudyObserver : MonoBehaviour
{
    public TeleportProvider teleportProvider;
    public GestureRecognizer recognizer;
    public OVRPlayerController player;
    public UnityEvent<GestureType> OnStateChange = new UnityEvent<GestureType>();
    private NetworkAdapter network;
    private Vector3 startPos;

    private int state = 0;
    private GestureType[] typeArray = { GestureType.FingerGesture, GestureType.PalmGesture, GestureType.TriangleGesture };

    void Start()
    {
        network = new NetworkAdapter();

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
        OnStateChange.Invoke(typeArray[state]);
        // recognizer.AllowedType = typeArray[state];
    }

    void SendDistance(float distance)
    {
        StartCoroutine(network.Set("/stats/distance", "distance", distance, "type", (int)typeArray[state]));
    }
    void StudyDone()
    {
        QuestDebug.Instance.Log("The study is completed");
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
