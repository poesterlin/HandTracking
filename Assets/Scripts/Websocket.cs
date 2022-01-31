using UnityEngine;
using WebSocketSharp;
using System.Collections;

public class Websocket : MonoBehaviour
{
    // WebSocket ws;
    // public GestureRecognizer gest;
    // public string server; //bpi.oesterlin.dev

    // private void Start()
    // {
    //     ws = new WebSocket("ws://" + server + ":8080");
    //     ws.Connect();
    //     ws.OnMessage += (object sender, MessageEventArgs e) => MessageReceived(sender, e);
    //     ws.OnClose += (object sender, CloseEventArgs e) =>
    //     {
    //         Debug.Log("closing: " + e.Reason + "(" + e.Code + ")");
    //         Invoke("Reconnect", 1f);
    //     };
    //     ws.OnError += (object sender, ErrorEventArgs e) =>
    //     {
    //         Debug.Log("error: " + e.Message);
    //         Debug.Log(e.Exception);
    //         Invoke("Reconnect", 1f);
    //     };
    // }

    // private void MessageReceived(object sender, MessageEventArgs e)
    // {
    //     string saveCmd = "save";
    //     if (saveCmd.Equals(e.Data))
    //     {
    //         gest.shouldSave = true;
    //         Debug.Log("starting save");
    //         // StartCoroutine(callback());
    //         ws.Send("done");
    //     }
    // }

    // private IEnumerator callback()
    // {
    //     while (gest.shouldSave)
    //     {
    //         yield return new WaitForSeconds(0.1f);
    //     }
    //     ws.Send("done");

    //     Debug.Log("sending");
    // }

    // private void Reconnect()
    // {
    //     Debug.Log("reconnecting");
    //     ws.Close();
    //     ws.Connect();
    // }
}