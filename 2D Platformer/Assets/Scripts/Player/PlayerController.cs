using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [SerializeField] Rigidbody2D rb2d;

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
        rb2d.linearVelocity = new Vector2(moveAction.ReadValue<Vector2>().x * moveSpeed, 0.0f);
    }
}
