using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class CNNClient : MonoBehaviour
{
    [Header("Servidor Flask (mismo PC)")]
    public string serverURL = "http://127.0.0.1:5000/predict";

    [Header("Stream (ESP32Stream.cs)")]
    public ESP32Stream esp32Stream;

    [Header("Estado actual del terreno")]
    public string terrenoDetectado = "ninguno";
    public float probabilidad = 0f;

    [Header("Probabilidades completas")]
    public float probAsfalto;
    public float probCesped;
    public float probGrava;

    private Texture2D scaled;

    void Start()
    {
        // Se crea textura 224x224 donde copiaremos el frame real
        scaled = new Texture2D(224, 224, TextureFormat.RGB24, false);
        InvokeRepeating(nameof(EnviarFrame), 1f, 1f);
    }

    void EnviarFrame()
    {
        if (esp32Stream == null || esp32Stream.latestFrame == null)
        {
            Debug.Log("CNNClient: no hay frame todavía.");
            return;
        }

        StartCoroutine(EnviarAlServidor());
    }

    IEnumerator EnviarAlServidor()
    {
        Texture2D frame = esp32Stream.latestFrame;

        if (frame == null || frame.width < 10 || frame.height < 10)
        {
            Debug.Log("CNNClient: frame inválido, skip");
            yield break;
        }

        // ─────────────────────────────────────────────
        // Redimensionar usando ReadPixels (siempre funciona)
        // ─────────────────────────────────────────────

        RenderTexture rt = RenderTexture.GetTemporary(224, 224, 0);
        Graphics.Blit(frame, rt);

        RenderTexture.active = rt;
        scaled.ReadPixels(new Rect(0, 0, 224, 224), 0, 0);
        scaled.Apply();
        RenderTexture.active = null;

        RenderTexture.ReleaseTemporary(rt);

        // Convertir a JPG
        byte[] jpg = scaled.EncodeToJPG();

        WWWForm form = new WWWForm();
        form.AddBinaryData("image", jpg, "frame.jpg", "image/jpg");

        using (UnityWebRequest req = UnityWebRequest.Post(serverURL, form))
        {
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("CNN → " + req.downloadHandler.text);

                Prediccion r = JsonUtility.FromJson<Prediccion>(req.downloadHandler.text);

                terrenoDetectado = r.label;
                probabilidad = r.prob;

                if (r.raw_probs != null && r.raw_probs.Length == 3)
                {
                    probAsfalto = r.raw_probs[0];
                    probCesped = r.raw_probs[1];
                    probGrava = r.raw_probs[2];
                }
            }
            else
            {
                Debug.LogError("Error CNN: " + req.error);
            }
        }
    }

    [System.Serializable]
    public class Prediccion
    {
        public string label;
        public float prob;
        public float[] raw_probs;
    }
}
