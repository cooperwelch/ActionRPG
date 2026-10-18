using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Projectile : MonoBehaviour
{
    [SerializeField] private float timeToLive = 3f;
    [SerializeField] private float spinSpeed = 720f;
    [SerializeField] private bool alignToVelocity = false;
    [SerializeField] private float damageCooldown = 0.5f;
    [SerializeField] private bool destroyOnHit = false;

    private int _damage;
    private Rigidbody2D _rigidBody;
    private SpriteRenderer _spriteRenderer;
    private ParticleSystemRenderer _particleRenderer;
    private float _spinDirection;
    private float _damageCooldownTimer;
    private bool _canDamageEnemies = true;
    private bool _canMineOre;
    private bool _canBreakBreakables;
    private bool _canBreakRocks;

    public void Initialize(
        int damage,
        Vector2 velocity,
        bool canDamageEnemies = true,
        bool canMineOre = false,
        bool canBreakBreakables = false,
        bool canBreakRocks = false)
    {
        _damage = damage;
        _canDamageEnemies = canDamageEnemies;
        _canMineOre = canMineOre;
        _canBreakBreakables = canBreakBreakables;
        _canBreakRocks = canBreakRocks;
        _rigidBody = GetComponent<Rigidbody2D>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _particleRenderer = GetComponentInChildren<ParticleSystemRenderer>();
        _rigidBody.velocity = velocity;
        _spinDirection = velocity.x < 0f ? 1f : -1f;

        if (!alignToVelocity && velocity.x < 0f)
        {
            transform.localScale = new Vector2(-transform.localScale.x, transform.localScale.y);
        }

        if (alignToVelocity && _particleRenderer != null && velocity.x < 0f)
        {
            _particleRenderer.flip = new Vector3(1f, 0f, 0f);
        }

        Destroy(gameObject, timeToLive);
    }

    private void Update()
    {
        if (alignToVelocity)
        {
            Vector2 vel = _rigidBody.velocity;
            bool goingLeft = vel.x < 0f;
            float angle = Mathf.Atan2(vel.y, Mathf.Abs(vel.x)) * Mathf.Rad2Deg;
            if (goingLeft) angle = -angle;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
            _spriteRenderer.flipX = goingLeft;
        }
        else
        {
            transform.Rotate(0f, 0f, spinSpeed * _spinDirection * Time.deltaTime);
        }

        if (_damageCooldownTimer > 0f)
            _damageCooldownTimer -= Time.deltaTime;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        float damageDirection = Utilities.DamageDirection(gameObject, collision.gameObject);
        bool hitEnemyOrOre = false;

        if (_damageCooldownTimer <= 0f)
        {
            if (_canDamageEnemies)
            {
                IDamageable target = collision.GetComponentInParent<IDamageable>();
                if (target != null)
                {
                    target.Damage(_damage, damageDirection);
                    hitEnemyOrOre = true;
                }
            }

            if (_canMineOre)
            {
                IMineable mineable = collision.GetComponentInParent<IMineable>();
                if (mineable != null)
                {
                    mineable.Mine(damageDirection);
                    hitEnemyOrOre = true;
                }
            }
        }

        bool hitBreakable = false;
        if (_canBreakBreakables || _canBreakRocks)
        {
            IBreakable breakable = collision.GetComponentInParent<IBreakable>();
            if (breakable != null && CanBreak(breakable))
            {
                breakable.Hit(damageDirection);
                hitBreakable = true;
            }
        }

        if (destroyOnHit && (hitEnemyOrOre || hitBreakable))
        {
            Destroy(gameObject);
            return;
        }

        if (hitEnemyOrOre)
        {
            _damageCooldownTimer = damageCooldown;
        }
    }

    private bool CanBreak(IBreakable breakable)
    {
        return breakable.Kind == BreakableKind.Rock
            ? _canBreakRocks
            : _canBreakBreakables;
    }
}
