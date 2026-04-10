using UnityEngine;
using Unity.Profiling;
using System.Collections;
using System.Collections.Generic;

public class ProfilerExample : MonoBehaviour
{
    static readonly ProfilerMarker preformanceMarker = new("ProfilerExample.prepare");
    public GameObject prefab;

    public void Update()
    {
        //if Space is pressed, run AddMarker once.
        if (Input.GetKeyDown(KeyCode.Space))
            AddMarker();
    }


    //adds a marker in the profiler to track a certain method's preformance
    public void AddMarker()
    {
        Debug.Log("baboom");
        preformanceMarker.Begin();
        IntenseLogic();
        preformanceMarker.End();

    }

    //does unnessisary and intense work so something is seen in the compiler
    private void IntenseLogic()
    {
        Debug.Log("kaboom");
        List<GameObject> gameObjects = new();
        for (int i = 0; i < 5500; i++)
        {
            GameObject go = Instantiate(prefab);

            go.transform.position = Random.insideUnitSphere * 100;

            gameObjects.Add(go);

        }
    }
}
