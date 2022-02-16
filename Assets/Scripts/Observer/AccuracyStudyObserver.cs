using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class AccuracyStudyObserver : StudyObserver
{

    public TeleportProvider teleportProvider;
    public GestureRecognizer recognizer;
    public OVRPlayerController player;
    public UnityEvent<GestureType> OnStateChange = new UnityEvent<GestureType>();
    public Vector3[] targets = new Vector3[5];
    public GameObject Target;
    public GameObject Obstacle;
    public int obstacleNr = 20;
    public int obstacleGrid = 20;
    private GameObject[] obstacles;
    public Vector3 obstacleStart = new Vector3(0, 1, 0);
    public float obstacleDist = 1.77f;

    private NetworkAdapter network;
    private Vector3 startPos;

    private int state = 0;
    private GestureType[] typeArray;
    private GameObject[] targetObj;
    private int targetIdx = -1;

    void Start()
    {
        network = new NetworkAdapter();

        teleportProvider.OnTeleport.AddListener(CheckCollision);
        startPos = player.transform.position;
        StartCoroutine(network.GetOrder(this));
        MakeTargets();
        SelectNextTarget();
    }

    void MakeObstacles()
    {
        obstacles = new GameObject[obstacleNr];
        Vector3 pos = obstacleStart;
        for (int i = 0; i < obstacleNr; i++)
        {
            pos = new Vector3(i % obstacleGrid == 0 ? obstacleStart.x : pos.x + obstacleDist, pos.y, i % obstacleGrid == 0 ? pos.z + obstacleDist : pos.z);
            obstacles[i] = Instantiate(Obstacle, pos, Quaternion.identity);
        }
    }

    void MakeTargets()
    {
        targetObj = new GameObject[targets.Length];
        for (int i = 0; i < targets.Length; i++)
        {
            targetObj[i] = Instantiate(Target, targets[i], Quaternion.identity);
        }
    }


    void RemoveObstacles()
    {
        for (int i = 0; i < obstacles.Length; i++)
        {
            if (Application.isEditor)
            {
                DestroyImmediate(obstacles[i]);
            }
            else
            {
                Destroy(obstacles[i]);
            }
        }
    }


    void CheckCollision(Vector3 newPos)
    {
        var pPos = player.transform.position;
        var target = targetObj[targetIdx];
        var dist = Vector3.Distance(target.transform.position, pPos);
        Debug.Log(dist + "< 0.5 * " + target.transform.localScale.x);
        if (dist < target.transform.localScale.x / 2)
        {
            var t = target.GetComponent<Target>();
            t.Focus();
        }

        for (int i = 0; i < obstacles.Length; i++)
        {
            if (Vector3.Distance(obstacles[i].transform.position, pPos) < 0.4f)
            {
                StartCoroutine(network.Set("/stats/collision", "state", state, "pos", obstacles[i].transform.position));
            }
        }
    }

    void SelectNextTarget()
    {
        targetIdx += 1;
        if (targetIdx == 6)
        {
            MakeObstacles();
        }
        if (targetIdx >= 10)
        {
            targetIdx = 0;
            RemoveObstacles();
            state += 1;
            if (state >= typeArray.Length)
            {
                SceneManager.LoadScene("TakeOffHeadset");
                return;
            }
            SetTeleporterState();
        }
        var target = targetObj[targetIdx % targetObj.Length];
        var t = target.GetComponent<Target>();
        t.Select();
        // t.Focus();
        t.OnComplete.AddListener(SelectNextTarget);
    }

    public override GestureType GetCurrentGesture()
    {
        return (GestureType)state;
    }

    public override void SetOrder(GestureType[] order)
    {
        typeArray = order;
        SetTeleporterState();
    }

    void SetTeleporterState()
    {
        OnStateChange.Invoke(typeArray[state]);
        recognizer.AllowedType = typeArray[state];
        recognizer.AbortCurrentGesture();
    }
}
