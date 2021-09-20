using System;
using UnityEngine;
using UnityEngine.Events;

public abstract class Teleporter
{
    public TransporterState state { get { return _state; } }
    private TransporterState _state = TransporterState.none;

    public UnityEvent onSateChange = new UnityEvent();
    public UnityEvent onTeleport = new UnityEvent();

    protected LineRenderer line;
    public Vector3 target;
    protected GameObject reticleInstance;
    protected Reticle reticle;
    protected int layerMask = 1 << 7;
    protected float maxDistance;
    protected int index = 0;
    protected int maxIndex = 0;

    public Teleporter(float distance, LineRenderer lineRenderer, GameObject targetReticle) : base()
    {
        maxDistance = distance;
        line = lineRenderer;
        line.enabled = false;

        reticleInstance = UnityEngine.Object.Instantiate(targetReticle, Vector3.zero, Quaternion.identity);
        reticle = reticleInstance.GetComponent<Reticle>();
        reticle.deactivate();
    }

    public virtual void abort()
    {
        reset();
        UnityEngine.Object.Destroy(reticleInstance);
        updateState(TransporterState.aborted);
    }

    public virtual void init(TrackingInfo track)
    {
        updateState(TransporterState.ready);
    }

    public virtual void confirmTeleport()
    {
        onTeleport.Invoke();
    }

    public void debugPosition(Vector3 pos)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        obj.transform.position = pos;
        obj.transform.localScale = 0.05f * Vector3.one;
        obj.GetComponent<Collider>().enabled = false;
    }

    protected void updateState(TransporterState newState)
    {
        if (state != newState)
        {
            _state = newState;
            onSateChange.Invoke();
            QuestDebug.Instance.Log("state: " + newState);
        }
    }

    public virtual void update()
    {
        reset();
    }

    public bool hasIndex(int n) => n <= maxIndex && n > index;

    public void setIndex(int n)
    {
        index = n;
        // QuestDebug.Instance.Log("new index: " + index);
    }

    internal void reset()
    {
        updateState(TransporterState.ready);
        setIndex(0);
        target = Vector3.zero;
        line.enabled = false;
        reticle.deactivate();
    }
}

public class TeleportProvider : MonoBehaviour
{
    public GameObject reticle;
    public GameObject portal;
    public OVRPlayerController player;
    public CharacterController character;
    public LineRenderer line;
    private Teleporter method;
    public float distance;
    public double teleportDelay = 0.7;

    public UnityEvent<Vector3> OnTeleport = new UnityEvent<Vector3>();

    public bool freeTeleport = true;
    public bool updateMapStat = true;
    private bool teleportBlock;

    private NetworkAdapter network;
    private Vector3 target;
    private DateTime lastTeleport = DateTime.Now;
    private GestureType[] _allowedTypes = new GestureType[0];

    public GestureType[] AllowedTypes
    {
        set { _allowedTypes = value; }
    }

    public void Start()
    {
        network = new NetworkAdapter(PlayerPrefs.GetString("server"));
    }

    public void selectMethod(GestureType gesture)
    {
        if (method != null)
        {
            abortTeleport();
        }
        if (_allowedTypes.Length != 0 && Array.Exists(_allowedTypes, g => g == gesture))
        {
            method = getType(gesture);
        }
    }

    public Teleporter getType(GestureType gesture) =>
       gesture switch
       {
           GestureType.FingerGesture => new FingerTeleport(distance, line, reticle),
           GestureType.PalmGesture => new PalmTeleport(distance, line, reticle),
           GestureType.TriangleGesture => new TriangleTeleport(distance, line, reticle),
           _ => null,
       };

    public void initTeleport(TrackingInfo track)
    {
        if (method != null)
        {
            method.init(track);
            QuestDebug.Instance.Log("initiating teleport");
        }
    }

    private void confirmTeleport()
    {
        player.transform.position = new Vector3(target.x, character.height / 2.0f, target.z);
        player.Teleported = true;
        player.enabled = true;
        character.enabled = true;

        teleportBlock = false;
        if (updateMapStat)
        {
            StartCoroutine(network.Set("/stats/position", "x", target.x, "y", target.z));
        }
        lastTeleport = DateTime.Now;
        OnTeleport.Invoke(player.transform.position);
    }

    public bool updateAndTryTeleport(int index)
    {
        if (method == null && !teleportBlock)
        {
            return false;
        }

        if (method.hasIndex(index))
        {
            method.setIndex(index);
        }

        // update method to calculate target and set state
        method.update();

        // method has found a target
        if (method.state == TransporterState.avaliable && lastTeleport.AddSeconds(teleportDelay).CompareTo(DateTime.Now) < 0)
        {
            // block teleport call until current lock is released
            teleportBlock = true;

            // disable controll
            player.enabled = false;
            character.enabled = false;

            target = method.target;

            // reset method to ready state
            method.reset();

            Invoke("confirmTeleport", 0.5f);
            return true;
        }

        return false;
    }

    public void abortTeleport()
    {
        if (method != null)
        {
            method.abort();
            method = null;
            QuestDebug.Instance.Log("aborting teleport");
        }
    }
}
