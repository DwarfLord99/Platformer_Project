using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [SerializeField] Rigidbody2D rb2d;
    [SerializeField] Animator animator;

    [SerializeField] float moveSpeed;

    private InputAction moveAction;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        moveAction = InputSystem.actions.FindAction("Move");
    }

    // Update is called once per frame
    void Update()
    {
        print(moveAction.ReadValue<Vector2>());
        Movement();
    }

    private void Movement()
    {
        float horizontalInput = moveAction.ReadValue<Vector2>().x;

        rb2d.linearVelocity = new Vector2(horizontalInput * moveSpeed, 0.0f);

        // Sets walking animation based on movement using bool
        if (rb2d.linearVelocity.magnitude > 0.0f)
        {
            animator.SetBool("isWalking", true);
        }
        else
        {
            animator.SetBool("isWalking", false);
        }

        // Flips sprite based on movement direction
        if (horizontalInput == 1.0f)
        {
            transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
        }
        else if (horizontalInput == -1.0f)
        {
            transform.localScale = new Vector3(-1.0f, 1.0f, 1.0f);
        }
    }
}
