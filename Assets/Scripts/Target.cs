using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Target : MonoBehaviour
{
    public ParticleSystem partSys;
    public UnityEvent OnComplete = new UnityEvent();

    public void Select()
    {
        var em = partSys.emission;
        em.enabled = true;
    }

    public void Focus()
    {
        var main = partSys.main;
        main.startColor = Color.yellow;
        Invoke("OnDone", 3f);
    }

    public void OnDone()
    {
        var main = partSys.main;
        main.startColor = Color.green;
        OnComplete.Invoke();
        Invoke("Disable", 1f);
    }

    public void Disable()
    {
        OnComplete.RemoveAllListeners();
        var em = partSys.emission;
        em.enabled = false;
    }
}
