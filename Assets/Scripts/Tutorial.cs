using System;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class Tutorial : MonoBehaviour
{
    public Animator handAnimation;
    public Animator[] gestureAnimation;
    public AnimationEvents events;
    public Text canvasText;
    public Text doneText;

    public Vector3[] lineAnchors = new Vector3[] { new Vector3(1, 1, 1), new Vector3(1, 1, 1), new Vector3(1, 1, 1), new Vector3(1, 1, 1), new Vector3(1, 1, 1), new Vector3(1, 1, 1) };

    public Vector3 anchorOffset = new Vector3(0, 0, 0);

    public LineRenderer line;

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
        if (gestureType != (int)type && type != GestureType.Default)
        {
            HideTutorials();
        }

        gestureType = (int)type;
        isDone = type == GestureType.Default;
        doneText.gameObject.SetActive(isDone);
        canvasText.gameObject.SetActive(!isDone);
        handAnimation.SetTrigger("flyIn");
        if (!isDone)
        {
            Invoke("ShowLine", 2f);
        }
    }

    void Update()
    {
        // ShowLine();
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

        LeftHand.enabled = !isFlying;
        RightHand.enabled = !isFlying;

        // only enable left hand for triangle gesture
        LeftHand.gameObject.SetActive(!isFlying && gestureType == (int)GestureType.TriangleGesture);

        if (!isFlying)
        {
            LeftHand.SetInteger("state", gestureType);
            RightHand.SetInteger("state", gestureType);
        }
    }

    public void HideTutorials()
    {
        HideLine();
        doneText.gameObject.SetActive(isDone);
        SwitchAnimator(0);
        canvasText.gameObject.SetActive(false);
        LeftHand.SetTrigger("exit");
        RightHand.SetTrigger("exit");
        LeftHand.enabled = false;
        RightHand.enabled = false;
        handAnimation.SetTrigger("goAway");
    }

    private void ShowLine()
    {
        if (gestureType == 0)
        {
            return;
        }
        line.enabled = true;
        Vector3[] positions = { lineAnchors[(gestureType - 1) * 2] + anchorOffset, lineAnchors[(gestureType - 1) * 2 + 1] + anchorOffset };
        line.SetPositions(positions);
    }

    private void HideLine()
    {
        ShowLine();
        line.enabled = false;
    }
}
