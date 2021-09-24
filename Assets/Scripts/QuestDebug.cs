
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Text;
using System.Collections.Generic;
using System;

public class QuestDebug : MonoBehaviour
{
    public static QuestDebug Instance;
    public Text textEl;
    public Queue<string> logs;
    public int QueueSize = 30;
    public bool showLog = false;

    void Awake()
    {
        Instance = this;
        textEl = GetComponentInChildren<Text>();
        logs = new Queue<string>(QueueSize);
        textEl.gameObject.SetActive(showLog);
    }

    public void Log(string msg, bool post = false)
    {
        logs.Enqueue(msg /* + ": " + DateTime.Now */);
        if (logs.Count > QueueSize)
        {
            logs.Dequeue();
        }
        textEl.text = "Debug Log:\n" + String.Join("\n", logs);
        Debug.Log(msg);
        if (post)
        {
            StartCoroutine(postLog(msg));
        }
    }

    private IEnumerator postLog(string msg)
    {
        var request = new UnityWebRequest(PlayerPrefs.GetString("server") + "/logs", "POST");

        byte[] bodyRaw = Encoding.UTF8.GetBytes(msg);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "text/plain");
        yield return request.SendWebRequest();
    }
}
