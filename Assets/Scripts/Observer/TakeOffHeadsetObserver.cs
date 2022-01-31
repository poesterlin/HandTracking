using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TakeOffHeadsetObserver : StudyObserver
{
    public override GestureType GetCurrentGesture()
    {
        return GestureType.Default;
    }
}
