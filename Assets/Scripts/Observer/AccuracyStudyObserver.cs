using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class AccuracyStudyObserver : MonoBehaviour, IStudyObserver 
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
        recognizer.OnLoaded.AddListener(() =>
      {
          StartCoroutine(network.GetOrder(this));
      });
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
        var target = targetObj[targetIdx % targetObj.Length];
        var dist = Vector3.Distance(target.transform.position, newPos);
        var t = target.GetComponent<Target>();
        if (dist < target.transform.localScale.x)
        {
            t.OnCollision();
        }
        else if (t.isTargeted)
        {
            t.OnLostCollision();
        }



        if (obstacles == null) { return; }

        for (int i = 0; i < obstacles.Length; i++)
        {
            Assert.IsNotNull(obstacles[i]);
            if (Vector3.Distance(obstacles[i].transform.position, newPos) < 0.4f)
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
                StartCoroutine(network.Set("/stats/cb-order"));
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

    public GestureType GetCurrentGesture()
    {
        return (GestureType)state;
    }

    public void SetOrder(GestureType[] order)
    {
        typeArray = order;
        SetTeleporterState();
    }

    void SetTeleporterState()
    {
        OnStateChange.Invoke(typeArray[state]);
        recognizer.SetAllowedType(typeArray[state]);
        recognizer.AbortCurrentGesture();
    }
}
