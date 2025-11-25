using UnityEngine;
using UnityEngine.UI;

public class RobotTelemetryUI : MonoBehaviour
{
    public RobotUDP robot;
    public Text textField; // asigna un Text en Canvas

    void Update()
    {
        if (robot == null || textField == null) return;

        textField.text =
            $"S1:{robot.s1}  S2:{robot.s2}  S3:{robot.s3}\n" +
            $"S4:{robot.s4}  S5:{robot.s5}\n" +
            $"E1:{robot.e1}  E2:{robot.e2}";
    }
}
