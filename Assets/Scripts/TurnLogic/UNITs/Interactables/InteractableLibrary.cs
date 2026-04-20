using System.Collections.Generic;
using UnityEngine;

public class InteractableLibrary : MonoBehaviour
{
    [SerializeField] private string resourcesPath = "InteractableData";
    [SerializeField] private List<InteractableData> loadedInteractables = new List<InteractableData>();

    private Dictionary<string, InteractableData> interactableById = new Dictionary<string, InteractableData>();

    private void Awake()
    {
        LoadAll();
    }

    public void LoadAll()
    {
        loadedInteractables.Clear();
        interactableById.Clear();

        InteractableData[] interactables = Resources.LoadAll<InteractableData>(resourcesPath);
        foreach (InteractableData interactable in interactables)
        {
            if (interactable == null || string.IsNullOrWhiteSpace(interactable.interactableId))
                continue;

            loadedInteractables.Add(interactable);
            interactableById[interactable.interactableId] = interactable;
        }
    }

    public List<InteractableData> GetAll()
    {
        return loadedInteractables;
    }

    public InteractableData GetById(string interactableId)
    {
        if (string.IsNullOrWhiteSpace(interactableId))
            return null;

        interactableById.TryGetValue(interactableId, out InteractableData result);
        return result;
    }
}