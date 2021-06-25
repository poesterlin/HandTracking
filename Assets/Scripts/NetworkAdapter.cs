using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;


public class NetworkAdapter {
    string ip;

    public NetworkAdapter(string address){
        ip = address;
    }

    public IEnumerator Post(string bodyJsonString)
    {
        var request = new UnityWebRequest(ip + "/gesture", "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(bodyJsonString);
        request.uploadHandler = (UploadHandler) new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = (DownloadHandler) new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        yield return request.SendWebRequest();
        Debug.Log("Status Code: " + request.responseCode);
    }

    public IEnumerator Get(GestureRecognizer inst) {
        using (UnityWebRequest webRequest = UnityWebRequest.Get(ip + "/gesture")) {
            // Request and wait for the desired page.
            yield return webRequest.SendWebRequest();
 
            if (webRequest.result == UnityWebRequest.Result.ConnectionError) {
                Debug.Log("Error: " + webRequest.error);
            }

            GestureList list = Gesture.CreateFromJSON(webRequest.downloadHandler.text);
            inst.SavedGestures = new List<Gesture>(list.Gestures);
            QuestDebug.Instance.Log("downloaded " + inst.SavedGestures.Count + " gestures");
            
        }
    }
}
