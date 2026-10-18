using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEditor.Rendering;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(DamageFlash))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(CapsuleCollider2D))]
public class SubzoneEnemy : MonoBehaviour, IDamageable
{
    public float moveSpeed;
    public int attackDamage = 1;
    public int health;
    public SubzoneAudioManager audioManager;
    protected float patrolTime;
    protected Rigidbody2D rigidBody;
    protected SpriteRenderer spriteRenderer;
    protected DamageFlash damageFlash;
    protected Animator _animator;
    protected bool _isDying = false;
    [SerializeField] protected CameraShake cameraShake;
    [SerializeField] protected bool enableHitStun = false;
    [SerializeField] protected float hitStunDuration = 0.1f;

    private float _hitStunTimer;

    protected bool IsHitStunned => _hitStunTimer > 0f;

    public virtual void Awake()
    {
        rigidBody = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        damageFlash = GetComponent<DamageFlash>();
        _animator = GetComponent<Animator>();
    }

    public virtual void Update()
    {
        if (!enableHitStun && _hitStunTimer > 0f)
        {
            ClearHitStun();
            return;
        }

        if (!IsHitStunned)
            return;

        _hitStunTimer -= Time.deltaTime;
        if (_hitStunTimer <= 0f)
            ClearHitStun();
    }

    protected virtual bool CanBeStunned() => enableHitStun;

    protected void Flip()
    {
        transform.localScale = new Vector2(transform.localScale.x * -1, transform.localScale.y);

        moveSpeed *= -1;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            if (collision.gameObject.TryGetComponent<IDamageable>(out var player))
            {
                player.Damage(attackDamage, Utilities.DamageDirection(gameObject, collision.gameObject));
            }
        }
    }

    // IDamageable
    public virtual void Damage(int damage, float damageDirection)
    {
        if (health <= 0) return;

        health -= damage;

        if (audioManager != null)
            audioManager.PlayAttackHit();

        if (health <= 0)
        {
            health = 0;
            _isDying = true;
            _animator.SetBool("IsDead", true);
            rigidBody.velocity = Vector2.zero;
            ClearHitStun();

            if (TryGetComponent(out CapsuleCollider2D capsule))
            {
                capsule.enabled = false;
            } else if (TryGetComponent(out BoxCollider2D box))
            {
                box.enabled = false;
            }
            return;
        }

        damageFlash.Flash();

        if (CanBeStunned())
            BeginHitStun();
    }

    private void BeginHitStun()
    {
        if (hitStunDuration <= 0f)
            return;

        _hitStunTimer = hitStunDuration;
        _animator.speed = 0f;
    }

    private void ClearHitStun()
    {
        _hitStunTimer = 0f;
        if (_animator != null)
            _animator.speed = 1f;
    }

#if UNITY_EDITOR
    protected virtual void OnValidate()
    {
        hitStunDuration = Mathf.Max(0f, hitStunDuration);
    }
#endif

    public void EnemyDeathAnimationComplete()
    {
        Destroy(gameObject);
    }
}
