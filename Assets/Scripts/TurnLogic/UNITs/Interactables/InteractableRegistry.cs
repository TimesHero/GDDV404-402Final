using System.Collections.Generic;
using UnityEngine;

public class InteractableRegistry : MonoBehaviour
{
    [SerializeField] private List<PlacedInteractable> placedInteractables = new List<PlacedInteractable>();

    public List<PlacedInteractable> GetAllPlacedInteractables()
    {
        return placedInteractables;
    }

    public void Register(PlacedInteractable interactable)
    {
        if (interactable == null)
            return;

        if (!placedInteractables.Contains(interactable))
            placedInteractables.Add(interactable);
    }

    public void Unregister(PlacedInteractable interactable)
    {
        if (interactable == null)
            return;

        placedInteractables.Remove(interactable);
    }

    public void ClearAll()
    {
        placedInteractables.Clear();
    }
}