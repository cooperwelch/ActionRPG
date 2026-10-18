using System.Collections;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MeleeController : MonoBehaviour
{
    public const string UnarmedWeaponName = "Fists";

    public enum PlayerDirection { Left, Right };
    public MeleeWeapon currentMeleeWeapon { get; private set; }
    [SerializeField] private SubzoneAudioManager audioManager;
    [SerializeField] private WeaponCatalog weaponCatalog;
    [SerializeField] private MeleeWeapon forceEquipOnStart;
    [SerializeField] private bool stripMeleeOnStart;
    [HideInInspector] public PlayerDirection playerDirection;
    [HideInInspector] public bool isCrouching = false;
    public bool HasWeapon
    {
        get
        {
            return currentMeleeWeapon != null;
        }
    }
    private Vector2 attackOriginPoint;
    private Vector2 attackSize;

    private MeleeWeapon UnarmedWeapon
    {
        get
        {
            if (weaponCatalog != null
                && weaponCatalog.TryGetMelee(UnarmedWeaponName, out MeleeWeapon fists)
                && fists != null)
            {
                return fists;
            }
            return null;
        }
    }

    private void Awake()
    {
        // TODO: Elliott remove this
        PlayerStats.Initialize();

        if (weaponCatalog == null)
        {
            Debug.LogWarning("MeleeController: Weapon Catalog is not assigned.");
        }

        LoadLastObtainedWeapon();

        if (!HasWeapon) return;

        attackSize = currentMeleeWeapon.attackBounds;
    }

    private void Start()
    {
        if (stripMeleeOnStart)
        {
            // Unequip real weapons → persist Fists as unarmed primary
            EquipUnarmed();
            return;
        }

        if (forceEquipOnStart != null)
        {
            PickUpMeleeWeapon(forceEquipOnStart);
        }
    }

    private void LoadLastObtainedWeapon()
    {
        if (PlayerStats.MeleeWeapon != null
            && weaponCatalog != null
            && weaponCatalog.TryGetMelee(PlayerStats.MeleeWeapon, out MeleeWeapon weapon)
            && weapon != null)
        {
            SetMeleeWeapon(weapon);
            return;
        }

        // No valid equipped primary (or first run) — fall back to Fists and persist
        EquipUnarmed();
    }

    private void Update()
    {
        if (!HasWeapon) return;

        attackOriginPoint = new Vector2(
            transform.position.x,
            transform.position.y
        );

        Vector2 weaponAttackPoint = isCrouching ? currentMeleeWeapon.crouchAttackPoint : currentMeleeWeapon.attackPoint;
        if ( playerDirection == PlayerDirection.Left )
        {
            attackOriginPoint += new Vector2(-weaponAttackPoint.x, weaponAttackPoint.y);
        } else
        {
            attackOriginPoint += weaponAttackPoint;
        }
    }

    public void Attack()
    {
        if (!HasWeapon) return;

        Collider2D[] hitEnemies = Physics2D.OverlapBoxAll(
            attackOriginPoint,
            attackSize,
            transform.eulerAngles.z,
            currentMeleeWeapon.GetAttackLayerMask()
        );

        // If we don't hit anything, play swipe sound
        if (hitEnemies.Length <= 0)
        {
            audioManager.PlayAttack();
        }

        DebugDrawBox(attackOriginPoint, attackSize);

        foreach (Collider2D c in hitEnemies)
        {
            float damageDirection = Utilities.DamageDirection(gameObject, c.gameObject);

            if (currentMeleeWeapon.canDamageEnemies)
            {
                IDamageable enemy = c.GetComponentInParent<IDamageable>();
                if (enemy != null)
                {
                    enemy.Damage(currentMeleeWeapon.attackDamage, damageDirection);
                }
            }

            if (currentMeleeWeapon.canMineOre)
            {
                IMineable mineable = c.GetComponentInParent<IMineable>();
                if (mineable != null)
                {
                    mineable.Mine(damageDirection);
                }
            }

            IBreakable breakable = c.GetComponentInParent<IBreakable>();
            if (breakable != null && CanBreak(currentMeleeWeapon, breakable))
            {
                breakable.Hit(damageDirection);
            }
        }
    }

    private static bool CanBreak(MeleeWeapon weapon, IBreakable breakable)
    {
        return breakable.Kind == BreakableKind.Rock
            ? weapon.canBreakRocks
            : weapon.canBreakBreakables;
    }

    public void PickUpMeleeWeapon(MeleeWeapon weapon)
    {
        if (weapon == null) return;
        PlayerStats.PickUpWeapon(weapon.name, weapon.attackDamage);
        SetMeleeWeapon(weapon);
    }

    /// <summary>
    /// Equips an already-acquired primary weapon (or acquires it if missing). Used by the equipment menu.
    /// </summary>
    public void EquipMeleeWeapon(MeleeWeapon weapon)
    {
        if (weapon == null) return;
        PlayerStats.EquipMeleeWeapon(weapon.name, weapon.attackDamage);
        SetMeleeWeapon(weapon);
    }

    public bool TryGetWeapon(string weaponName, out MeleeWeapon weapon)
    {
        weapon = null;
        if (weaponCatalog == null)
        {
            return false;
        }

        return weaponCatalog.TryGetMelee(weaponName, out weapon);
    }

    /// <summary>
    /// Unequips the current primary and equips Fists, persisting that choice across scenes.
    /// </summary>
    public void ClearMeleeWeapon()
    {
        EquipUnarmed();
    }

    private void EquipUnarmed()
    {
        MeleeWeapon fists = UnarmedWeapon;
        if (fists == null)
        {
            Debug.LogWarning("MeleeController: Fists weapon is not registered on the weapon catalog.");
            PlayerStats.ClearMeleeWeapon();
            currentMeleeWeapon = null;
            attackSize = Vector2.zero;
            SubzoneHUD hud = FindObjectOfType<SubzoneHUD>();
            if (hud != null)
            {
                hud.SetItemFrameImage(null);
            }
            return;
        }

        PickUpMeleeWeapon(fists);
    }

    private void SetMeleeWeapon(MeleeWeapon weapon)
    {
        SubzoneHUD hud = FindObjectOfType<SubzoneHUD>();
        if (hud != null)
        {
            hud.SetItemFrameImage(weapon.itemFrameImage);
        }
        currentMeleeWeapon = weapon;
        attackSize = weapon.attackBounds;
    }

    private void DebugDrawBox(Vector2 point, Vector2 size)
    {
        Vector2 bottomLeft = new Vector2(point.x - size.x / 2, point.y - size.y / 2);
        Vector2 bottomRight = new Vector2(point.x + size.x / 2, point.y - size.y / 2);
        Vector2 topRight = point + size / 2;
        Vector2 topLeft = new Vector2(point.x - size.x / 2, point.y + size.y / 2);

        Debug.DrawLine(bottomLeft, bottomRight, Color.red, 1f);
        Debug.DrawLine(bottomLeft, topLeft, Color.red, 1f);
        Debug.DrawLine(topLeft, topRight, Color.red, 1f);
        Debug.DrawLine(topRight, bottomRight, Color.red, 1f);
    }

    private void OnDrawGizmosSelected()
    {
        if (!HasWeapon) return;

        Gizmos.DrawWireCube(
            currentMeleeWeapon.attackPoint,
            currentMeleeWeapon.attackBounds
        );
    }
}
