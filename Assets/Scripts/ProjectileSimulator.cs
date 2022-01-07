using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ProjectileSimulator : MonoBehaviour
{
    public GameObject capsulePrefab;
    public int maxIterations;
    public GameObject environment;
    public int layer;

    Scene currentScene;
    Scene predictionScene;

    PhysicsScene currentPhysicsScene;
    PhysicsScene predictionPhysicsScene;
    readonly List<GameObject> dummyObstacles = new List<GameObject>();
    GameObject capsule;

    void Start()
    {

        Physics.autoSimulation = false;
        currentScene = SceneManager.GetActiveScene();
        currentPhysicsScene = currentScene.GetPhysicsScene();

        CreateSceneParameters parameters = new CreateSceneParameters(LocalPhysicsMode.Physics3D);
        predictionScene = SceneManager.CreateScene("Prediction", parameters);
        predictionPhysicsScene = predictionScene.GetPhysicsScene();

        CopyAllObstacles();
    }

    void FixedUpdate()
    {
        if (currentPhysicsScene.IsValid())
        {
            currentPhysicsScene.Simulate(Time.fixedDeltaTime);
        }
    }

    public void CopyAllObstacles()
    {
        GameObject fakeT = Instantiate(environment);
        fakeT.transform.position = environment.transform.position;
        fakeT.transform.rotation = environment.transform.rotation;

        foreach (var fakeR in fakeT.GetComponentsInChildren<Renderer>())
        {
            fakeR.enabled = false;
        }

        SceneManager.MoveGameObjectToScene(fakeT, predictionScene);
        dummyObstacles.Add(fakeT);
    }

    void KillAllObstacles()
    {
        foreach (var o in dummyObstacles)
        {
            Destroy(o);
        }
        dummyObstacles.Clear();
    }

    public List<Vector3> Predict(Vector3 currentPosition, Vector3 force, out bool hit)
    {
        List<Vector3> positionList = new List<Vector3>();
        hit = false;
        if (!currentPhysicsScene.IsValid() && !predictionPhysicsScene.IsValid())
        {
            return positionList;
        }

        capsule = Instantiate(capsulePrefab);
        SceneManager.MoveGameObjectToScene(capsule, predictionScene);
        capsule.layer = layer;
        capsule.transform.position = currentPosition;

        var body = capsule.GetComponent<Rigidbody>();
        body.AddForce(force, ForceMode.Impulse);

        var projectile = capsule.GetComponent<Projectile>();

        for (int i = 0; i < maxIterations; i++)
        {
            predictionPhysicsScene.Simulate(Time.fixedDeltaTime);
            positionList.Add(capsule.transform.position);
            if (projectile.CollisionDetected)
            {
                hit = true;
                break;
            }
        }

        Destroy(capsule);
        return positionList;
    }

    public List<Vector3> Skew(List<Vector3> trajectory, Vector3 target)
    {
        var newPos = new List<Vector3>(trajectory.Count);
        newPos.Add(trajectory.First());
        for (int i = 1; i < trajectory.Count; i++)
        {
            var curr = trajectory[i];
            float factor = i / (float)trajectory.Count;
            var skewed = Vector3.Lerp(curr, target, factor);
            newPos.Add(new Vector3(skewed.x, curr.y, skewed.z)); // keep hight
        }

        return newPos;
    }

    void OnDestroy()
    {
        KillAllObstacles();
    }
}
