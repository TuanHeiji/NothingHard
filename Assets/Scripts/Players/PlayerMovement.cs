using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public InputAction MoveAction;

    [Header("Movement")]
    public float walkSpeed = 3f;

    [Header("Rotation")]
    public float turnSpeed = 720f;

    [Header("Auto Attack")]
    public float attackRadius = 2f;
    public float attackCooldown = 0.8f;
    public float attackDuration = 0.5f;
    public LayerMask zombieLayer;

    private Rigidbody m_Rigidbody;
    private Animator m_Animator;

    private Vector3 m_Movement;
    private Transform m_CurrentTarget;

    private float m_AttackCooldownTimer;
    private float m_AttackTimer;

    private void Start()
    {
        m_Rigidbody = GetComponent<Rigidbody>();
        m_Animator = GetComponent<Animator>();

        MoveAction.Enable();
    }

    private void FixedUpdate()
    {
        Move();
        Attack();
        UpdateAnimation();
    }

    // =========================
    // MOVEMENT
    // =========================
    private void Move()
    {
        Vector2 input = MoveAction.ReadValue<Vector2>();

        input = Vector2.ClampMagnitude(input, 1f);

        if (input.magnitude < 0.1f)
        {
            input = Vector2.zero;
        }

        Camera cam = Camera.main;

        if (cam == null)
        {
            m_Movement = Vector3.zero;
            return;
        }

        Vector3 cameraForward = cam.transform.forward;
        Vector3 cameraRight = cam.transform.right;

        cameraForward.y = 0f;
        cameraRight.y = 0f;

        cameraForward.Normalize();
        cameraRight.Normalize();

        m_Movement =
            cameraRight * input.x +
            cameraForward * input.y;

        m_Movement = Vector3.ClampMagnitude(m_Movement, 1f);

        if (m_Movement.magnitude <= 0.1f)
            return;

        Quaternion targetRotation =
            Quaternion.LookRotation(m_Movement);

        Quaternion smoothRotation =
            Quaternion.RotateTowards(
                m_Rigidbody.rotation,
                targetRotation,
                turnSpeed * Time.fixedDeltaTime
            );

        m_Rigidbody.MoveRotation(smoothRotation);

        Vector3 movement =
            m_Movement *
            walkSpeed *
            Time.fixedDeltaTime;

        m_Rigidbody.MovePosition(
            m_Rigidbody.position + movement
        );
    }

    // =========================
    // AUTO ATTACK
    // =========================
    private void Attack()
    {
        // Update timers.
        if (m_AttackCooldownTimer > 0f)
            m_AttackCooldownTimer -= Time.fixedDeltaTime;

        if (m_AttackTimer > 0f)
        {
            m_AttackTimer -= Time.fixedDeltaTime;

            // Keep facing the zombie during the attack.
            if (m_CurrentTarget != null)
            {
                FaceTarget(m_CurrentTarget);
            }

            if (m_AttackTimer <= 0f)
            {
                SetAttacking(false);
                m_CurrentTarget = null;
            }

            return;
        }

        // Find a zombie inside the attack circle.
        Transform target = FindNearestZombie();

        if (target == null)
        {
            m_CurrentTarget = null;
            SetAttacking(false);
            return;
        }

        m_CurrentTarget = target;

        // Face the zombie before attacking.
        FaceTarget(m_CurrentTarget);

        // Wait for the attack cooldown.
        if (m_AttackCooldownTimer > 0f)
            return;

        // Start attack.
        SetAttacking(true);

        m_AttackTimer = attackDuration;
        m_AttackCooldownTimer = attackCooldown;
    }

    private Transform FindNearestZombie()
    {
        Collider[] zombies = Physics.OverlapSphere(
            transform.position,
            attackRadius,
            zombieLayer
        );

        Transform nearestZombie = null;
        float nearestDistanceSqr = Mathf.Infinity;

        foreach (Collider zombie in zombies)
        {
            if (zombie == null)
                continue;

            float distanceSqr =
                (zombie.transform.position - transform.position).sqrMagnitude;

            if (distanceSqr < nearestDistanceSqr)
            {
                nearestDistanceSqr = distanceSqr;
                nearestZombie = zombie.transform;
            }
        }

        return nearestZombie;
    }

    private void FaceTarget(Transform target)
    {
        Vector3 direction = target.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.001f)
            return;

        Quaternion targetRotation =
            Quaternion.LookRotation(direction);

        Quaternion smoothRotation =
            Quaternion.RotateTowards(
                m_Rigidbody.rotation,
                targetRotation,
                turnSpeed * Time.fixedDeltaTime
            );

        m_Rigidbody.MoveRotation(smoothRotation);
    }

    // =========================
    // ANIMATION
    // =========================
    private void UpdateAnimation()
    {
        if (m_Animator == null)
            return;

        bool isRunning = m_Movement.magnitude > 0.1f;
        bool isAttacking = m_AttackTimer > 0f;

        m_Animator.SetBool("IsRunning", isRunning);
        m_Animator.SetBool("IsAttacking", isAttacking);
    }

    private void SetAttacking(bool value)
    {
        if (m_Animator != null)
        {
            m_Animator.SetBool("IsAttacking", value);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRadius);
    }

    private void OnDestroy()
    {
        MoveAction.Disable();
    }
}