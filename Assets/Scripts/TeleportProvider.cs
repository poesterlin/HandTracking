using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;


public abstract class Teleporter {
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


    public Teleporter(float distance, LineRenderer lineRenderer, GameObject targetReticle): base(){
        maxDistance = distance;
        line = lineRenderer;
        line.enabled = false;

        reticleInstance = Object.Instantiate(targetReticle, Vector3.zero, Quaternion.identity);
        reticle = reticleInstance.GetComponent<Reticle>();
        reticle.deactivate();
    }

    public virtual void abort(){
        Object.Destroy(reticleInstance);
        updateState(TransporterState.aborted);
    }

    public virtual void init(TrackingInfo track){
        updateState(TransporterState.ready);
    }

    public virtual void confirmTeleport(){
        onTeleport.Invoke();
    }

    protected void debugPosition(Vector3 pos){
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        obj.transform.position = pos;
        obj.transform.localScale = 0.05f * Vector3.one;
        obj.GetComponent<Collider>().enabled = false;
    }

    protected void updateState(TransporterState newState){
        _state = newState;
        onSateChange.Invoke();
        QuestDebug.Instance.Log("state: " + newState);
    }

    public virtual void update(){
        target = Vector3.zero;
        // updateState("no point");
        line.enabled = false;
        reticle.deactivate();
    }

    public bool hasIndex(int n) => n <= maxIndex && n > index;

    public void setIndex(int n){
        index = n;
        QuestDebug.Instance.Log("new index: " + index);
    }
}

public class TeleportProvider : MonoBehaviour
{
    private Teleporter method;
    private GameObject target;
    public GameObject reticle;
    public GameObject portal;
    public OVRPlayerController player;
    public LineRenderer line;
    public float distance;

    public void selectMethod(GestureType gesture){
        if(method != null){
            abortTeleport();
        }
        method = getType(gesture);
    }

    public Teleporter getType(GestureType gesture) =>
       gesture switch
        {
            GestureType.FingerGesture => new FingerTeleport(distance, line, reticle),
            GestureType.TriangleGesture => new TriangleTeleport(distance, line, reticle),
            GestureType.PortalGesture => new PortalTeleport(distance, line, reticle, portal),
            _ => null,
        };

    public void initTeleport(TrackingInfo track){
        if(method != null){
            method.init(track);
            QuestDebug.Instance.Log("initiating teleport");
        }

        // Invoke("confirmTeleport", 2);
    }

    public void confirmTeleport(){
        if(method != null && method.state == TransporterState.avaliable){
            player.enabled = false;
            player.transform.position = method.target + new Vector3(0, player.transform.position.y, 0);
            player.enabled = true;
        }
        Invoke("confirmTeleport", 2);
    }

    public void updateTeleport(int index){
        if(method != null){
            if(method.hasIndex(index)){
                method.setIndex(index);
            }
            method.update();
        }
    }

    public void abortTeleport(){
        if(method != null){
            method.abort();
            method = null;
            QuestDebug.Instance.Log("aborting teleport");
        }
    }
}
