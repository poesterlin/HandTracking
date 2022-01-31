
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Text;
using System.Collections.Generic;
using System;
using System.Linq;

public class QuestDebug : MonoBehaviour
{
    public static QuestDebug Instance;
    public Text textEl;
    public Queue<string> logs;
    public Queue<string> severeLogs;
    public int QueueSize = 20;
    public bool showLog = false;

    void Awake()
    {
        Instance = this;
        textEl = GetComponentInChildren<Text>();
        logs = new Queue<string>(QueueSize);
        severeLogs = new Queue<string>(QueueSize / 2);
        textEl.gameObject.SetActive(showLog);
        Application.logMessageReceivedThreaded += HandleLog;
    }

    void OnDisable()
    {
        Application.logMessageReceivedThreaded -= HandleLog;
    }

    public void Log(string msg, bool severe = false)
    {
        try
        {
            var q = severe ? severeLogs : logs;
            q.Enqueue(msg);
            if (q.Count > QueueSize)
            {
                q.Dequeue();
            }
            // bool different = q.Last().Equals(msg);
            textEl.text = "Severe Log:\n" + String.Join("\n", severeLogs) + "\nDebug Log:\n" + String.Join("\n", logs);
            if (severe)
            {
                Debug.Log(msg);
                StartCoroutine(PostLog(msg));
            }
        }
        catch (Exception) { }
    }

    void HandleLog(string logString, string stackTrace, LogType type)
    {
        if (type != LogType.Exception)
        {
            return;
        }

        var msg = "Exception: " + logString + "\n" + stackTrace;
        Instance.Log(msg, true);
    }


    private IEnumerator PostLog(string msg)
    {
        var request = new UnityWebRequest(PlayerPrefs.GetString("server") + "/logs", "POST");

        byte[] bodyRaw = Encoding.UTF8.GetBytes(msg);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "text/plain");
        yield return request.SendWebRequest();
    }
}
