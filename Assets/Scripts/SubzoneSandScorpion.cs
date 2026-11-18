using UnityEngine;

/// <summary>
/// Sand-mound ambush enemy: buries until the player approaches, hops out toward them,
/// then patrols with a biased approach/retreat mix and a telegraphed horizontal lunge.
/// </summary>
public class SubzoneSandScorpion : SubzoneEnemy
{
    public enum SandScorpionState
    {
        Hiding,
        Emerging,
        Walking,
        Telegraphing,
        Lunging
    }

    [Header("References")]
    [SerializeField] private Transform player;

    [Header("Detection")]
    [SerializeField] private float wakeTriggerDistance = 6f;
    [SerializeField] private float attackTriggerDistance = 2.5f;
    [SerializeField] private Vector2 triggerCenterOffset = new Vector2(0f, -0.5f);
    [SerializeField] private float facingFlipThreshold = 0.2f;

    [Header("Emerge Hop")]
    [SerializeField] private float emergeHopDistance = 2f;
    [SerializeField] private float emergeHopHeight = 1.5f;

    [Header("Emerge FX")]
    [SerializeField] private SandEffect sandEffectPrefab;
    [SerializeField] private float sandEffectYOffset = -0.5f;
    [SerializeField] private int sandEffectCount = 3;

    [Header("Walking")]
    [SerializeField] private float walkSpeed = 2f;
    [SerializeField] [Range(0f, 1f)] private float approachChance = 0.45f;
    [SerializeField] private float directionReconsiderInterval = 2f;
    [SerializeField] private float wallCheckDistance = 0.2f;

    [Header("Attack")]
    [SerializeField] private int lungeAttackDamage = 2;
    [SerializeField] private float telegraphDuration = 0.4f;
    [SerializeField] private float attackLungeDistance = 2.5f;
    [SerializeField] private float attackLungeDuration = 0.25f;
    [SerializeField] private float attackCooldown = 1.5f;

    [Header("Ground Check")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckDistance = 0.15f;

    private SandScorpionState _state = SandScorpionState.Hiding;
    private CapsuleCollider2D _collider;
    private bool _approachPlayer = true;
    private float _directionTimer;
    private float _phaseTimer;
    private float _attackCooldownTimer;
    private bool _hasLeftGround;
    private float _minEmergeAirTime;
    private float _hopVelocityX;
    private float _lungeDirection;
    private float _lungeSpeed;
    private float _lungeDistanceTraveled;
    private Vector2 _lungeStartPosition;
    private float _defaultGravityScale = 1.5f;
    private int _contactDamage;

    private static readonly int AnimIsBuried = Animator.StringToHash("IsBuried");
    private static readonly int AnimIsEmerging = Animator.StringToHash("IsEmerging");
    private static readonly int AnimIsWalking = Animator.StringToHash("IsWalking");
    private static readonly int AnimIsTelegraphing = Animator.StringToHash("IsTelegraphing");
    private static readonly int AnimIsAttacking = Animator.StringToHash("IsAttacking");

    public SandScorpionState State => _state;

    public override void Awake()
    {
        base.Awake();

        _collider = GetComponent<CapsuleCollider2D>();
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
                player = playerObject.transform;
        }

        _contactDamage = attackDamage;
        if (health <= 0)
            health = 4;

        _defaultGravityScale = rigidBody.gravityScale > 0f ? rigidBody.gravityScale : 1.5f;
        EnterHiding();
    }

    void FixedUpdate()
    {
        if (_isDying || IsHitStunned || _collider == null || !_collider.enabled)
            return;

        switch (_state)
        {
            case SandScorpionState.Emerging:
                // Keep hop horizontal velocity stable while gravity handles the arc.
                rigidBody.velocity = new Vector2(_hopVelocityX, rigidBody.velocity.y);
                break;

            case SandScorpionState.Walking:
                UpdateWalkingMovement();
                break;

            case SandScorpionState.Telegraphing:
                rigidBody.velocity = new Vector2(0f, rigidBody.velocity.y);
                break;

            case SandScorpionState.Lunging:
                UpdateLungeMovement();
                break;
        }
    }

