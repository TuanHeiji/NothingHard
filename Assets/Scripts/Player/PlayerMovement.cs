using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public InputAction MoveAction;

    [Header("Movement")]
    public float walkSpeed = 3f;

    [Header("Rotation")]
    public float turnSpeed = 720f;

    private Rigidbody m_Rigidbody;
    private Animator m_Animator;

    private Vector3 m_Movement;

    void Start()
    {
        m_Rigidbody = GetComponent<Rigidbody>();
        m_Animator = GetComponent<Animator>();

        MoveAction.Enable();
    }

    void FixedUpdate()
    {
        // Get joystick input
        Vector2 input = MoveAction.ReadValue<Vector2>();

        // Limit joystick magnitude to 1
        input = Vector2.ClampMagnitude(input, 1f);

        // Dead zone
        if (input.magnitude < 0.1f)
        {
            input = Vector2.zero;
        }

        // Get camera
        Camera cam = Camera.main;

        if (cam == null)
            return;

        // Camera forward and right
        Vector3 cameraForward = cam.transform.forward;
        Vector3 cameraRight = cam.transform.right;

        // Ignore camera's vertical rotation
        cameraForward.y = 0f;
        cameraRight.y = 0f;

        cameraForward.Normalize();
        cameraRight.Normalize();

        // Convert joystick input to camera-relative movement
        m_Movement =
            cameraRight * input.x +
            cameraForward * input.y;

        // Keep movement within magnitude 1
        m_Movement = Vector3.ClampMagnitude(m_Movement, 1f);

        float inputMagnitude = m_Movement.magnitude;

        // Animator
        bool isWalking = inputMagnitude > 0.1f;

        if (m_Animator != null)
        {
            m_Animator.SetBool("IsWalking", isWalking);
        }

        // No movement
        if (inputMagnitude <= 0.1f)
            return;

        // Rotate character toward movement direction
        Quaternion targetRotation =
            Quaternion.LookRotation(m_Movement);

        Quaternion smoothRotation =
            Quaternion.RotateTowards(
                m_Rigidbody.rotation,
                targetRotation,
                turnSpeed * Time.fixedDeltaTime
            );

        m_Rigidbody.MoveRotation(smoothRotation);

        // Move forward in the direction the joystick points
        Vector3 movement =
            m_Movement *
            walkSpeed *
            Time.fixedDeltaTime;

        m_Rigidbody.MovePosition(
            m_Rigidbody.position + movement
        );
    }

    void OnDestroy()
    {
        MoveAction.Disable();
    }
}