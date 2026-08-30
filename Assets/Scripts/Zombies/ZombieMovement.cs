using UnityEngine;

public class ZombieMovement : MonoBehaviour
{
    [Header("Detection")]
    public float detectionRadius = 8f;
    public float attackDistance = 1.5f;

    [Header("Movement")]
    public float moveSpeed = 2f;
    public float turnSpeed = 360f;

    [Header("Attack")]
    public float attackDuration = 0.6f;
    public float attackCooldown = 1f;

    [Header("References")]
    public Transform player;
    public string playerTag = "Player";

    private Rigidbody m_Rigidbody;
    private Animator m_Animator;

    private bool m_IsRunning;
    private bool m_IsAttacking;

    private float m_AttackTimer;
    private float m_AttackCooldownTimer;

    private void Start()
    {
        m_Rigidbody = GetComponent<Rigidbody>();
        m_Animator = GetComponent<Animator>();

        FindPlayer();
    }

    private void FixedUpdate()
    {
        if (player == null)
        {
            FindPlayer();
            StopMoving();
            return;
        }

        UpdateAttackTimer();

        float distanceToPlayer =
            Vector3.Distance(transform.position, player.position);

        // Player is outside detection radius.
        if (distanceToPlayer > detectionRadius)
        {
            StopMoving();
            UpdateAnimation();
            return;
        }

        // Zombie is attacking.
        if (m_IsAttacking)
        {
            FacePlayer();
            UpdateAnimation();
            return;
        }

        // Player is close enough to attack.
        if (distanceToPlayer <= attackDistance)
        {
            StopMoving();
            TryAttack();
        }
        else
        {
            // Player detected, run toward player.
            MoveToPlayer();
        }

        UpdateAnimation();
    }

    // =========================
    // FIND PLAYER
    // =========================
    private void FindPlayer()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);

        if (playerObject != null)
        {
            player = playerObject.transform;
        }
    }

    // =========================
    // MOVEMENT
    // =========================
    private void MoveToPlayer()
    {
        m_IsRunning = true;

        Vector3 direction = player.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.001f)
        {
            StopMoving();
            return;
        }

        direction.Normalize();

        RotateTowards(direction);

        Vector3 movement =
            direction *
            moveSpeed *
            Time.fixedDeltaTime;

        m_Rigidbody.MovePosition(
            m_Rigidbody.position + movement
        );
    }

    private void StopMoving()
    {
        m_IsRunning = false;
    }

    private void RotateTowards(Vector3 direction)
    {
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

    private void FacePlayer()
    {
        Vector3 direction = player.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.001f)
            return;

        RotateTowards(direction.normalized);
    }

    // =========================
    // ATTACK
    // =========================
    private void TryAttack()
    {
        if (m_AttackCooldownTimer > 0f)
            return;

        m_IsAttacking = true;
        m_IsRunning = false;

        m_AttackTimer = attackDuration;
        m_AttackCooldownTimer = attackCooldown;

        FacePlayer();
    }

    private void UpdateAttackTimer()
    {
        if (m_AttackTimer > 0f)
        {
            m_AttackTimer -= Time.fixedDeltaTime;

            if (m_AttackTimer <= 0f)
            {
                m_IsAttacking = false;
            }
        }

        if (m_AttackCooldownTimer > 0f)
        {
            m_AttackCooldownTimer -= Time.fixedDeltaTime;
        }
    }

    // =========================
    // ANIMATION
    // =========================
    private void UpdateAnimation()
    {
        if (m_Animator == null)
            return;

        m_Animator.SetBool("IsRunning", m_IsRunning);
        m_Animator.SetBool("IsAttacking", m_IsAttacking);
    }

    // =========================
    // DEBUG
    // =========================
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackDistance);
    }
}