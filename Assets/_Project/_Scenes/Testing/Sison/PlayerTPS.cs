using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerTPS : MonoBehaviour
{
    [Header("Movement")]
[SerializeField] private float moveSpeed = 5f;
[SerializeField] private float acceleration = 20f;
[SerializeField] private float deceleration = 25f;
[SerializeField] private float rotationSpeed = 10f;

[Header("Jump")]
[SerializeField] private InputActionReference jumpAction;
[SerializeField] private float jumpForce = 7f;
[SerializeField] private Transform groundCheck;
[SerializeField] private float groundCheckRadius = 0.2f;
[SerializeField] private LayerMask groundLayer; 

    [Header("References")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Joystick joystick;

    private Rigidbody rb;
    private Vector2 moveInput;
    private bool isGrounded;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        // Read WASD / left stick
        moveInput = new Vector2(
            joystick.Horizontal,
            joystick.Vertical
        );

               CheckGround();

    if (jumpAction.action.WasPressedThisFrame() && isGrounded)
    {
        Jump();
    }
    }

    private void FixedUpdate()
    {
        Move();
        Rotate();
    }
private void Move()
{
    Vector3 cameraForward = cameraTransform.forward;
    Vector3 cameraRight = cameraTransform.right;

    // Keep movement on the ground plane
    cameraForward.y = 0f;
    cameraRight.y = 0f;

    cameraForward.Normalize();
    cameraRight.Normalize();

    Vector3 movement =
        cameraForward * moveInput.y +
        cameraRight * moveInput.x;

    movement = Vector3.ClampMagnitude(movement, 1f);

    // Target horizontal velocity
    Vector3 targetVelocity = movement * moveSpeed;

    // Current horizontal velocity
    Vector3 currentVelocity = rb.linearVelocity;
    Vector3 currentHorizontalVelocity =
        new Vector3(currentVelocity.x, 0f, currentVelocity.z);

    // Accelerate when moving, decelerate when stopping
    float speedChange = movement.sqrMagnitude > 0.01f
        ? acceleration
        : deceleration;

    Vector3 newHorizontalVelocity = Vector3.MoveTowards(
        currentHorizontalVelocity,
        targetVelocity,
        speedChange * Time.fixedDeltaTime
    );

    // Preserve vertical velocity
    rb.linearVelocity = new Vector3(
        newHorizontalVelocity.x,
        rb.linearVelocity.y,
        newHorizontalVelocity.z
    );
}
private void CheckGround()
{
    isGrounded = Physics.CheckSphere(
        groundCheck.position,
        groundCheckRadius,
        groundLayer
    );
}
private void Jump()
{
    Vector3 velocity = rb.linearVelocity;

    // Remove existing downward velocity
    velocity.y = 0f;
    rb.linearVelocity = velocity;

    rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
}

    private void Rotate()
    {
        // Don't rotate when standing still
        Vector3 movement = rb.linearVelocity;
        movement.y = 0f;

        if (movement.sqrMagnitude < 0.01f)
            return;

        // Face the direction we're moving
        Quaternion targetRotation =
            Quaternion.LookRotation(movement);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.fixedDeltaTime
        );
    }
}