using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private LayerMask groundLayer;

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float slopeFactor = 0.5f;
    [SerializeField] private float rayDistance = 1.1f;
    [SerializeField] private float footOffset = 0.3f;         // left/right foot ray offset
    [SerializeField] private float slopeStickStrength = 15f;  // press down into slope
    [SerializeField] private float normalSmoothSpeed = 10f;   // smooth slope normals

    [Header("Slope Alignment")]
    [SerializeField] private bool tiltToSlope = true;
    [SerializeField] private float tiltSpeed = 8f; // higher = snappier rotation
    [SerializeField] private float maxTiltAngle = 25f; // prevents crazy angles

    private Rigidbody2D rb2d;
    private InputAction moveAction;
    private float horizontalInput;
    private bool isGrounded;
    private Vector2 slopeNormal = Vector2.up; // current smoothed normal

    // --- Optional debug visualization ---
    private Vector2 projectedGravity;
    private Vector2 pressDownForce;

    private void Start()
    {
        if (rb2d == null)
            rb2d = GetComponent<Rigidbody2D>();

        // Smooth movement between physics updates
        rb2d.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb2d.gravityScale = 1f;

        moveAction = InputSystem.actions.FindAction("Move");
    }

    private void Update()
    {
        horizontalInput = moveAction.ReadValue<Vector2>().x;
        HandleAnimation();
    }

    private void FixedUpdate()
    {
        HandleMovement();
        HandleSlopeRotation();
    }

    private void HandleMovement()
    {
        Vector2 position = transform.position;

        // --- 1. Dual foot raycasts ---
        Vector2 leftFootPos = position + Vector2.left * footOffset;
        Vector2 rightFootPos = position + Vector2.right * footOffset;

        RaycastHit2D leftHit = Physics2D.Raycast(leftFootPos, Vector2.down, rayDistance, groundLayer);
        RaycastHit2D rightHit = Physics2D.Raycast(rightFootPos, Vector2.down, rayDistance, groundLayer);

        Debug.DrawRay(leftFootPos, Vector2.down * rayDistance, Color.green);
        Debug.DrawRay(rightFootPos, Vector2.down * rayDistance, Color.cyan);

        // --- 2. Compute averaged ground normal ---
        Vector2 targetNormal = Vector2.up;
        isGrounded = false;

        if (leftHit.collider && rightHit.collider)
        {
            targetNormal = (leftHit.normal + rightHit.normal).normalized;
            isGrounded = true;
        }
        else if (leftHit.collider)
        {
            targetNormal = leftHit.normal;
            isGrounded = true;
        }
        else if (rightHit.collider)
        {
            targetNormal = rightHit.normal;
            isGrounded = true;
        }

        // --- 3. Smooth the normal to reduce jitter ---
        slopeNormal = Vector2.Lerp(slopeNormal, targetNormal, Time.fixedDeltaTime * normalSmoothSpeed).normalized;

        float slopeAngle = isGrounded ? Vector2.Angle(Vector2.up, slopeNormal) : 0f;

        // --- 4. Adjust speed slightly on steep slopes (optional) ---
        float adjustedSpeed = moveSpeed;
        if (isGrounded && slopeAngle > 0f)
        {
            adjustedSpeed -= slopeFactor * slopeAngle / 90f;
            adjustedSpeed = Mathf.Max(adjustedSpeed, moveSpeed * 0.5f);
        }

        // --- 5. Base horizontal movement ---
        Vector2 velocity = rb2d.linearVelocity;
        velocity.x = horizontalInput * adjustedSpeed;

        // --- 6. Slope sticking & projected gravity ---
        if (isGrounded)
        {
            // Project gravity so it acts parallel to the slope
            projectedGravity = Physics2D.gravity;
            projectedGravity -= Vector2.Dot(projectedGravity, slopeNormal) * slopeNormal;

            rb2d.AddForce(projectedGravity * rb2d.gravityScale, ForceMode2D.Force);

            // Apply "press down" to maintain contact
            pressDownForce = -slopeNormal * slopeStickStrength;
            rb2d.AddForce(pressDownForce, ForceMode2D.Force);

            // Smooth stop on slope when no input
            if (Mathf.Abs(horizontalInput) < 0.01f)
                velocity.x = Mathf.Lerp(velocity.x, 0f, 0.2f);
        }

        rb2d.linearVelocity = velocity;
    }

    private void HandleAnimation()
    {
        bool isWalking = Mathf.Abs(rb2d.linearVelocity.x) > 0.05f && Mathf.Abs(horizontalInput) > 0.01f;
        animator.SetBool("isWalking", isWalking);

        // Flip sprite
        if (horizontalInput > 0.1f)
            transform.localScale = new Vector3(1f, 1f, 1f);
        else if (horizontalInput < -0.1f)
            transform.localScale = new Vector3(-1f, 1f, 1f);
    }

    private void HandleSlopeRotation()
    {
        if (!tiltToSlope) return;

        // Skip rotation if airborne or slope is flat
        if ((!isGrounded || Vector2.Angle(slopeNormal, Vector2.up) < 0.1f))
        {
            float newZ = Mathf.LerpAngle(transform.eulerAngles.z, 0f, Time.fixedDeltaTime * tiltSpeed);
            transform.rotation = Quaternion.Euler(0f, 0f, newZ);
            return;
        }

        if (isGrounded)
        {
            // Get angle between world up and slope normal
            float angle = Vector2.Angle(Vector2.up, slopeNormal);

            // Determine sign using cross product (positive = right tilt, negative = left tilt)
            float sign = Mathf.Sign(Vector3.Cross(Vector2.up, slopeNormal).z);
            float targetAngle = angle * sign;

            // Clamp to avoid extreme tilts
            targetAngle = Mathf.Clamp(targetAngle, -maxTiltAngle, maxTiltAngle);

            // Smooth rotation
            float newZ = Mathf.LerpAngle(transform.eulerAngles.z, targetAngle, Time.fixedDeltaTime * tiltSpeed);
            transform.rotation = Quaternion.Euler(0f, 0f, newZ);
        }
        else
        {
            // Return upright when airborne
            float newZ = Mathf.LerpAngle(transform.eulerAngles.z, 0f, Time.fixedDeltaTime * tiltSpeed);
            transform.rotation = Quaternion.Euler(0f, 0f, newZ);
        }
    }


    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        // Slope normal
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + (Vector3)slopeNormal * 0.75f);

        // Projected gravity (along slope)
        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(transform.position, transform.position + (Vector3)projectedGravity.normalized * 0.75f);

        // Press down force (into slope)
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + (Vector3)pressDownForce.normalized * 0.5f);
    }
}
