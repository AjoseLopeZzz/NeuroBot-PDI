using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class RobotUDP : MonoBehaviour
{
    [Header("Red")]
    [Tooltip("IP del ESP32 (la que imprime en el Serial Monitor)")]
    public string robotIp = "192.168.1.50";

    [Tooltip("Puerto UDP del ESP32 (en tu código: 4210)")]
    public int robotPort = 4210;

    [Tooltip("Puerto local donde escucha Unity (cualquiera > 1024)")]
    public int localPort = 4211;

    private UdpClient udp;
    private IPEndPoint robotEndPoint;

    // Últimos datos recibidos (sensores + encoders)
    public int s1, s2, s3, s4, s5;
    public long e1, e2;

    void Start()
    {
        // Bind al puerto local para recibir telemetría
        udp = new UdpClient(localPort);
        robotEndPoint = new IPEndPoint(IPAddress.Parse(robotIp), robotPort);
        Debug.Log($"RobotUDP iniciado. LocalPort={localPort} → {robotIp}:{robotPort}");
    }

    // Llamas esto desde RobotController
    public void SendMotors(int a, int b)
    {
        if (udp == null) return;

        string msg = a + "," + b;     // mismo formato que espera el ESP32
        byte[] data = Encoding.ASCII.GetBytes(msg);
        udp.Send(data, data.Length, robotEndPoint);
        // Debug.Log("TX: " + msg); // descomenta si quieres ver lo que sale
    }

    void Update()
    {
        // Leer telemetría del robot si hay datos
        if (udp == null || udp.Available <= 0) return;

        IPEndPoint any = new IPEndPoint(IPAddress.Any, 0);
        byte[] data = udp.Receive(ref any);
        string msg = Encoding.ASCII.GetString(data).Trim();

        // Formato desde ESP32:
        // d1,d2,d3,d4,d5,encoder1,encoder2
        string[] parts = msg.Split(',');
        if (parts.Length == 7)
        {
            int.TryParse(parts[0], out s1);
            int.TryParse(parts[1], out s2);
            int.TryParse(parts[2], out s3);
            int.TryParse(parts[3], out s4);
            int.TryParse(parts[4], out s5);
            long.TryParse(parts[5], out e1);
            long.TryParse(parts[6], out e2);
            // Debug.Log($"RX: {msg}");
        }
    }

    void OnDestroy()
    {
        if (udp != null)
        {
            udp.Close();
            udp = null;
        }
    }
}
