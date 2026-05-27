using UnityEngine;

public class CharacterAnimation : MonoBehaviour
{
    [SerializeField] private Animator animator;

    void Start()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        float speed = new Vector2(
            Input.GetAxis("Horizontal"),
            Input.GetAxis("Vertical")
        ).magnitude;

        animator.SetFloat("Speed", speed);

        if (Input.GetButtonDown("Punch"))
            animator.SetTrigger("Punch");
        if (Input.GetButtonDown("Kick"))
            animator.SetTrigger("Kick");
    }
}