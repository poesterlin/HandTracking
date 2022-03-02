using UnityEngine;

public interface IStudyObserver
{
    public void SetOrder(GestureType[] order);

    public GestureType GetCurrentGesture();
}
