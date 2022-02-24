using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;

public abstract class Teleporter
{
    public TransporterState State { get { return _state; } }
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
    protected bool teleported = false;
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

    // ~Teleporter()
    // {
    //     Abort();
    // }

    public virtual void Abort()
    {
        Reset();
        if (reticleInstance != null)
            UnityEngine.Object.Destroy(reticleInstance);
        if (oldReticles != null)
            oldReticles.ForEach((r) =>
            {
                if (r != null)
                    UnityEngine.Object.Destroy(r);
            });
        UpdateState(TransporterState.aborted);
    }

    public virtual void Init(TrackingInfo track)
    {
        UpdateState(TransporterState.none);
    }

    public void DebugPosition(Vector3 pos)
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

    protected void UpdateState(TransporterState newState)
    {
        if (State != newState)
        {
            _state = newState;
            onSateChange.Invoke();
            QuestDebug.Instance.Log("state: " + newState, true);
        }
    }

    public virtual void Update()
    {
        Reset();
    }

    public bool HasIndex(int n) => n <= maxIndex/*  && n > index */;

    public void SetIndex(int n)
    {
        // reset teleport flag
        if (n < index)
        {
            teleported = false;
            QuestDebug.Instance.Log("reset teleport flag", true);
        }
        index = n;
        QuestDebug.Instance.Log("new index: " + index);
    }

    internal void Reset(bool keepReticle = false, int state = 0)
    {
        UpdateState(TransporterState.none);
        SetIndex(state);
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

    internal bool CanTeleport()
    {
        bool evaluation = index == maxIndex && State == TransporterState.confirmed && target != Vector3.zero;
        return evaluation && !teleported;
    }

    internal bool WasTeleported()
    {
        return teleported = true;
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
            method.Init(track);
            QuestDebug.Instance.Log("initiating teleport", true);
        }
        else
        {
            QuestDebug.Instance.Log("wrong state: InitTeleport", true);
        }
    }

    public bool UpdateAndTryTeleport(int index)
    {
        if (method == null)
        {
            return false;
        }

        if (method.HasIndex(index))
        {
            method.SetIndex(index);
        }

        // update method to calculate target and set state
        method.Update();

        var timeThresholdReached = lastTeleport.AddSeconds(teleportDelay).CompareTo(DateTime.Now) < 0;
        var distanceThresholdReached = minDistance == 0f || Vector3.Distance(method.target, player.transform.position) > minDistance;

        // method has found a target
        if (method.CanTeleport() && timeThresholdReached && distanceThresholdReached)
        {
            target = method.target;

            // reset method to ready state
            method.Reset(keepReticle, index);
            method.WasTeleported();

            var offset = Vector3.Scale(Camera.transform.position - player.transform.position, new Vector3(1, 0, 1));
            player.transform.position = new Vector3(target.x, character.height / 2.0f, target.z) - offset;

            lastTeleport = DateTime.Now;
            OnTeleport.Invoke(player.transform.position);
            return true;
        }
        else
        {
            teleportBlock = false;
            QuestDebug.Instance.Log("teleport not executed");
        }

        return false;
    }

    public void AbortTeleport()
    {
        if (method != null)
        {
            method.Abort();
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

        return method.State;
    }
}
