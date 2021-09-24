using UnityEngine;

public class Tutorial : MonoBehaviour
{
    public GameObject[] images = new GameObject[4];
    public GameObject headlineText;
    private bool isDone = false;
    void Start()
    {
        HideTutorials();
    }

    public void ShowTutorialFor(GestureType type)
    {
        if (type == GestureType.Default)
        {
            headlineText.SetActive(false);
            isDone = true;
        }
        int index = (int)type;
        images[index].SetActive(true);
    }

    public void HideTutorials()
    {
        if (isDone) { return; }
        foreach (var item in images)
        {
            item.SetActive(false);
        }
    }
}
