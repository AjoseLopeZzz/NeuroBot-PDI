using UnityEngine;

public class OpenURL : MonoBehaviour
{
    [Header("URL a abrir")]
    public string url;

    public void OpenLink()
    {
        Application.OpenURL(url);
    }
}
