
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Text;



public class QuestDebug : MonoBehaviour
{
    public static QuestDebug Instance;
    public Text logs;

    void Awake(){
        Instance = this;
    }

    // Update is called once per frame
    public void Log(string msg)
    {
        // DebugUIBuilder.instance.Show();
        // Instance.logs.text = msg;
        Debug.Log(msg);
        StartCoroutine(postLog(msg));
    }


    private IEnumerator postLog(string msg){
        var request = new UnityWebRequest(PlayerPrefs.GetString("server") + "/logs", "POST");

        byte[] bodyRaw = Encoding.UTF8.GetBytes(msg);
        request.uploadHandler = (UploadHandler) new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = (DownloadHandler) new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "text/plain");
        yield return request.SendWebRequest();
    }
}
