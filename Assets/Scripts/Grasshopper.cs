using System.Collections.Generic;
using UnityEngine;

public class Grasshopper : SubzoneEnemy
{
    public enum GrasshopperState { Idle, Jumping }

    [SerializeField] private Transform player;
    [SerializeField] private float detectionRange = 7f;
    [SerializeField] private float jumpForceX = 4f;
    [SerializeField] private float jumpForceY = 8f;
    [SerializeField] private float restDuration = 2f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckDistance = 0.15f;

    private GrasshopperState _state = GrasshopperState.Idle;
    private float _restTimer;
    private bool _hasLeftGround;
    private float _jumpVelocityX;
    private CapsuleCollider2D _collider;
    private ContactFilter2D _oneWayOverlapFilter;
    private readonly List<Collider2D> _overlapScratch = new List<Collider2D>(16);
    private readonly HashSet<Collider2D> _oneWayTouched = new HashSet<Collider2D>();
    private readonly HashSet<Collider2D> _oneWayManagedColliders = new HashSet<Collider2D>();

    public GrasshopperState State => _state;

    private const RigidbodyConstraints2D IdleConstraints =
        RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;
    private const RigidbodyConstraints2D JumpingConstraints =
        RigidbodyConstraints2D.FreezeRotation;

    public override void Awake()
    {
        base.Awake();
        _collider = GetComponent<CapsuleCollider2D>();
        _restTimer = 0f;
        rigidBody.constraints = IdleConstraints;
        _oneWayOverlapFilter = default;
        _oneWayOverlapFilter.NoFilter();
    }

    void OnDisable()
    {
        if (_collider == null)
            return;
        foreach (var c in _oneWayManagedColliders)
        {
            if (c != null)
                Physics2D.IgnoreCollision(_collider, c, false);
        }
        _oneWayManagedColliders.Clear();
    }

    void FixedUpdate()
    {
        if (_isDying || !_collider.enabled || IsHitStunned)
            return;

        if (_state == GrasshopperState.Jumping)
        {
            rigidBody.velocity = new Vector2(_jumpVelocityX, rigidBody.velocity.y);
        }

        UpdateOneWayPlatformCollisionIgnoring();
    }

    /// <summary>
    /// Same rule as the player: only collide with one-ways when feet are above the composite top
    /// (<see cref="OneWayPlatformGrounding"/>). Toggles IgnoreCollision so pass-through matches PlatformEffector2D intent.
    /// </summary>
    void UpdateOneWayPlatformCollisionIgnoring()
    {
        _oneWayTouched.Clear();
        _overlapScratch.Clear();
        _collider.OverlapCollider(_oneWayOverlapFilter, _overlapScratch);

        for (int i = 0; i < _overlapScratch.Count; i++)
        {
            Collider2D c = _overlapScratch[i];
            if (c == null || c.gameObject == gameObject)
                continue;
            PlatformEffector2D pe = c.GetComponentInParent<PlatformEffector2D>();
            if (pe == null || !pe.useOneWay)
                continue;
            _oneWayTouched.Add(c);
        }

        foreach (var c in _oneWayManagedColliders)
        {
            if (!_oneWayTouched.Contains(c))
                Physics2D.IgnoreCollision(_collider, c, false);
        }

        _oneWayManagedColliders.Clear();
        float lowestFootY = LowestFootWorldY();

        foreach (var c in _oneWayTouched)
        {
            bool ignore = !OneWayPlatformGrounding.ColliderCountsAsGround(c, lowestFootY);
            Physics2D.IgnoreCollision(_collider, c, ignore);
            _oneWayManagedColliders.Add(c);
        }
    }

    public override void Update()
    {
        base.Update();

        if (_isDying)
        {
            rigidBody.bodyType = RigidbodyType2D.Kinematic;
            rigidBody.velocity = Vector2.zero;
            return;
        }

        if (IsHitStunned)
            return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        switch (_state)
        {
            case GrasshopperState.Idle:
                _restTimer -= Time.deltaTime;

                if (distanceToPlayer <= detectionRange && _restTimer <= 0f)
                {
                    FacePlayer();
                    Jump();
                }
                break;

            case GrasshopperState.Jumping:
                if (!_hasLeftGround && !IsGrounded())
                {
                    _hasLeftGround = true;
                }

                if (_hasLeftGround && IsGrounded())
                {
                    Land();
                }
                break;
        }
    }

    private void Jump()
    {
        _state = GrasshopperState.Jumping;
        _hasLeftGround = false;
        _animator.SetBool("IsJumping", true);
        rigidBody.constraints = JumpingConstraints;

        float directionX = Mathf.Sign(player.position.x - transform.position.x);
        _jumpVelocityX = directionX * jumpForceX;
        rigidBody.velocity = new Vector2(_jumpVelocityX, 0f);
        rigidBody.AddForce(new Vector2(0f, jumpForceY), ForceMode2D.Impulse);
    }

    private void Land()
    {
        _state = GrasshopperState.Idle;
        _hasLeftGround = false;
        _restTimer = restDuration;
        _animator.SetBool("IsJumping", false);
        rigidBody.constraints = IdleConstraints;

        rigidBody.velocity = new Vector2(0f, rigidBody.velocity.y);
    }

    private void FacePlayer()
    {
        float direction = player.position.x - transform.position.x;
        if ((direction > 0f && transform.localScale.x > 0f) ||
            (direction < 0f && transform.localScale.x < 0f))
        {
            transform.localScale = new Vector2(-transform.localScale.x, transform.localScale.y);
        }
    }

    Vector2 GetCapsuleBottomWorld()
    {
        return (Vector2)transform.position + _collider.offset - new Vector2(0f, _collider.size.y * 0.5f);
    }

    float LowestFootWorldY()
    {
        return Mathf.Min(GetCapsuleBottomWorld().y, _collider.bounds.min.y);
    }

    private bool IsGrounded()
    {
        Vector2 origin = GetCapsuleBottomWorld();
        float lowestFootY = LowestFootWorldY();
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, groundCheckDistance, groundLayer);
        if (hit.collider == null)
            return false;
        return OneWayPlatformGrounding.ColliderCountsAsGround(hit.collider, lowestFootY);
    }
}
