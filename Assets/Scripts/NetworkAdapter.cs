using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Globalization;

public class NetworkAdapter
{
    string ip;

    public NetworkAdapter()
    {
        ip = PlayerPrefs.GetString("server");
    }

    public IEnumerator Set(string endpoint, params object[] properties)
    {
        string bodyJsonString = "{";

        for (int i = 0; i + 1 < properties.Length; i += 2)
        {
            bodyJsonString += "\"" + properties[i] + "\":";

            Type t = properties[i + 1].GetType();
            NumberFormatInfo nfi = new NumberFormatInfo();
            nfi.NumberDecimalSeparator = ".";

            if (t.Equals(typeof(Vector3)))
            {
                var v = (Vector3)properties[i + 1];
                bodyJsonString += $"{{ \"x\": {v.x.ToString(nfi)},\"y\": {v.y.ToString(nfi)}, \"z\": {v.z.ToString(nfi)}}},";
            }

            if (t.Equals(typeof(int)))
            {
                bodyJsonString += ((int)properties[i + 1]) + ",";
            }

            if (t.Equals(typeof(float)))
            {
                bodyJsonString += ((float)properties[i + 1]).ToString(nfi) + ",";
            }

            if (t.Equals(typeof(bool)))
            {
                bodyJsonString += (((bool)properties[i + 1]) ? "true" : "false") + ",";
            }

            if (t.Equals(typeof(string)))
            {
                bodyJsonString += "\"" + ((string)properties[i + 1]).Replace("\"", "\\\"") + "\",";
            }
        }

        bodyJsonString = bodyJsonString.TrimEnd(',') + "}";
        Debug.Log(bodyJsonString);

        var request = new UnityWebRequest(ip + endpoint, "POST");

        byte[] bodyRaw = Encoding.UTF8.GetBytes(bodyJsonString);
        request.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        yield return request.SendWebRequest();
        Debug.Log("Status Code: " + request.responseCode);
        Debug.Log(request.downloadHandler.text);
    }

    public IEnumerator Post(string bodyJsonString)
    {
        var request = new UnityWebRequest(ip + "/gesture", "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(bodyJsonString);
        request.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        yield return request.SendWebRequest();
        Debug.Log("Status Code: " + request.responseCode);
    }

    public IEnumerator GetGestures(GestureRecognizer inst, float size)
    {
        var getSize = "small";

        if (size > 0.9f)
        {
            getSize = "medium";
        }

        if (size > 1.05f)
        {
            getSize = "large";
        }

        QuestDebug.Instance.Log(ip);
        using (UnityWebRequest webRequest = UnityWebRequest.Get(ip + "/gesture?size=" + getSize))
        {
            // Request and wait for the desired page.
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.ConnectionError)
            {
                QuestDebug.Instance.Log(webRequest.error, true);
            }

            GestureList list = Gesture.CreateFromJSON(webRequest.downloadHandler.text);
            inst.SavedGestures = new List<Gesture>(list.Gestures);
            QuestDebug.Instance.Log("downloaded " + inst.SavedGestures.Count + " gestures", true);
        }
    }

    public IEnumerator UpdateForceGesture(GestureRecognizer inst)
    {
        while (true)
        {
            using (UnityWebRequest webRequest = UnityWebRequest.Get(ip + "/stats/forceGesture"))
            {
                // Request and wait for the desired page.
                yield return webRequest.SendWebRequest();

                if (webRequest.result == UnityWebRequest.Result.ConnectionError)
                {
                    QuestDebug.Instance.Log(webRequest.error, true);
                }
                var json = webRequest.downloadHandler.text;
                if (json.Length > 5)
                {
                    inst.forceGesture = JsonUtility.FromJson<Gesture>(json);
                }
            }
            yield return new WaitForSeconds(0.5f);
        }
    }

    public IEnumerator UpdateState(StateController inst)
    {
        while (true)
        {
            using (UnityWebRequest request = new UnityWebRequest(ip + "/stats/state", "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(inst.state.ToJSON());
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.ConnectionError)
                {
                    QuestDebug.Instance.Log(request.error, true);
                }

                var json = request.downloadHandler.text;

                if (json.Length > 5)
                {
                    inst.LoadState(State.FromJSON(json));
                }
            }

            yield return new WaitForSeconds(0.5f);
        }
    }

    public IEnumerator GetOrder(StudyObserver inst)
    {
        using (UnityWebRequest webRequest = UnityWebRequest.Get(ip + "/stats/cb-order"))
        {
            yield return webRequest.SendWebRequest();
            if (webRequest.result == UnityWebRequest.Result.ConnectionError)
            {
                QuestDebug.Instance.Log(webRequest.error, true);
            }

            CounterBallanceRecord counterballance = CounterBallanceRecord.CreateFromJSON(webRequest.downloadHandler.text);
            inst.SetOrder(counterballance.Order);
            QuestDebug.Instance.Log("teleport order: " + String.Join(", ", counterballance.Order), true);
        }
    }
}
