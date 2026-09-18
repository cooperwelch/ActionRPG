using UnityEngine;

[CreateAssetMenu(fileName = "MeleeWeapon", menuName = "ScriptableObjects/Weapon/Melee")]
public class MeleeWeapon : ScriptableObject
{
    public string displayName;
    public Vector2 attackPoint;
    public Vector2 crouchAttackPoint;
    public Vector2 attackBounds;
    public int attackDamage;
    public Sprite itemFrameImage;
    public float attackAnimationDuration = 0.36f;
    public float crouchAttackAnimationDuration = 0.36f;
    public string attackAnimationTrigger = "IsAttacking";

    public bool canDamageEnemies = true;
    public bool canMineOre = false;
    public bool canBreakBreakables = false;

    public LayerMask GetAttackLayerMask()
    {
        int mask = 0;
        if (canDamageEnemies)
        {
            mask |= 1 << LayerMask.NameToLayer("Enemy");
        }
        if (canMineOre)
        {
            mask |= 1 << LayerMask.NameToLayer("Ore");
        }
        if (canBreakBreakables)
        {
            mask |= 1 << LayerMask.NameToLayer("Breakables");
        }
        return mask;
    }
}
