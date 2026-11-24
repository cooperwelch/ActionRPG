using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Trigger on the serpent's snout. Only enabled during the active frames of the bite
// (driven by animation events through SeaSerpentBossAnimatorBridge).
// Keep this OFF the Enemy layer so player swings that overlap it don't double-hit the boss.
[RequireComponent(typeof(Collider2D))]
public class SeaSerpentBossBiteHitbox : MonoBehaviour
{
    [SerializeField] private int damage = 2;

    private Collider2D hitbox;
    private SeaSerpentBossHealth health;

    private void Awake()
    {
        hitbox = GetComponent<Collider2D>();
        hitbox.isTrigger = true;
        hitbox.enabled = false;
        health = GetComponentInParent<SeaSerpentBossHealth>();
    }

    public void SetActive(bool active)
    {
        if (active && health != null && health.IsDead) return;
        hitbox.enabled = active;
    }

    // Stay (not just Enter) so a player standing in the bite zone when it activates still gets hit.
    // The player's i-frames in Attack.Damage stop repeat hits.
    private void OnTriggerStay2D(Collider2D collision)
    {
        if (!hitbox.enabled) return;
        if (!collision.CompareTag("Player")) return;

        if (collision.TryGetComponent<IDamageable>(out var player))
        {
            player.Damage(damage, Utilities.DamageDirection(gameObject, collision.gameObject));
        }
    }
}
