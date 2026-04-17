using UnityEngine;

public class PlaceableAnchor : MonoBehaviour
{
    [SerializeField] private Transform placementAnchor;

    public Vector3 LocalAnchorPosition
    {
        get
        {
            if (placementAnchor != null)
                return placementAnchor.localPosition;

            return Vector3.zero;
        }
    }
}