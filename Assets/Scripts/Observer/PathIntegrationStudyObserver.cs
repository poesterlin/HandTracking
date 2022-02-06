using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class PathIntegrationStudyObserver : StudyObserver
{
    public TeleportProvider teleportProvider;
    public GestureRecognizer recognizer;
    public OVRPlayerController player;
    public UnityEvent<GestureType> OnStateChange = new UnityEvent<GestureType>();
    public float minTpDistance;
    private NetworkAdapter network;
    private Vector3 startPos;
    private bool isInBetween = false;

    private int state = 0;
    private GestureType[] typeArray;

    void Start()
    {
        network = new NetworkAdapter();

        teleportProvider.OnTeleport.AddListener(UpdateState);
        startPos = player.transform.position;
        StartCoroutine(network.GetOrder(this));
    }

    public override void SetOrder(GestureType[] order)
    {
        typeArray = order;
        SetTeleporterState();
        SendDistance(Vector3.zero, Vector3.zero);
    }

    void UpdateState(Vector3 newPos)
    {
        if (!isInBetween)
        {
            recognizer.tpProv.minDistance = minTpDistance;
            isInBetween = true;
            return;
        }

        isInBetween = false;
        recognizer.tpProv.minDistance = 0f;
        SendDistance(startPos, newPos);
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
        recognizer.AllowedType = typeArray[state];
        recognizer.AbortCurrentGesture();
        player.transform.position = startPos;
    }

    void SendDistance(Vector3 pointA, Vector3 pointB)
    {
        var distance = Vector3.Distance(pointA, pointB);
        StartCoroutine(network.Set("/stats/pathIntegration", "pointA", pointA, "pointB", pointB, "distance", distance, "type", (int)typeArray[state]));
    }

    void StudyDone()
    {
        StartCoroutine(network.Set("/stats/cb-order"));
        SceneManager.LoadScene("TakeOffHeadset");
    }

    public override GestureType GetCurrentGesture()
    {
        if (typeArray != null && typeArray.Length > state)
        {
            return typeArray[state];
        }
        return GestureType.Default;
    }
}
