using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class SettingsDto
{
    public float threshold;
}

[ExecuteAlways]
public class Setup : MonoBehaviour
{

    public string productionServer = "https://vr.oesterlin.dev";
    public string devServer = "http://192.168.1.100:3000";
    public bool productionSettings = true;
    private NetworkAdapter network;
    public UnityEvent<SettingsDto> SettingsChanged = new UnityEvent<SettingsDto>();

    public SettingsDto settings = new SettingsDto();

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
        PlayerPrefs.SetFloat("threshold", 0.012f);
        network = new NetworkAdapter();
        StartCoroutine(network.GetSettings(this));
    }

    public void SetSettings(SettingsDto settings)
    {
        Debug.Log("changing settings: " + settings.threshold);
        this.settings = settings;
        SettingsChanged.Invoke(settings);
    }
}