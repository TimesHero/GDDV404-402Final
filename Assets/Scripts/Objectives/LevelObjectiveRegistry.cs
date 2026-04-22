using System.Collections.Generic;
using UnityEngine;

public class LevelObjectiveRegistry : MonoBehaviour
{
    [SerializeField] private List<LevelObjectiveData> objectives = new List<LevelObjectiveData>();
    [SerializeField] private bool loseWhenSeen;

    public bool LoseWhenSeen => loseWhenSeen;

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

    public void SetLoseWhenSeen(bool isEnabled)
    {
        loseWhenSeen = isEnabled;
    }
}
