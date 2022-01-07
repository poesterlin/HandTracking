using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.UI;

public class Tutorial : MonoBehaviour
{
    public Animator handAnimation;
    public Animator[] gestureAnimation;
    public AnimationEvents events;
    public Text canvasText;
    public Text doneText;

    private bool isDone = false;
    private int gestureType = 0;

    private Animator LeftHand { get { return gestureAnimation[1]; } }
    private Animator RightHand { get { return gestureAnimation[0]; } }

    void Start()
    {
        events.OnStateChange.AddListener(SwitchAnimator);
    }

    public void ShowTutorialFor(GestureType type)
    {
        if (type == GestureType.Default)
        {
            isDone = true;
        }
        canvasText.gameObject.SetActive(true);
        gestureType = (int)type;
        handAnimation.SetTrigger("flyIn");
    }

    void Update()
    {
        if (gestureType != (int)GestureType.TriangleGesture) { return; }


        // Sync animations
        var info = LeftHand.GetCurrentAnimatorStateInfo(0);
        RightHand.Play(info.fullPathHash, 0, info.normalizedTime);
        RightHand.SetLayerWeight(0, LeftHand.GetLayerWeight(0));
    }

    public void SwitchAnimator(int state)
    {
        bool isFlying = state == 0;
        handAnimation.enabled = isFlying;

        // only enable left hand for triangle gesture
        LeftHand.enabled = !isFlying;
        LeftHand.gameObject.SetActive(!isFlying && gestureType == (int)GestureType.TriangleGesture);
        RightHand.enabled = !isFlying;

        if (!isFlying)
        {
            LeftHand.SetInteger("state", gestureType);
            RightHand.SetInteger("state", gestureType);
        }
    }

    public void HideTutorials()
    {
        if (isDone)
        {
            doneText.gameObject.SetActive(true);
            return;
        }
        canvasText.gameObject.SetActive(false);
        LeftHand.SetTrigger("exit");
        RightHand.SetTrigger("exit");
        LeftHand.enabled = false;
        RightHand.enabled = false;
        SwitchAnimator(0);
        handAnimation.SetTrigger("goAway");
    }
}
