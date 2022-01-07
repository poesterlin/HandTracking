using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class Setup : MonoBehaviour
{

    public string productionServer = "https://bpi.oesterlin.dev";
    public string devServer = "http://192.168.1.100:3000";
    public bool productionSettings = true;
    // Start is called before the first frame update
    void Awake()
    {
        var server = productionSettings ? productionServer : devServer;
        PlayerPrefs.SetString("server", server);
        Debug.Log("Server set to: " + server);
    }

    void Start()
    {
        Awake();
    }
}
