using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;


public class NetworkAdapter
{
    string ip;

    public NetworkAdapter(string address)
    {
        ip = address;
    }

    public IEnumerator Set(string endpoint, params object[] properties)
    {

        string bodyJsonString = "{";

        for (int i = 0; i + 1 < properties.Length; i += 2)
        {
            bodyJsonString += "\"" + properties[i] + "\":";

            Type t = properties[i + 1].GetType();

            if (t.Equals(typeof(int)))
            {
                bodyJsonString += ((int)properties[i + 1]) + ",";
            }

            if (t.Equals(typeof(float)))
            {
                bodyJsonString += ((float)properties[i + 1]) + ",";
            }

            if (t.Equals(typeof(bool)))
            {
                bodyJsonString += (((bool)properties[i + 1]) ? "true" : "false") + ",";
            }

            if (t.Equals(typeof(string)))
            {
                bodyJsonString += "\"" + ((string)properties[i + 1]) + "\",";
            }
        }

        bodyJsonString = bodyJsonString.TrimEnd(',') + "}";

        var request = new UnityWebRequest(ip + endpoint, "POST");

        byte[] bodyRaw = Encoding.UTF8.GetBytes(bodyJsonString);
        request.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        yield return request.SendWebRequest();
        Debug.Log("Status Code: " + request.responseCode);
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
}
