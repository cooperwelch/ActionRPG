using UnityEngine;

public class SubzoneCrab : SubzoneEnemy
{
    public enum CrabState { Hiding, Telegraphing, Appearing, Moving, Disappearing }

    [SerializeField] private float walkSpeed = 3f;
    [SerializeField] private float moveDuration = 1.5f;
    [SerializeField] private float appearDuration = 0.4f;
    [SerializeField] private float disappearDuration = 0.35f;
    [SerializeField] private float minHideDuration = 1f;
    [SerializeField] private float maxHideDuration = 2f;
    [SerializeField] private float burrowWarningDuration = 1.5f;

    private CrabState _state = CrabState.Hiding;
    private CrabSpawnZone _zone;
    private BoxCollider2D _hitCollider;
    private GameObject _burrowEffect;
    private float _moveDirection = 1f;
    private float _minX;
    private float _maxX;
    private float _surfaceY;
    private float _undergroundY;
    private float _phaseTimer;
    private float _phaseDuration;
    private Vector2 _moveStartPosition;
    private Vector2 _moveEndPosition;
    private bool _removedFromZone;

    public CrabState State => _state;

    public override void Awake()
    {
        base.Awake();
        _hitCollider = GetComponent<BoxCollider2D>();
        rigidBody.bodyType = RigidbodyType2D.Kinematic;
        rigidBody.gravityScale = 0f;
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();

        if (minHideDuration > maxHideDuration)
            (minHideDuration, maxHideDuration) = (maxHideDuration, minHideDuration);
    }
#endif

    public void Initialize(CrabSpawnZone zone, CrabEmergePlacement placement, SubzoneAudioManager audioManager)
    {
        _zone = zone;
        this.audioManager = audioManager;
        ApplyPlacement(placement);
        EnterHiding();
    }

    protected override bool CanBeStunned()
    {
        return enableHitStun && _state == CrabState.Moving;
    }

    public override void Update()
    {
        base.Update();

        if (_isDying || IsHitStunned)
            return;

        switch (_state)
        {
            case CrabState.Hiding:
                _phaseTimer -= Time.deltaTime;
                if (_phaseTimer <= 0f)
                    BeginTelegraph();
                break;

            case CrabState.Telegraphing:
                _phaseTimer -= Time.deltaTime;
                if (_phaseTimer <= 0f)
                    BeginAppear();
                break;

            case CrabState.Appearing:
                UpdateVerticalMovement();
                if (_phaseTimer >= _phaseDuration)
                    BeginMoving();
                break;

            case CrabState.Moving:
                UpdateMoving();
                if (_phaseTimer >= _phaseDuration)
                    BeginDisappear();
                break;

            case CrabState.Disappearing:
                UpdateVerticalMovement();
                if (_phaseTimer >= _phaseDuration)
                    EnterHiding();
                break;
        }
    }

    private void OnDestroy()
    {
        DestroyBurrowEffect();

        if (_zone == null || _removedFromZone)
            return;

        _removedFromZone = true;
        _zone.NotifyCrabRemoved(this);
    }

    private void ApplyPlacement(CrabEmergePlacement placement)
    {
        _moveDirection = placement.MoveDirection;
        _minX = placement.MinX;
        _maxX = placement.MaxX;
        _surfaceY = placement.SurfaceY;
        _undergroundY = placement.UndergroundY;

        transform.position = new Vector3(placement.SpawnX, _undergroundY, transform.position.z);
        SetFacing(_moveDirection);
    }

    private void EnterHiding()
    {
        _state = CrabState.Hiding;
        _phaseTimer = Random.Range(minHideDuration, maxHideDuration);
        transform.position = new Vector3(transform.position.x, _undergroundY, transform.position.z);
        SetColliderEnabled(false);
        SetSpriteVisible(false);
        _animator.SetBool("IsWalking", false);
    }

    private void BeginTelegraph()
    {
        CrabEmergePlacement placement = _zone.RequestEmergePlacement(this);
        ApplyPlacement(placement);

        _state = CrabState.Telegraphing;
        _phaseTimer = burrowWarningDuration;
        _burrowEffect = _zone.SpawnSandBurrowEffect(transform.position.x);
    }

    private void BeginAppear()
    {
        DestroyBurrowEffect();

        _state = CrabState.Appearing;
        _moveStartPosition = transform.position;
        _moveEndPosition = new Vector2(transform.position.x, _surfaceY);
        _phaseTimer = 0f;
        _phaseDuration = appearDuration;

        SetSpriteVisible(true);
        SetColliderEnabled(true);
        _animator.SetBool("IsWalking", false);
        _zone.SpawnSandEffect(transform.position.x);
        _zone.SpawnSandBalls(transform.position.x);
    }

    private void BeginMoving()
    {
        _state = CrabState.Moving;
        _phaseTimer = 0f;
        _phaseDuration = moveDuration;
        transform.position = new Vector3(transform.position.x, _surfaceY, transform.position.z);
        SetColliderEnabled(true);
        _animator.SetBool("IsWalking", true);
    }

    private void BeginDisappear()
    {
        _state = CrabState.Disappearing;
        _moveStartPosition = transform.position;
        _moveEndPosition = new Vector2(transform.position.x, _undergroundY);
        _phaseTimer = 0f;
        _phaseDuration = disappearDuration;
        SetColliderEnabled(true);
        _animator.SetBool("IsWalking", false);
        _zone.SpawnSandEffect(transform.position.x);
    }

    private void UpdateMoving()
    {
        _phaseTimer += Time.deltaTime;

        float newX = transform.position.x + _moveDirection * walkSpeed * Time.deltaTime;
        newX = Mathf.Clamp(newX, _minX, _maxX);
        transform.position = new Vector3(newX, _surfaceY, transform.position.z);
    }

    private void UpdateVerticalMovement()
    {
        _phaseTimer += Time.deltaTime;
        float t = Mathf.Clamp01(_phaseTimer / _phaseDuration);
        transform.position = Vector2.Lerp(_moveStartPosition, _moveEndPosition, QuarticEaseOut(t));
    }

    private void DestroyBurrowEffect()
    {
        if (_burrowEffect == null)
            return;

        Destroy(_burrowEffect);
        _burrowEffect = null;
    }

    private void SetFacing(float direction)
    {
        float scaleX = Mathf.Abs(transform.localScale.x);
        transform.localScale = new Vector3(direction >= 0f ? scaleX : -scaleX, transform.localScale.y, transform.localScale.z);
    }

    private void SetColliderEnabled(bool enabled)
    {
        if (_hitCollider != null)
            _hitCollider.enabled = enabled;
    }

    private void SetSpriteVisible(bool visible)
    {
        if (spriteRenderer != null)
            spriteRenderer.enabled = visible;
    }

    private static float QuarticEaseOut(float t)
    {
        float u = 1f - t;
        return 1f - u * u * u * u;
    }
}
