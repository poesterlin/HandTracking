using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    protected ProjectileSimulator simulator;
    protected List<GameObject> oldReticles = new List<GameObject>();

    public Teleporter(float distance, ProjectileSimulator sim, LineRenderer lineRenderer, GameObject targetReticle) : base()
    {
        maxDistance = distance;
        simulator = sim;
        line = lineRenderer;
        line.enabled = false;

        reticleInstance = UnityEngine.Object.Instantiate(targetReticle, Vector3.zero, Quaternion.identity);
        reticle = reticleInstance.GetComponent<Reticle>();
        reticle.deactivate();
    }

    ~Teleporter()
    {
        abort();
    }

    public virtual void abort()
    {
        reset();
        UnityEngine.Object.Destroy(reticleInstance);
        oldReticles.ForEach((r) =>
        {
            UnityEngine.Object.Destroy(r);
        });
        updateState(TransporterState.aborted);
    }

    public virtual void init(TrackingInfo track)
    {
        updateState(TransporterState.none);
    }

    public void debugPosition(Vector3 pos)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        obj.transform.position = pos;
        obj.transform.localScale = 0.05f * Vector3.one;
        obj.GetComponent<Collider>().enabled = false;
    }

    public bool SimulateProjectile(Vector3 pos, Vector3 force)
    {
        var trajectory = simulator.Predict(pos, force * 6, out bool hit);
        var last = trajectory.Last();

        if (!hit || Vector3.Distance(pos, last) > maxDistance)
        {
            return false;
        }

        UpdateTarget(last);

        trajectory = simulator.Skew(trajectory, target);

        line.positionCount = trajectory.Count + 1;
        line.SetPosition(0, pos);
        for (int i = 0; i < trajectory.Count; i++)
        {
            line.SetPosition(i + 1, trajectory[i]);
        }
        return hit;
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

    public bool hasIndex(int n) => n <= maxIndex/*  && n > index */;

    public void setIndex(int n)
    {
        index = n;
        QuestDebug.Instance.Log("new index: " + index);
    }

    internal void reset(bool keepReticle = false)
    {
        updateState(TransporterState.none);
        setIndex(0);
        target = Vector3.zero;
        line.enabled = false;
        if (keepReticle)
        {
            oldReticles.Add(reticleInstance);
            reticleInstance = UnityEngine.Object.Instantiate(reticleInstance, Vector3.zero, Quaternion.identity);
            reticle = reticleInstance.GetComponent<Reticle>();
        }
        reticle.deactivate();
    }

    internal void UpdateTarget(Vector3 newTarget, float smoothing = 0.1f)
    {
        if (target == Vector3.zero)
        {
            target = newTarget;
        }
        else
        {
            target = Vector3.Lerp(target, newTarget, smoothing);
        }
        reticle.transform.position = target;
    }
}

public class TeleportProvider : MonoBehaviour
{
    public GameObject reticle;
    public ProjectileSimulator sim;
    public OVRPlayerController player;
    public GameObject Camera;
    public CharacterController character;
    public LineRenderer line;
    private Teleporter method;
    public float distance;
    public double teleportDelay = 0.7;

    public UnityEvent<Vector3> OnTeleport = new UnityEvent<Vector3>();
    public UnityEvent OnAbort = new UnityEvent();

    public bool keepReticle = false;

    private bool teleportBlock;

    private Vector3 target = Vector3.zero;
    private DateTime lastTeleport = DateTime.Now;

    public float minDistance = 0f;

    public void SelectMethod(GestureType gesture)
    {
        AbortTeleport();
        method = GetType(gesture);
    }

    public Teleporter GetType(GestureType gesture) =>
       gesture switch
       {
           GestureType.FingerGesture => new FingerTeleport(distance, sim, line, reticle),
           GestureType.PalmGesture => new PalmTeleport(distance, sim, line, reticle),
           GestureType.TriangleGesture => new TriangleTeleport(distance, sim, line, reticle),
           _ => null,
       };

    public void InitTeleport(TrackingInfo track)
    {
        if (method != null)
        {
            method.init(track);
            QuestDebug.Instance.Log("initiating teleport", true);
        }
        else
        {
            QuestDebug.Instance.Log("wrong state: InitTeleport", true);
        }
    }

    private IEnumerator ConfirmTeleport()
    {
        if (teleportBlock)
        {
            yield return new WaitForSeconds(0.1f);
            var offset = Vector3.Scale(Camera.transform.position - player.transform.position, new Vector3(1, 0, 1));
            player.transform.position = new Vector3(target.x, character.height / 2.0f, target.z) - offset;
            player.Teleported = true;
            player.enabled = true;
            // character.enabled = true;

            teleportBlock = false;
            lastTeleport = DateTime.Now;
            OnTeleport.Invoke(player.transform.position);
        }
    }

    public bool UpdateAndTryTeleport(int index)
    {
        if (method == null || teleportBlock)
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
        if (method.state == TransporterState.confirmed && lastTeleport.AddSeconds(teleportDelay).CompareTo(DateTime.Now) < 0 && (minDistance == 0f || Vector3.Distance(method.target, player.transform.position) > minDistance))
        {
            // block teleport call until current lock is released
            teleportBlock = true;

            // disable controll
            player.enabled = false;
            character.enabled = false;

            target = method.target;

            // reset method to ready state
            method.reset(keepReticle);

            StartCoroutine(ConfirmTeleport());
            return true;
        }
        else
        {
            QuestDebug.Instance.Log("teleport not executed");
        }

        return false;
    }

    public void AbortTeleport()
    {
        if (method != null)
        {
            method.abort();
            method = null;
            QuestDebug.Instance.Log("aborting teleport", true);
            OnAbort.Invoke();
        }
    }

    public bool IsMethodSet()
    {
        return method != null;
    }

    public TransporterState GetCurrentTeleporterState()
    {
        if (method == null)
        {
            return TransporterState.none;
        }

        return method.state;
    }
}
