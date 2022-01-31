using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PositionRecorder : MonoBehaviour
{
    public float periodInSek = 0.1f;
    private NetworkAdapter network;

    // Start is called before the first frame update
    void Start()
    {
        network = new NetworkAdapter();
        StartCoroutine(DoCheck());
    }

    IEnumerator DoCheck()
    {
        while (true)
        {
            var pos = gameObject.transform.position;
            var rotation = gameObject.transform.rotation;
            StartCoroutine(network.Set("/stats/position", "posX", pos.x, "posY", pos.z, "rotation", rotation.eulerAngles.y));
            yield return new WaitForSeconds(periodInSek);
        }
    }
}
