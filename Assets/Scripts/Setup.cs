using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class Setup : MonoBehaviour
{

    public string server = "https://bpi.oesterlin.dev";
    // Start is called before the first frame update
    void Awake()
    {
        PlayerPrefs.SetString("server", server);
    }
}
