using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Collections;
using System.IO;

public class ESP32Stream : MonoBehaviour
{
    [Header("URL del Stream MJPEG")]
    public string streamURL = "http://192.168.80.29:81/stream";

    [Header("Destino")]
    public RawImage rawImageTarget;         // Mostrar en UI (Canvas)
    public MeshRenderer meshRendererTarget; // Mostrar en objeto 3D

    [Header("Exportar frame (para la CNN)")]
    public Texture2D latestFrame;           // último frame para el CNNClient (Unity → Flask)

    private Texture2D tex;
    private bool streaming = true;

    void Start()
    {
        tex = new Texture2D(2, 2, TextureFormat.RGB24, false);
        StartCoroutine(StreamMJPEG());
    }

    IEnumerator StreamMJPEG()
    {
        UnityWebRequest req = UnityWebRequest.Get(streamURL);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SendWebRequest();

        MemoryStream imgBuffer = new MemoryStream();
        bool readingImage = false;

        while (streaming)
        {
            byte[] data = req.downloadHandler.data;

            if (data == null || data.Length < 2)
            {
                yield return null;
                continue;
            }

            for (int i = 0; i < data.Length - 1; i++)
            {
                // ───────────────────────────────────────────────
                // INICIO JPG (FFD8)
                // ───────────────────────────────────────────────
                if (!readingImage && data[i] == 0xFF && data[i + 1] == 0xD8)
                {
                    readingImage = true;
                    imgBuffer = new MemoryStream();
                    imgBuffer.WriteByte(0xFF);
                    imgBuffer.WriteByte(0xD8);
                    i++;
                }

                // ───────────────────────────────────────────────
                // FIN JPG (FFD9)
                // ───────────────────────────────────────────────
                else if (readingImage && data[i] == 0xFF && data[i + 1] == 0xD9)
                {
                    imgBuffer.WriteByte(0xFF);
                    imgBuffer.WriteByte(0xD9);

                    byte[] jpgBytes = imgBuffer.ToArray();
                    ApplyFrame(jpgBytes);

                    readingImage = false;
                    i++;
                }

                // ───────────────────────────────────────────────
                // CONTENIDO JPG
                // ───────────────────────────────────────────────
                else if (readingImage)
                {
                    imgBuffer.WriteByte(data[i]);
                }
            }

            yield return null;
        }
    }

    void ApplyFrame(byte[] jpg)
    {
        tex.LoadImage(jpg);

        // Mostrar en Canvas
        if (rawImageTarget != null)
            rawImageTarget.texture = tex;

        // Mostrar en objeto 3D
        if (meshRendererTarget != null)
            meshRendererTarget.material.mainTexture = tex;

        // Exportar textura para la CNN
        latestFrame = tex;
    }
}
