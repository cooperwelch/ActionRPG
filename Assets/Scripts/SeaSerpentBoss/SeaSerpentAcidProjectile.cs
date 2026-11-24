using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Acid glob spat by the Sea Serpent.
// Flies on a ballistic arc in the ship's local space (so it lands on the deck even while the ship bobs),
// bursts on landing, burns for a while, then burns out and destroys itself.
// Hitting the player mid-flight splashes it (no lingering burn).
//
// Expected prefab layout:
//   AcidProjectile (this, Animator, kinematic Rigidbody2D, ball + burn trigger colliders)
//     Visual (SpriteRenderer) - animation clips key the sprite on this child
// The root's position is the point that touches the deck.
[RequireComponent(typeof(Rigidbody2D))]
public class SeaSerpentAcidProjectile : MonoBehaviour
{
    private enum Phase { Flying, Bursting, Burning, BurningOut, Splashing }

    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private Transform visual;
    [SerializeField] private Collider2D ballHitbox;
    [SerializeField] private Collider2D burnHitbox;

    [Header("Damage")]
    [SerializeField] private int ballDamage = 1;
    [SerializeField] private int burnDamage = 1;

    [Header("Timing")]
    [SerializeField] private float burstDuration = 0.33f;
    [SerializeField] private float burnDuration = 3f;
    [SerializeField] private float burnOutDuration = 0.3f;

    [Header("Visual Offsets")]
    // Ball sprite is centered on its pivot; lift it so the bottom of the ball is the contact point.
    [SerializeField] private float ballVisualOffsetY = 0.47f;
    // Burn sprites are 16x32 with a center pivot; lift 1 unit so the flame's base sits on the deck.
    [SerializeField] private float burnVisualOffsetY = 1f;

    private Phase phase;
    private Transform surface;
    private Vector2 localPos;
    private Vector2 localVelocity;
    private float gravity;
    private float landLocalY;
    private float phaseTimer;

    private void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
    }

    /// <summary>
    /// Launch toward a point in <paramref name="surface"/>'s local space, arriving after <paramref name="flightTime"/> seconds.
    /// </summary>
    public void Launch(Vector3 worldStart, Transform surface, Vector2 localTarget, float flightTime, float gravity)
    {
        this.surface = surface;
        this.gravity = gravity;

        localPos = surface != null ? (Vector2)surface.InverseTransformPoint(worldStart) : (Vector2)worldStart;
        landLocalY = localTarget.y;

        float t = Mathf.Max(0.05f, flightTime);
        localVelocity = new Vector2(
            (localTarget.x - localPos.x) / t,
            (localTarget.y - localPos.y + 0.5f * gravity * t * t) / t
        );

        SetPhase(Phase.Flying);
        SyncTransform();
    }

    private void Update()
    {
        switch (phase)
        {
            case Phase.Flying:
                localVelocity.y -= gravity * Time.deltaTime;
                localPos += localVelocity * Time.deltaTime;
                if (localVelocity.y < 0f && localPos.y <= landLocalY)
                {
                    localPos.y = landLocalY;
                    SetPhase(Phase.Bursting);
                }
                break;

            case Phase.Bursting:
                phaseTimer -= Time.deltaTime;
                if (phaseTimer <= 0f) SetPhase(Phase.Burning);
                break;

            case Phase.Burning:
                phaseTimer -= Time.deltaTime;
                if (phaseTimer <= 0f) SetPhase(Phase.BurningOut);
                break;

            case Phase.BurningOut:
            case Phase.Splashing:
                phaseTimer -= Time.deltaTime;
                if (phaseTimer <= 0f) Destroy(gameObject);
                break;
        }

        SyncTransform();
    }

    private void SyncTransform()
    {
        transform.position = surface != null ? surface.TransformPoint(localPos) : (Vector3)localPos;
    }

    private void SetPhase(Phase next)
    {
        phase = next;

        switch (next)
        {
            case Phase.Flying:
                SetVisualOffset(ballVisualOffsetY);
                SetHitboxes(ball: true, burn: false);
                animator.Play("Ball", 0, 0f);
                break;

            case Phase.Bursting:
                SetVisualOffset(burnVisualOffsetY);
                SetHitboxes(ball: false, burn: true);
                animator.Play("Burst", 0, 0f);
                phaseTimer = burstDuration;
                break;

            case Phase.Burning:
                animator.Play("Burn", 0, 0f);
                phaseTimer = burnDuration;
                break;

            case Phase.BurningOut:
                SetHitboxes(ball: false, burn: false);
                animator.Play("BurnOut", 0, 0f);
                phaseTimer = burnOutDuration;
                break;

            case Phase.Splashing:
                SetVisualOffset(burnVisualOffsetY);
                SetHitboxes(ball: false, burn: false);
                animator.Play("Burst", 0, 0f);
                phaseTimer = burstDuration;
                break;
        }
    }

    private void SetVisualOffset(float y)
    {
        if (visual != null) visual.localPosition = new Vector3(0f, y, 0f);
    }

    private void SetHitboxes(bool ball, bool burn)
    {
        if (ballHitbox != null) ballHitbox.enabled = ball;
        if (burnHitbox != null) burnHitbox.enabled = burn;
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;
        if (!collision.TryGetComponent<IDamageable>(out var player)) return;

        float direction = Utilities.DamageDirection(gameObject, collision.gameObject);

        if (phase == Phase.Flying)
        {
            player.Damage(ballDamage, direction);
            SetPhase(Phase.Splashing);
        }
        else if (phase == Phase.Bursting || phase == Phase.Burning)
        {
            // Player i-frames gate repeat hits while standing in the fire.
            player.Damage(burnDamage, direction);
        }
    }
}
