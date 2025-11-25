using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;

public class RobotSerial : MonoBehaviour
{
    UdpClient udp;
    IPEndPoint endPoint;

    void Start()
    {
        udp = new UdpClient();
        endPoint = new IPEndPoint(IPAddress.Parse("192.168.1.50"), 4210);
        // Cambia la IP por la del ESP32
    }

    public void SendMotors(int a, int b)
    {
        string msg = a + "," + b;
        byte[] data = Encoding.ASCII.GetBytes(msg);
        udp.Send(data, data.Length, endPoint);
    }
}
