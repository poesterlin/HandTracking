using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;


public abstract class Teleporter {
    public string state { get { return _state; } }
    private string _state = "";

    public UnityEvent onSateChange = new UnityEvent();
    public UnityEvent onTeleport = new UnityEvent();

    protected LineRenderer line;
    public Vector3 target;
    protected Reticle reticle;
    protected int layerMask = 1 << 7; 
    protected float maxDistance;


    public Teleporter(float distance, LineRenderer lineRenderer, GameObject targetReticle): base(){
        maxDistance = distance;
        line = lineRenderer;
        line.enabled = false;

        var reticleInstance = Object.Instantiate(targetReticle, Vector3.zero, Quaternion.identity);
        reticle = reticleInstance.GetComponent<Reticle>();
        reticle.deactivate();
    }

    public virtual void abort(){
        Object.Destroy(reticle);
        updateState("aborting");
    }

    public virtual void init(TrackingInfo track){
        updateState("ready");
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

    protected void updateState(string newState){
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
}

public class TeleportProvider : MonoBehaviour
{
    private Teleporter method;
    private GameObject target;
    public GameObject reticle;
    public OVRPlayerController player;
    public LineRenderer line;
    public float distance;

    public void selectMethod(GestureType gesture){
        method = getType(gesture);
    }

    public Teleporter getType(GestureType gesture) =>
       gesture switch
        {
            GestureType.FingerGesture => new FingerTeleport(distance, line, reticle),
            GestureType.TriangleGesture => new TriangleTeleport(distance, line, reticle),
            _ => null,
        };

    public void initTeleport(TrackingInfo track){
        if(method != null){
            method.init(track);
            QuestDebug.Instance.Log("initiating teleport");
        }

        Invoke("confirmTeleport", 2);
    }

    public void confirmTeleport(){
        if(method != null && method.state == "avaliable"){
            player.enabled = false;
            player.transform.position = method.target + new Vector3(0, player.transform.position.y, 0);
            player.enabled = true;
        }
        Invoke("confirmTeleport", 2);
    }

    public void updateTeleport(){
        if(method != null){
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
