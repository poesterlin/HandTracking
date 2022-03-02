using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class Setup : MonoBehaviour
{

    public string productionServer = "https://bpi.oesterlin.dev";
    public string devServer = "http://192.168.1.100:3000";
    public bool productionSettings = true;

    public Dictionary<string, string> GetSettings()
    {
        var settings = new Dictionary<string, string>();
        settings.Add("server", productionSettings ? productionServer : devServer);
        return settings;
    }

    void Awake()
    {
        var settings = GetSettings();
        foreach (var item in settings)
        {
            PlayerPrefs.SetString(item.Key, item.Value);
            Debug.Log("set " + item.Key + " to " + item.Value);
        }
    }
}
