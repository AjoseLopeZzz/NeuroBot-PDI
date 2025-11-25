using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;

public class RobotController : MonoBehaviour
{
    [Header("Entrada")]
    public MobileJoystick joystick;

    [Header("Animación")]
    public AnimationClip walkClip;

    [Header("Movimiento virtual")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 10f;
    public float deadZone = 0.15f;

    [Header("Robot físico")]
    public RobotUDP robotUDP;
    public int maxMotorPwm = 255; // rango -255..255

    private PlayableGraph graph;
    private AnimationClipPlayable walkPlayable;
    private bool isPlaying = false;

    void Start()
    {
        // Grafo de animación sin Animator Controller
        graph = PlayableGraph.Create("RobotGraph");
        graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

        Animator a = GetComponent<Animator>();
        if (a == null)
            a = gameObject.AddComponent<Animator>(); // sin controller

        var output = AnimationPlayableOutput.Create(graph, "Animation", a);

        walkPlayable = AnimationClipPlayable.Create(graph, walkClip);
        walkPlayable.SetSpeed(0);           // empieza quieto
        walkPlayable.SetTime(0.15f);        // arrancar un poco avanzada

        output.SetSourcePlayable(walkPlayable);
        graph.Play();
    }

    void Update()
    {
        Vector3 raw = joystick != null ? joystick.GetMoveVector() : Vector3.zero;
        Vector3 input = new Vector3(raw.x, 0, raw.y);

        bool moving = input.magnitude > deadZone;

        // Valores que mandaremos al ESP32
        int velA = 0;
        int velB = 0;

        if (moving)
        {
            // Dirección 3D: x = izquierda/derecha, z = adelante/atrás
            Vector3 dir = new Vector3(input.x, 0, input.z != 0 ? input.z : input.y);
            dir.Normalize();

            // Movimiento del modelo en Unity
            transform.Translate(dir * moveSpeed * Time.deltaTime, Space.World);

            // Rotación suave hacia la dirección
            Quaternion targetRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRot,
                Time.deltaTime * rotationSpeed
            );

            // Animación
            if (!isPlaying)
            {
                walkPlayable.SetSpeed(2f);
                isPlaying = true;
            }

            // ----- Cálculo de velocidades para motores -----
            // Adelante/atrás
            float forward = dir.z; // -1 atrás, 1 adelante
            // Giro
            float turn = dir.x;    // -1 izquierda, 1 derecha

            // Mezcla diferencial simple
            float aFloat = forward * maxMotorPwm + turn * maxMotorPwm * 0.5f;
            float bFloat = forward * maxMotorPwm - turn * maxMotorPwm * 0.5f;

            velA = Mathf.Clamp(Mathf.RoundToInt(aFloat), -maxMotorPwm, maxMotorPwm);
            velB = Mathf.Clamp(Mathf.RoundToInt(bFloat), -maxMotorPwm, maxMotorPwm);
        }
        else
        {
            // Parar animación
            if (isPlaying)
            {
                walkPlayable.SetSpeed(0);
                isPlaying = false;
            }

            velA = 0;
            velB = 0;
        }

        // Enviar velocidades al robot físico
        if (robotUDP != null)
        {
            robotUDP.SendMotors(velA, velB);
        }
    }

    void OnDestroy()
    {
        if (graph.IsValid())
            graph.Destroy();
    }
}
