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

    public virtual void init(OVRSkeleton hand, Bone finger){
        updateState("ready");
    }

    public virtual void confirmTeleport(){
        onTeleport.Invoke();
    }

    protected void updateState(string newState){
        _state = newState;
        onSateChange.Invoke();
        QuestDebug.Instance.Log("state: " + newState);
    }

    public virtual void update(){
        QuestDebug.Instance.Log("base");
    }
}


public class FingerTeleport: Teleporter {
    private Bone anchorF;
    private OVRSkeleton anchorH;

    public FingerTeleport(float distance, LineRenderer line, GameObject targetReticle): base(distance, line, targetReticle) {
        QuestDebug.Instance.Log("creating new FingerTeleporter");
    }

    public override void init(OVRSkeleton hand, Bone finger){
        anchorF = finger;
        anchorH = hand;
        base.init(hand, finger);
    }

    public override void abort(){
        base.abort();
        anchorF = null;
        anchorH = null;
    }

    public override void update(){
        RaycastHit hit;
        Vector3 start = anchorH.transform.position;
        if (Physics.Raycast(start, anchorF.Finger.Transform.TransformDirection(Vector3.right), out hit, maxDistance /*, layerMask */)) {
            target = hit.point;
            updateState("avaliable");

            line.SetPosition(0, start);
            line.SetPosition(1, target);

            reticle.activate();
            reticle.transform.position = target;    
            line.enabled = true;
        } else {
            target = Vector3.zero;
            updateState("no point");
            line.enabled = false;
            reticle.deactivate();
        }
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
            GestureType.TriangleGesture => new FingerTeleport(distance, line, reticle),
            _ => null,
        };

    public void initTeleport(OVRSkeleton hand, Bone finger){
        if(method != null){
            method.init(hand, finger);
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
