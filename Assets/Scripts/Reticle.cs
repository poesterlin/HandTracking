using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Reticle : MonoBehaviour
{

    public Renderer child;

    public void activate(){
        setState(true);
    }

    public void deactivate(){
        setState(false);
    }

    private void setState(bool state){
        child.enabled = state;
    }

}
