using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    [Header("Nombre exacto de la escena")]
    public string sceneName;

    // Llamado desde el botón
    public void LoadScene()
    {
        SceneManager.LoadScene(sceneName);
    }
}
