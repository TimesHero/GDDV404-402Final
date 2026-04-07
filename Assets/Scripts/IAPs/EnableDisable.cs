using UnityEngine;

public class EnableDisable : MonoBehaviour
{
    public GameObject objectToEnable;

    public void ToggleObject()
    {
        objectToEnable.SetActive(!objectToEnable.active);
    }
}