    public override void Update()
    {
        base.Update();

        if (_isDying)
        {
            rigidBody.velocity = Vector2.zero;
            return;
        }

        if (IsHitStunned)
            return;

        if (_attackCooldownTimer > 0f)
            _attackCooldownTimer -= Time.deltaTime;

        switch (_state)
        {
            case SandScorpionState.Hiding:
                UpdateHiding();
                break;

            case SandScorpionState.Emerging:
                UpdateEmerging();
                break;

            case SandScorpionState.Walking:
                UpdateWalking();
                break;

            case SandScorpionState.Telegraphing:
                UpdateTelegraphing();
                break;

            case SandScorpionState.Lunging:
                UpdateLunging();
                break;
        }
    }

    public override void Damage(int damage, float damageDirection)
    {
        if (_state == SandScorpionState.Hiding)
            return;

        base.Damage(damage, damageDirection);
    }

    protected override bool CanBeStunned()
    {
        // Telegraph and lunge should never be interrupted by hit stun.
        return enableHitStun
            && _state != SandScorpionState.Telegraphing
            && _state != SandScorpionState.Lunging;
    }

    /// <summary>
    /// Animation event hook: add to the end of the telegraph clip when art is ready.
    /// </summary>
    public void TelegraphAnimationComplete()
    {
        if (_state != SandScorpionState.Telegraphing)
            return;

        BeginLunge();
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        wakeTriggerDistance = Mathf.Max(0.01f, wakeTriggerDistance);
        attackTriggerDistance = Mathf.Max(0.01f, attackTriggerDistance);
        facingFlipThreshold = Mathf.Max(0f, facingFlipThreshold);
        emergeHopDistance = Mathf.Max(0.01f, emergeHopDistance);
        emergeHopHeight = Mathf.Max(0.01f, emergeHopHeight);
        walkSpeed = Mathf.Max(0f, walkSpeed);
        directionReconsiderInterval = Mathf.Max(0.01f, directionReconsiderInterval);
        wallCheckDistance = Mathf.Max(0.01f, wallCheckDistance);
        telegraphDuration = Mathf.Max(0.01f, telegraphDuration);
        attackLungeDistance = Mathf.Max(0.01f, attackLungeDistance);
        attackLungeDuration = Mathf.Max(0.01f, attackLungeDuration);
        attackCooldown = Mathf.Max(0f, attackCooldown);
        groundCheckDistance = Mathf.Max(0.01f, groundCheckDistance);
        lungeAttackDamage = Mathf.Max(0, lungeAttackDamage);
        sandEffectCount = Mathf.Max(0, sandEffectCount);
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 origin = GetTriggerCenter();
        Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.35f);
        Gizmos.DrawWireSphere(origin, wakeTriggerDistance);
        Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.45f);
        Gizmos.DrawWireSphere(origin, attackTriggerDistance);
    }
