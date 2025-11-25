using UnityEngine;

public class CameraFollowSmooth : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0, 6, -10);
    public float followSpeed = 5f;
    public float rotationSpeed = 5f;

    void LateUpdate()
    {
        if (target == null) return;

        // Posición suave
        Vector3 desiredPos = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desiredPos, Time.deltaTime * followSpeed);

        // Rotación suave hacia el robot
        Quaternion desiredRot = Quaternion.LookRotation(target.position - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRot, Time.deltaTime * rotationSpeed);
    }
}
