using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.PlayerLoop;

public class Target : MonoBehaviour
{
    public ParticleSystem partSys;
    public GameObject column;
    public UnityEvent OnComplete = new UnityEvent();
    public bool isTargeted = false;
    public float counter = 0;

    public void Select()
    {
        var em = partSys.emission;
        em.enabled = true;
        column.SetActive(true);
    }

    void Update()
    {
        if (isTargeted)
        {
            counter += Time.deltaTime;
            if (counter > 3f)
            {
                OnDone();
            }
        }

    }

    public void OnCollision()
    {
        var main = partSys.main;
        main.startColor = Color.yellow;
        var col = new Color(0, 140f / 255f, 1f, 0.2f);
        column.GetComponent<Renderer>().material.color = col;
        isTargeted = true;
    }

    public void OnLostCollision()
    {
        // TODO: log lost 
        isTargeted = false;
        counter = 0;
    }

    public void OnDone()
    {
        var main = partSys.main;
        main.startColor = Color.green;
        var col = new Color(0, 1, 0, 0.2f);
        column.GetComponent<Renderer>().material.color = col;
        OnComplete.Invoke();
        Invoke("Disable", 1f);
        isTargeted = false;
    }

    public void Disable()
    {
        OnComplete.RemoveAllListeners();
        var em = partSys.emission;
        em.enabled = false;
        column.SetActive(false);
    }
}
