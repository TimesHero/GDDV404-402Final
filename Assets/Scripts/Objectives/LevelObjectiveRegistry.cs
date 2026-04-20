using System.Collections.Generic;
using UnityEngine;

public class LevelObjectiveRegistry : MonoBehaviour
{
    [SerializeField] private List<LevelObjectiveData> objectives = new List<LevelObjectiveData>();

    public List<LevelObjectiveData> GetObjectives()
    {
        return objectives;
    }

    public void SetObjectives(List<LevelObjectiveData> newObjectives)
    {
        objectives = newObjectives ?? new List<LevelObjectiveData>();
    }

    public void ClearObjectives()
    {
        objectives.Clear();
    }
}