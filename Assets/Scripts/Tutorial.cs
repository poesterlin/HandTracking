using UnityEngine;

public class Tutorial : MonoBehaviour
{
    public GameObject[] images = new GameObject[3];

    void Start()
    {
        HideTutorials();
    }

    public void ShowTutorialFor(GestureType type)
    {
        images[((int)type) - 1].SetActive(true);
    }

    public void HideTutorials()
    {
        foreach (var item in images)
        {
            item.SetActive(false);
        }
    }
}
