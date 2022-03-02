using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TakeOffHeadsetObserver : MonoBehaviour, IStudyObserver
{
    public GestureType GetCurrentGesture()
    {
        return GestureType.Default;
    }

    public void SetOrder(GestureType[] order)
    {
    }
}
