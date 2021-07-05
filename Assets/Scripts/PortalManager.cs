using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PortalManager : MonoBehaviour
{
    public GameObject cube;
    public float animationSpeed;
    public UnityEvent onTeleportEnter = new UnityEvent();
    public bool active = false;


    void Awake(){
        cube.SetActive(false);
    }

    public void initPortal(Vector3 pos){
        transform.position = pos;
        cube.SetActive(true);
        StartCoroutine(AnimatePortalOpenCoroutine());
    }


    public void destroyPortal(){
        StartCoroutine(AnimatePortalCloseCoroutine());
    }

    IEnumerator AnimatePortalOpenCoroutine()
    {
        active = true;
        for (float i = 0; i < 3; i+=0.2f)
        {
            scale(i);
            yield return new WaitForSeconds(animationSpeed);
        }
    }
   
    IEnumerator AnimatePortalCloseCoroutine()
    {
        for (float i = 3; i > 0; i-=0.2f)
        {
            scale(i);
            yield return new WaitForSeconds(animationSpeed);
        }
        cube.SetActive(false);
        Destroy(gameObject);
    }

    private void scale(float factor){
        cube.transform.localScale = new Vector3(cube.transform.localScale.x, factor, cube.transform.localScale.z);
    }

    public void registerCollision(){
        onTeleportEnter.Invoke();
    }
}
