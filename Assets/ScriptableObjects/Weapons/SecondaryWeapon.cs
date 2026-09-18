using UnityEngine;
using UnityEngine.Serialization;

public abstract class SecondaryWeapon : ScriptableObject
{
    public string displayName;
    public Sprite itemFrameImage;
    public float cooldown = 0.5f;
    public float attackAnimationDuration = 0.3f;
    public string attackAnimationTrigger;
    public int attackDamage;
    public int maxAmmo = 10;

    public bool canDamageEnemies = true;
    public bool canMineOre = false;
    public bool canBreakBreakables = false;

    public abstract void Execute(Transform player, Vector2 direction, bool isCrouching);
}
