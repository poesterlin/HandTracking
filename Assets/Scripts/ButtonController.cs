using UnityEngine;
using UnityEngine.Events;

public class ButtonController : MonoBehaviour
{
    public UnityEvent onButtonPressed = new UnityEvent();

    void Start()
    {
        var trigger = GetComponentInChildren<ButtonTrigger>();
        trigger.onButtonPressed.AddListener(() =>
        {
            onButtonPressed.Invoke();
        });

        
    }
}