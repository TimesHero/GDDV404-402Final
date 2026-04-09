using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneNavigationButton : MonoBehaviour
{
    [SerializeField] private string sceneName = string.Empty;

    public void LoadConfiguredScene()
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("SceneNavigationButton scene name is empty.");
            return;
        }

        SceneManager.LoadScene(sceneName);
    }
}
