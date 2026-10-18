using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class Breakable : MonoBehaviour, IBreakable
{
    [System.Serializable]
    public struct DropEntry
    {
        public GameObject prefab;
        [Range(0f, 1f)] public float chance;
    }

    private const float DropLaunchSpeedY = 6f;
    private const float DropMinHorizontalSpeed = 0.5f;
    private const float DropMaxHorizontalSpeed = 2f;

    [Header("Hits")]
    [SerializeField] private BreakableKind kind = BreakableKind.Standard;
    [SerializeField] private int hitsToBreak = 1;
    [SerializeField] private Sprite[] stageSprites;

    [Header("Drops")]
    [SerializeField] private DropEntry[] dropEntries;
    [SerializeField] private bool useHitDirectionForDrops = true;

    [Header("FX")]
    [SerializeField] private GameObject destroyedFXObject;

    [Header("Collision")]
    [SerializeField] private bool blocksPlayer = true;
    [SerializeField] private Collider2D bodyCollider;

    private SpriteRenderer spriteRenderer;
    private int hitsTaken;
    private bool isDestroyed;

    public BreakableKind Kind => kind;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        hitsToBreak = Mathf.Max(1, hitsToBreak);
        if (bodyCollider != null)
        {
            bodyCollider.enabled = blocksPlayer;
        }
    }

    public void Hit(float damageDirection)
    {
        if (isDestroyed)
        {
            return;
        }

        hitsTaken++;

        if (hitsTaken >= hitsToBreak)
        {
            DestroyBreakable(damageDirection);
            return;
        }

        ApplyStageSprite();
    }

    private void ApplyStageSprite()
    {
        if (stageSprites == null || stageSprites.Length == 0)
        {
            return;
        }

        // Ore-style: stageSprites[0] is after first hit, stageSprites[1] after second, etc.
        int spriteIndex = hitsTaken - 1;
        if (spriteIndex < 0 || spriteIndex >= stageSprites.Length)
        {
            return;
        }

        if (stageSprites[spriteIndex] != null)
        {
            spriteRenderer.sprite = stageSprites[spriteIndex];
        }
    }

    private void DestroyBreakable(float damageDirection)
    {
        isDestroyed = true;
        SpawnDrops(damageDirection);

        if (destroyedFXObject != null)
        {
            Instantiate(destroyedFXObject, transform.position, Quaternion.identity);
        }

        Destroy(gameObject);
    }

    private void SpawnDrops(float damageDirection)
    {
        if (dropEntries == null || dropEntries.Length == 0)
        {
            return;
        }

        Collider2D[] breakableColliders = GetComponentsInChildren<Collider2D>();

        foreach (DropEntry entry in dropEntries)
        {
            if (entry.prefab == null || entry.chance <= 0f)
            {
                continue;
            }

            if (Random.value >= entry.chance)
            {
                continue;
            }

            GameObject drop = Instantiate(entry.prefab, transform.position, Quaternion.identity);
            if (!drop.TryGetComponent<Rigidbody2D>(out var rigidBody))
            {
                continue;
            }

            float horizontal = 0f;
            if (useHitDirectionForDrops)
            {
                float horizontalSign = damageDirection < 0f ? -1f : 1f;
                horizontal = Random.Range(DropMinHorizontalSpeed, DropMaxHorizontalSpeed) * horizontalSign;
            }

            rigidBody.velocity = new Vector2(horizontal, DropLaunchSpeedY);

            IgnoreCollisionsWithBreakable(drop, breakableColliders);
        }
    }

    private static void IgnoreCollisionsWithBreakable(GameObject drop, Collider2D[] breakableColliders)
    {
        if (breakableColliders == null || breakableColliders.Length == 0)
        {
            return;
        }

        Collider2D[] dropColliders = drop.GetComponentsInChildren<Collider2D>();
        foreach (Collider2D dropCollider in dropColliders)
        {
            if (dropCollider == null)
            {
                continue;
            }

            foreach (Collider2D breakableCollider in breakableColliders)
            {
                if (breakableCollider == null)
                {
                    continue;
                }

                Physics2D.IgnoreCollision(dropCollider, breakableCollider);
            }
        }
    }
}