#endif

    private Vector2 GetTriggerCenter()
    {
        return (Vector2)transform.position + triggerCenterOffset;
    }

    private void EnterHiding()
    {
        _state = SandScorpionState.Hiding;
        attackDamage = _contactDamage;
        SetColliderEnabled(false);

        rigidBody.velocity = Vector2.zero;
        rigidBody.bodyType = RigidbodyType2D.Kinematic;
        rigidBody.gravityScale = 0f;

        SetAnimatorFlags(buried: true);
    }

    private void UpdateHiding()
    {
        if (!IsPlayerAlive())
            return;

        if (Vector2.Distance(GetTriggerCenter(), player.position) <= wakeTriggerDistance)
            BeginEmerging();
    }

    private void BeginEmerging()
    {
        _state = SandScorpionState.Emerging;
        _hasLeftGround = false;
        _phaseTimer = 0f;

        SetColliderEnabled(true);
        attackDamage = _contactDamage;

        rigidBody.bodyType = RigidbodyType2D.Dynamic;
        rigidBody.gravityScale = _defaultGravityScale;
        rigidBody.constraints = RigidbodyConstraints2D.FreezeRotation;

        FacePlayer();

        float directionX = GetDirectionTowardPlayer();
        float gravity = Mathf.Abs(Physics2D.gravity.y * rigidBody.gravityScale);
        if (gravity < 0.01f)
            gravity = 9.81f * 1.5f;

        float velocityY = Mathf.Sqrt(2f * gravity * emergeHopHeight);
        float airTime = (2f * velocityY) / gravity;
        // Don't allow "landed" until well into the hop — avoids ground-check flicker ending emerge early.
        _minEmergeAirTime = airTime * 0.35f;
        _hopVelocityX = directionX * (emergeHopDistance / Mathf.Max(0.01f, airTime));
        rigidBody.velocity = new Vector2(_hopVelocityX, velocityY);

        SpawnEmergeSandEffects();
        SetAnimatorFlags(emerging: true);
    }

    private void SpawnEmergeSandEffects()
    {
        if (sandEffectPrefab == null || sandEffectCount <= 0)
            return;

        float spacing = GetSandEffectWidth();
        float startX = -spacing * (sandEffectCount - 1) * 0.5f;
        Vector3 basePosition = transform.position + new Vector3(0f, sandEffectYOffset, 0f);

        for (int i = 0; i < sandEffectCount; i++)
        {
            Vector3 position = basePosition + new Vector3(startX + spacing * i, 0f, 0f);
            Instantiate(sandEffectPrefab, position, Quaternion.identity);
        }
    }

    private float GetSandEffectWidth()
    {
        if (sandEffectPrefab != null
            && sandEffectPrefab.TryGetComponent(out SpriteRenderer spriteRenderer)
            && spriteRenderer.sprite != null)
        {
            return Mathf.Max(0.01f, spriteRenderer.sprite.bounds.size.x * Mathf.Abs(sandEffectPrefab.transform.localScale.x));
        }

        return 1f;
    }

    private void UpdateEmerging()
    {
        FacePlayer();
        _phaseTimer += Time.deltaTime;

        // Require a clear upward liftoff so a one-frame ground miss can't count as leaving.
        if (!_hasLeftGround
            && !IsGrounded()
            && rigidBody.velocity.y > 0.1f
            && _phaseTimer >= 0.05f)
        {
            _hasLeftGround = true;
        }

        // Stay emerging until the hop has come back down onto ground.
        bool finishedHop =
            _hasLeftGround
            && _phaseTimer >= _minEmergeAirTime
            && IsGrounded()
            && rigidBody.velocity.y <= 0.1f;

        // Long timeout only — if the hop never registers liftoff, don't soft-lock forever.
        bool fallbackLand = !_hasLeftGround && _phaseTimer > 2f && IsGrounded();

        if (finishedHop || fallbackLand)
            BeginWalking();
    }

    private void BeginWalking()
    {
        _state = SandScorpionState.Walking;
        attackDamage = _contactDamage;
        _directionTimer = 0f;
        _approachPlayer = true;
        rigidBody.velocity = new Vector2(0f, rigidBody.velocity.y);

        SetAnimatorFlags(walking: true);
    }

    private void UpdateWalking()
    {
        if (IsPlayerAlive())
            FacePlayer();

        _directionTimer += Time.deltaTime;
        if (_directionTimer >= directionReconsiderInterval)
        {
            _directionTimer = 0f;
            _approachPlayer = Random.value <= approachChance;
        }

        if (IsPlayerAlive()
            && _attackCooldownTimer <= 0f
            && Vector2.Distance(GetTriggerCenter(), player.position) <= attackTriggerDistance
            && IsGrounded())
        {
            BeginTelegraph();
        }
    }

    private void UpdateWalkingMovement()
    {
        FacePlayer();

        float moveDirection = GetWalkMoveDirection();

        // Wall in the current move direction — reverse walk intent immediately.
        if (IsWallInDirection(moveDirection))
        {
            _approachPlayer = !_approachPlayer;
            _directionTimer = 0f;
            moveDirection = GetWalkMoveDirection();
        }

        rigidBody.velocity = new Vector2(moveDirection * walkSpeed, rigidBody.velocity.y);
    }

    private void BeginTelegraph()
    {
        _state = SandScorpionState.Telegraphing;
        _phaseTimer = 0f;
        attackDamage = _contactDamage;
        rigidBody.velocity = new Vector2(0f, rigidBody.velocity.y);
        FacePlayer();
        SetAnimatorFlags(telegraphing: true);
    }

    private void UpdateTelegraphing()
    {
        FacePlayer();
        _phaseTimer += Time.deltaTime;
        if (_phaseTimer >= telegraphDuration)
            BeginLunge();
    }

    private void BeginLunge()
    {
        _state = SandScorpionState.Lunging;
        _phaseTimer = 0f;
        _lungeDistanceTraveled = 0f;
        _lungeStartPosition = transform.position;
        attackDamage = lungeAttackDamage;

        FacePlayer();
        _lungeDirection = GetDirectionTowardPlayer();
        // Peak speed for cubic ease-out: distance/duration * 3 at t=0.
        _lungeSpeed = attackLungeDistance / Mathf.Max(0.01f, attackLungeDuration);
        UpdateLungeMovement();

        SetAnimatorFlags(attacking: true);
    }

    private void UpdateLunging()
    {
        _phaseTimer += Time.deltaTime;
        _lungeDistanceTraveled = Mathf.Abs(transform.position.x - _lungeStartPosition.x);

        bool finishedByDistance = _lungeDistanceTraveled >= attackLungeDistance;
        bool finishedByTime = _phaseTimer >= attackLungeDuration;

        if (finishedByDistance || finishedByTime)
            EndLunge();
    }

    private void UpdateLungeMovement()
    {
        float duration = Mathf.Max(0.01f, attackLungeDuration);
        float t = Mathf.Clamp01(_phaseTimer / duration);
        // Derivative of cubic ease-out (1 - (1-t)^3) => 3(1-t)^2: fast start, ease to stop.
        float easeOutSpeedFactor = 3f * (1f - t) * (1f - t);
        float speed = _lungeSpeed * easeOutSpeedFactor;

        rigidBody.velocity = new Vector2(
            _lungeDirection * speed,
            Mathf.Min(0f, rigidBody.velocity.y)
        );
    }

    private void EndLunge()
    {
        attackDamage = _contactDamage;
        _attackCooldownTimer = attackCooldown;
        rigidBody.velocity = new Vector2(0f, rigidBody.velocity.y);
        BeginWalking();
    }

    private float GetWalkMoveDirection()
    {
        float towardPlayer = GetDirectionTowardPlayer();
        return _approachPlayer ? towardPlayer : -towardPlayer;
    }

    private bool IsPlayerAlive()
    {
        return player != null && PlayerStats.Health > 0;
    }

    private float GetFacingMoveDirection()
    {
        // Positive localScale.x faces left (same convention as Grasshopper).
        return transform.localScale.x >= 0f ? -1f : 1f;
    }

    private float GetDirectionTowardPlayer()
    {
        if (!IsPlayerAlive())
            return GetFacingMoveDirection();

        float deltaX = player.position.x - transform.position.x;
        if (Mathf.Abs(deltaX) < facingFlipThreshold)
            return GetFacingMoveDirection();

        return Mathf.Sign(deltaX);
    }

    private void FacePlayer()
    {
        if (!IsPlayerAlive())
            return;

        // Positive localScale.x faces left — match Grasshopper.
        float direction = player.position.x - transform.position.x;
        // Deadband avoids rapid flipping when nearly overlapping the player's X.
        if (Mathf.Abs(direction) < facingFlipThreshold)
            return;

        if ((direction > 0f && transform.localScale.x > 0f) ||
            (direction < 0f && transform.localScale.x < 0f))
        {
            transform.localScale = new Vector3(
                -transform.localScale.x,
                transform.localScale.y,
                transform.localScale.z
            );
        }
    }

    private bool IsWallInDirection(float direction)
    {
        if (Mathf.Abs(direction) < 0.001f || _collider == null)
            return false;

        float dirX = Mathf.Sign(direction);
        Bounds bounds = _collider.bounds;
        Vector2 origin = bounds.center;
        // Reach past the collider edge so outline walls still register when pressed against them.
        float distance = bounds.extents.x + wallCheckDistance;

        RaycastHit2D[] hits = Physics2D.RaycastAll(
            origin,
            new Vector2(dirX, 0f),
            distance,
            groundLayer
        );

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hitCollider = hits[i].collider;
            if (hitCollider == null || hitCollider == _collider || hitCollider.gameObject == gameObject)
                continue;

            return true;
        }

        return false;
    }

    private bool IsGrounded()
    {
        if (_collider == null)
            return false;

        Vector2 origin = (Vector2)transform.position + _collider.offset
            - new Vector2(0f, _collider.size.y * 0.5f);
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, groundCheckDistance, groundLayer);
        return hit.collider != null;
    }

    private void SetColliderEnabled(bool enabled)
    {
        if (_collider != null)
            _collider.enabled = enabled;
    }

    private void SetAnimatorFlags(
        bool buried = false,
        bool emerging = false,
        bool walking = false,
        bool telegraphing = false,
        bool attacking = false)
    {
        if (_animator == null)
            return;

        _animator.SetBool(AnimIsBuried, buried);
        _animator.SetBool(AnimIsEmerging, emerging);
        _animator.SetBool(AnimIsWalking, walking);
        _animator.SetBool(AnimIsTelegraphing, telegraphing);
        _animator.SetBool(AnimIsAttacking, attacking);
    }
}
