using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class AnimationEvents : MonoBehaviour
{

    public UnityEvent<int> OnStateChange = new UnityEvent<int>();

    public void OnHandAnimationStateChange(int state)
    {
        OnStateChange.Invoke(state);
    }
}
