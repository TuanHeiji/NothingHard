using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public InputAction MoveAction;

    [Header("Movement")]
    public float walkSpeed = 3f;
    public float turnSpeed = 720f;

    [Header("HP")]
    public float maxHP = 100f;
    public float currentHP;

    [Header("Auto Attack")]
    public float attackRadius = 2f;
    public float attackDamage = 25f;
    public float attackCooldown = 0.8f;
    public float attackDuration = 0.5f;
    public float attackHitDelay = 0.2f;
    public LayerMask zombieLayer;

    [Header("Death")]
    public float deathDuration = 1.2f;

    private Rigidbody m_Rigidbody;
    private Animator m_Animator;
    private Vector3 m_Movement;
    private Transform m_CurrentTarget;

    private float m_AttackTimer;
    private float m_AttackCooldownTimer;
    private bool m_HasDealtDamage;
    private bool m_IsDead;

    public bool IsDead => m_IsDead;

    private void Start()
    {
        m_Rigidbody = GetComponent<Rigidbody>();
        m_Animator = GetComponent<Animator>();
        currentHP = maxHP;
        MoveAction.Enable();
    }

    private void FixedUpdate()
    {
        if (m_IsDead)
            return;

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
            input = Vector2.zero;

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

        m_Movement = cameraRight * input.x + cameraForward * input.y;
        m_Movement = Vector3.ClampMagnitude(m_Movement, 1f);

        if (m_Movement.magnitude <= 0.1f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(m_Movement);
        Quaternion smoothRotation = Quaternion.RotateTowards(
            m_Rigidbody.rotation,
            targetRotation,
            turnSpeed * Time.fixedDeltaTime
        );

        m_Rigidbody.MoveRotation(smoothRotation);
        m_Rigidbody.MovePosition(
            m_Rigidbody.position + m_Movement * walkSpeed * Time.fixedDeltaTime
        );
    }

    // =========================
    // AUTO ATTACK
    // =========================
    private void Attack()
    {
        UpdateAttackTimer();

        // Đang trong animation attack.
        if (m_AttackTimer > 0f)
        {
            if (m_CurrentTarget != null)
                FaceTarget(m_CurrentTarget);

            // Đến thời điểm vũ khí chạm mục tiêu thì kiểm tra hit thật.
            if (!m_HasDealtDamage && m_AttackTimer <= attackDuration - attackHitDelay)
            {
                DealAttackDamage();
                m_HasDealtDamage = true;
            }

            return;
        }

        m_CurrentTarget = FindNearestZombie();

        if (m_CurrentTarget == null || m_AttackCooldownTimer > 0f)
            return;

        FaceTarget(m_CurrentTarget);
        m_AttackTimer = attackDuration;
        m_HasDealtDamage = false;
    }

    private void UpdateAttackTimer()
    {
        if (m_AttackTimer > 0f)
            m_AttackTimer -= Time.fixedDeltaTime;

        if (m_AttackCooldownTimer > 0f)
            m_AttackCooldownTimer -= Time.fixedDeltaTime;
    }

    private void DealAttackDamage()
    {
        // OverlapSphere kiểm tra collider thật sự nằm trong vùng đánh.
        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            attackRadius,
            zombieLayer
        );

        foreach (Collider hit in hits)
        {
            ZombieMovement zombie = hit.GetComponentInParent<ZombieMovement>();

            if (zombie != null && !zombie.IsDead)
            {
                zombie.TakeDamage(attackDamage);
                break;
            }
        }

        m_AttackCooldownTimer = attackCooldown;
    }

    private Transform FindNearestZombie()
    {
        Collider[] zombies = Physics.OverlapSphere(
            transform.position,
            attackRadius,
            zombieLayer
        );

        Transform nearest = null;
        float nearestDistance = Mathf.Infinity;

        foreach (Collider zombieCollider in zombies)
        {
            ZombieMovement zombie = zombieCollider.GetComponentInParent<ZombieMovement>();

            if (zombie == null || zombie.IsDead)
                continue;

            float distance = (zombie.transform.position - transform.position).sqrMagnitude;

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = zombie.transform;
            }
        }

        return nearest;
    }

    private void FaceTarget(Transform target)
    {
        Vector3 direction = target.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        Quaternion smoothRotation = Quaternion.RotateTowards(
            m_Rigidbody.rotation,
            targetRotation,
            turnSpeed * Time.fixedDeltaTime
        );

        m_Rigidbody.MoveRotation(smoothRotation);
    }

    // =========================
    // RECEIVE DAMAGE / DEATH
    // =========================
    public void TakeDamage(float damage)
    {
        if (m_IsDead)
            return;

        currentHP = Mathf.Max(currentHP - damage, 0f);
        Debug.Log($"Player nhận {damage} damage. HP: {currentHP}/{maxHP}");

        if (currentHP <= 0f)
            Die();
    }

    private void Die()
    {
        m_IsDead = true;
        m_CurrentTarget = null;
        m_AttackTimer = 0f;
        m_Movement = Vector3.zero;

        m_Animator.SetBool("IsRunning", false);
        m_Animator.SetBool("IsAttacking", false);
        m_Animator.SetBool("IsDeath", true);

        StartCoroutine(StopSceneAfterDeath());
    }

    private IEnumerator StopSceneAfterDeath()
    {
        // Chờ animation Death chạy xong rồi mới dừng game.
        yield return new WaitForSecondsRealtime(deathDuration);
        Time.timeScale = 0f;
    }

    // =========================
    // ANIMATION
    // =========================
    private void UpdateAnimation()
    {
        if (m_Animator == null)
            return;

        m_Animator.SetBool("IsRunning", m_Movement.magnitude > 0.1f);
        m_Animator.SetBool("IsAttacking", m_AttackTimer > 0f);
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