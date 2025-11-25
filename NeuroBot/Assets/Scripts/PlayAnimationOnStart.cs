using UnityEngine;

public class PlayAnimationOnStart : MonoBehaviour
{
    [Header("Animator del modelo")]
    public Animator animator;

    [Header("Nombre del clip de animación")]
    public string animationName = "Idle"; // aquí pones el nombre del clip

    void Start()
    {
        // Apenas inicia la escena, reproduce la animación en loop
        if (animator != null)
        {
            animator.Play(animationName);
        }
    }
}
