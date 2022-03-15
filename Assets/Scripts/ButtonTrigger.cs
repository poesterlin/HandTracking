using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class ButtonTrigger : MonoBehaviour
{

    public Color hoverColor;
    public Color startColor;
    public UnityEvent onButtonPressed = new UnityEvent();
    private bool pressing = false;
    private Material material;
    void Start()
    {
        material = GetComponent<MeshRenderer>().material;
        SetColor(startColor);
    }

    private void SetColor(Color color)
    {
        material.color = color;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!pressing)
        {
            pressing = true;
            SetColor(hoverColor);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (pressing)
        {
            pressing = false;
            onButtonPressed.Invoke();
            SetColor(startColor);
        }
    }
}
