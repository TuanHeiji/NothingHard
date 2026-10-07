using System.Collections;
using UnityEngine;

public class ZombieMovement : MonoBehaviour
{
    [Header("Detection")]
    public float detectionRadius = 8f;
    public float attackDistance = 1.5f;

    [Header("Movement")]
    public float moveSpeed = 2f;
    public float turnSpeed = 360f;

    [Header("HP")]
    public float maxHP = 100f;
    public float currentHP;

    [Header("Attack")]
    public float attackDamage = 10f;
    public float attackDuration = 0.6f;
    public float attackCooldown = 1f;
    public float attackHitDelay = 0.25f;

    [Header("Death")]
    public float deathDuration = 1.2f;

    [Header("References")]
    public Transform player;

    private Rigidbody m_Rigidbody;
    private Animator m_Animator;

    private bool m_IsRunning;
    private bool m_IsAttacking;
    private bool m_IsDead;
    private bool m_HasDealtDamage;

    private float m_AttackTimer;
    private float m_AttackCooldownTimer;

    public bool IsDead => m_IsDead;

    private void Start()
    {
        m_Rigidbody = GetComponent<Rigidbody>();
        m_Animator = GetComponent<Animator>();
        currentHP = maxHP;

        if (player == null)
            FindPlayer();
    }

    private void FixedUpdate()
    {
        if (m_IsDead)
            return;

        if (player == null)
        {
            FindPlayer();
            StopMoving();
            UpdateAnimation();
            return;
        }

        UpdateAttackTimer();

        float distanceToPlayer = Vector3.Distance(
            transform.position,
            player.position
        );

        // Player ngoài vùng phát hiện -> đứng yên.
        if (distanceToPlayer > detectionRadius)
        {
            StopMoving();
            UpdateAnimation();
            return;
        }

        // Đang attack -> đứng yên và quay về phía Player.
        if (m_IsAttacking)
        {
            StopMoving();
            FacePlayer();

            // Đến thời điểm vũ khí chạm mục tiêu thì kiểm tra hit thật.
            if (!m_HasDealtDamage && m_AttackTimer <= attackDuration - attackHitDelay)
            {
                DealAttackDamage();
                m_HasDealtDamage = true;
            }

            UpdateAnimation();
            return;
        }

        // Player đủ gần -> attack.
        if (distanceToPlayer <= attackDistance)
        {
            StopMoving();
            TryAttack();
        }
        else
        {
            MoveToPlayer();
        }

        UpdateAnimation();
    }

    // =========================
    // FIND PLAYER
    // =========================
    private void FindPlayer()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
            player = playerObject.transform;
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

        m_Rigidbody.MovePosition(
            m_Rigidbody.position + direction * moveSpeed * Time.fixedDeltaTime
        );
    }

    private void StopMoving()
    {
        m_IsRunning = false;
    }

    private void RotateTowards(Vector3 direction)
    {
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        Quaternion smoothRotation = Quaternion.RotateTowards(
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

        if (direction.sqrMagnitude > 0.001f)
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
        m_HasDealtDamage = false;
        m_AttackTimer = attackDuration;

        FacePlayer();
    }

    private void UpdateAttackTimer()
    {
        if (m_AttackTimer > 0f)
        {
            m_AttackTimer -= Time.fixedDeltaTime;

            if (m_AttackTimer <= 0f)
                m_IsAttacking = false;
        }

        if (m_AttackCooldownTimer > 0f)
            m_AttackCooldownTimer -= Time.fixedDeltaTime;
    }

    private void DealAttackDamage()
    {
        // Kiểm tra collider Player thực sự nằm trong vùng đánh.
        int playerLayer = LayerMask.NameToLayer("Player");

        if (playerLayer == -1)
        {
            Debug.LogWarning("Chưa tạo Layer 'Player'. Zombie không thể gây damage.");
            return;
        }

        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            attackDistance,
            1 << playerLayer
        );

        foreach (Collider hit in hits)
        {
            PlayerMovement target = hit.GetComponentInParent<PlayerMovement>();

            if (target != null && !target.IsDead)
            {
                target.TakeDamage(attackDamage);
                break;
            }
        }

        m_AttackCooldownTimer = attackCooldown;
    }

    // =========================
    // RECEIVE DAMAGE / DEATH
    // =========================
    public void TakeDamage(float damage)
    {
        if (m_IsDead)
            return;

        currentHP = Mathf.Max(currentHP - damage, 0f);
        Debug.Log($"{name} nhận {damage} damage. HP: {currentHP}/{maxHP}");

        if (currentHP <= 0f)
            Die();
    }

    private void Die()
    {
        m_IsDead = true;
        m_IsRunning = false;
        m_IsAttacking = false;

        m_Animator.SetBool("IsRunning", false);
        m_Animator.SetBool("IsAttacking", false);
        m_Animator.SetBool("IsDeath", true);

        StartCoroutine(RemoveAfterDeath());
    }

    private IEnumerator RemoveAfterDeath()
    {
        // Chờ animation Death chạy xong rồi mới xoá Zombie.
        yield return new WaitForSeconds(deathDuration);
        Destroy(gameObject);
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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackDistance);
    }
}
