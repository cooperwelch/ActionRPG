using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Lives on the boss root. Player melee / projectiles hit the Head collider (Enemy layer)
// and find this via GetComponentInParent<IDamageable>().
public class SeaSerpentBossHealth : MonoBehaviour, IDamageable
{
    // Matches the 14-slot boss HUD meter (same as the Cyclops).
    [SerializeField] private int maxHealth = 14;
    [SerializeField] private SubzoneAudioManager audioManager;
    [SerializeField] private DamageFlash[] damageFlashes;

    public int CurrentHealth { get; private set; }
    public int MaxHealth => maxHealth;
    public bool IsDead => CurrentHealth <= 0;

    // Off until the fight actually starts (intro can't be cheesed).
    public bool IsVulnerable { get; set; }

    public event Action<int, int> OnHealthChanged;
    public event Action OnDied;

    private void Awake()
    {
        CurrentHealth = maxHealth;
    }

    // IDamageable
    public void Damage(int damage, float damageDirection)
    {
        if (!IsVulnerable || IsDead) return;

        if (audioManager != null)
        {
            audioManager.PlayAttackHit();
        }

        TakeDamage(damage);

        if (!IsDead)
        {
            foreach (DamageFlash flash in damageFlashes)
            {
                if (flash != null) flash.Flash();
            }
        }
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0) return;
        if (CurrentHealth <= 0) return;

        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);

        if (CurrentHealth == 0)
        {
            IsVulnerable = false;
            OnDied?.Invoke();
        }
    }
}
