using System;
using UnityEngine;
using UnityEngine.SceneManagement;

[Serializable]
public class State
{
    public string Scene;
    public GestureType CurrentGesture;

    public static State GetState(StudyObserver obs)
    {
        return new State
        {
            Scene = SceneManager.GetActiveScene().name,
            CurrentGesture = obs.GetCurrentGesture()
        };
    }

    public void LoadState(State s, StudyObserver obs)
    {
        if (!Scene.Equals(s.Scene))
        {
            SceneManager.LoadScene(s.Scene);
        }

        if (CurrentGesture != s.CurrentGesture && obs != null)
        {
            // reset 
            GestureType[] defaultOrder = { GestureType.Default };
            obs.SetOrder(defaultOrder);

            // apply new order
            GestureType[] order = { s.CurrentGesture };
            obs.SetOrder(order);
            QuestDebug.Instance.Log("state set to: " + ToString(), true);
        }
    }

    public static State FromJSON(string json)
    {
        return JsonUtility.FromJson<State>(json);
    }

    public string ToJSON()
    {
        return JsonUtility.ToJson(this);
    }

    public override string ToString()
    {
        return ToJSON();
    }
}


public class StateController : MonoBehaviour
{
    public State state;
    public StudyObserver observer;
    private NetworkAdapter network;
    void Start()
    {
        network = new NetworkAdapter();
        state = State.GetState(observer);
        StartCoroutine(network.UpdateState(this));
    }

    public void LoadState(State newState)
    {
        if (state != null)
        {
            state.LoadState(newState, observer);
        }
        state = newState;
    }
}
