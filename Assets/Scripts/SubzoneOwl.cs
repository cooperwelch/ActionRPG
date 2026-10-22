using System.Collections;
using UnityEngine;

public class SubzoneOwl : SubzoneEnemy
{
    public enum OwlState { Idle, Appearing, Attacking, FlyingAway }

    [SerializeField] private Transform player;
    [SerializeField] private BoxCollider2D _boxCollider;

    [SerializeField] private float swoopOvershoot = 1.5f;
    [SerializeField] private float windUpHeight = .2f;
    [SerializeField] private float windUpDuration = 0.4f;

    private OwlState _state = OwlState.Idle;
    private bool _isWindingUp;
    private Vector2 _windUpStart;
    private Vector2 _windUpEnd;
    private float _windUpTimer;
    private Vector2 _swoopStart;
    private Vector2 _swoopControl;
    private Vector2 _swoopTarget;
    private float _swoopTimer;
    private float _swoopDuration;
    private Vector2 _flyAwayDirection;
    private float _flyAwayTimer;
    private const float FLY_AWAY_LIFETIME = 5f;

    public void AppearTrigger()
    {
        _boxCollider.enabled = false;
        _animator.SetBool("IsAppearing", true);
        _state = OwlState.Appearing;
    }

    public void AppearAnimationComplete()
    {
        _animator.SetBool("IsAttacking", true);

        _windUpStart = rigidBody.position;
        _windUpEnd = _windUpStart + Vector2.up * windUpHeight;
        _windUpTimer = 0f;
        _isWindingUp = true;

        if (player.position.x > _windUpStart.x) { FlipFacingDirection(); }

        _state = OwlState.Attacking;
    }

    private void BeginSwoop()
    {
        _swoopStart = rigidBody.position;
        float approachDir = Mathf.Sign(player.position.x - _swoopStart.x);
        float targetX = player.position.x + approachDir * swoopOvershoot;
        _swoopTarget = new Vector2(targetX, player.position.y - 0.5f);
        _swoopControl = new Vector2(_swoopStart.x, _swoopTarget.y);
        _swoopDuration = Vector2.Distance(_swoopStart, _swoopTarget) / moveSpeed;
        _swoopTimer = 0f;
        _isWindingUp = false;
    }

    private void FixedUpdate()
    {
        if (_isDying || IsHitStunned)
            return;

        switch (_state)
        {
            case OwlState.Attacking:
                if (_isWindingUp)
                {
                    _windUpTimer += Time.fixedDeltaTime;
                    float wt = Mathf.Clamp01(_windUpTimer / windUpDuration);
                    rigidBody.MovePosition(Vector2.Lerp(_windUpStart, _windUpEnd, CubicEaseOut(wt)));

                    if (wt >= 1f)
                    {
                        BeginSwoop();
                    }
                }
                else
                {
                    _swoopTimer += Time.fixedDeltaTime;
                    float t = Mathf.Clamp01(_swoopTimer / _swoopDuration);
                    rigidBody.MovePosition(QuadraticBezier(_swoopStart, _swoopControl, _swoopTarget, t));

                    if (t >= 1f)
                    {
                        BeginFlyingAway();
                    }
                }
                break;

            case OwlState.FlyingAway:
                rigidBody.MovePosition(rigidBody.position + _flyAwayDirection * moveSpeed * Time.fixedDeltaTime);
                _flyAwayTimer += Time.fixedDeltaTime;
                if (_flyAwayTimer >= FLY_AWAY_LIFETIME)
                {
                    Destroy(gameObject);
                }
                break;
        }
    }

    private void BeginFlyingAway()
    {
        _animator.SetBool("IsFlyingAway", true);
        float horizontalDir = Mathf.Sign(_swoopTarget.x - _swoopStart.x);
        _flyAwayDirection = new Vector2(horizontalDir, 1f).normalized;
        _flyAwayTimer = 0f;
        _state = OwlState.FlyingAway;
    }

    private void FlipFacingDirection()
    {
        transform.localScale = new Vector2(transform.localScale.x * -1, transform.localScale.y);
    }

    private static Vector2 QuadraticBezier(Vector2 p0, Vector2 p1, Vector2 p2, float t)
    {
        float u = 1f - t;
        return u * u * p0 + 2f * u * t * p1 + t * t * p2;
    }

    private static float CubicEaseOut(float t)
    {
        float u = 1f - t;
        return 1f - u * u * u;
    }
}
