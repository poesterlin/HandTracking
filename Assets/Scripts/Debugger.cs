using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Tayx.Graphy;

public class Debugger : MonoBehaviour
{

    public static Debugger Instance;

    bool inMenu;
    // Text logText;
    OVRCameraRig rig;

    void Awake(){
        Instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        // var rt = DebugBuilder.instance.AddLabel("Debug");
        // logText = rt.GetComponent<Text>();

        rig = FindObjectOfType<OVRCameraRig>();
    }

    // Update is called once per frame
    void Update()
    {
        if(OVRInput.GetDown(OVRInput.Button.Two))   {
            if(inMenu){
                // DebugBuilder.instance.Hide();
                GraphyManager.Instance.Disable();
            }
            else
            {
                // DebugBuilder.instance.Show();
                GraphyManager.Instance.Enable();
                GraphyManager.Instance.transform.position = rig.transform.TransformPoint(0,0,4f);
                GraphyManager.Instance.transform.rotation = rig.transform.rotation;
            }
            inMenu = !inMenu;
        }
    }


    public void Log(string msg){
        // logText.text = msg;
    }
}
