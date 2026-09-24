using UnityEngine;

public class SecondaryWeaponController : MonoBehaviour
{
    [SerializeField] private WeaponCatalog weaponCatalog;

    public SecondaryWeapon currentWeapon { get; private set; }
    public bool HasWeapon
    {
        get
        {
            return currentWeapon != null;
        }
    }
    public bool CanAttack => _cooldownTimer <= 0f && HasAmmo;
    public bool HasAmmo => HasWeapon && PlayerStats.GetSecondaryWeaponAmmo(currentWeapon.name) > 0;
    public int CurrentAmmo => HasWeapon ? PlayerStats.GetSecondaryWeaponAmmo(currentWeapon.name) : 0;

    private float _cooldownTimer;
    private int _currentWeaponIndex = -1;

    private void Awake()
    {
        PlayerStats.Initialize();
        if (weaponCatalog == null)
        {
            Debug.LogWarning("SecondaryWeaponController: Weapon Catalog is not assigned.");
        }

        InitializeAmmoFromCatalog();
        LoadLastEquippedSecondaryWeapon();
    }

    private void Update()
    {
        if (_cooldownTimer > 0f)
            _cooldownTimer -= Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.T)
            && weaponCatalog != null
            && weaponCatalog.secondaryWeapons != null
            && weaponCatalog.secondaryWeapons.Count > 0
            && !GameplayUI.IsBlocking)
        {
            EquipNextAvailableWeapon();
        }
    }

    public void Attack(Vector2 direction, bool isCrouching)
    {
        if (!HasWeapon || _cooldownTimer > 0f || !HasAmmo) return;
        currentWeapon.Execute(transform, direction, isCrouching);
        PlayerStats.ConsumeSecondaryWeaponAmmo(currentWeapon.name);
        _cooldownTimer = currentWeapon.cooldown;
    }

    public void AcquireSecondaryWeapon(SecondaryWeapon weapon)
    {
        PlayerStats.AcquireSecondaryWeapon(weapon.name);
        PlayerStats.InitializeSecondaryWeaponAmmo(weapon.name, weapon.maxAmmo);
        EquipWeapon(weapon);
    }

    public void EquipWeapon(SecondaryWeapon weapon)
    {
        if (weapon == null) return;
        currentWeapon = weapon;
        PlayerStats.EquipSecondaryWeapon(weapon.name);
        _currentWeaponIndex = PlayerStats.SecondaryWeapons.IndexOf(weapon.name);
        SubzoneHUD hud = FindObjectOfType<SubzoneHUD>();
        if (hud != null)
        {
            hud.SetSecondaryItemFrameImage(weapon.itemFrameImage);
        }
    }

    public bool TryGetWeapon(string weaponName, out SecondaryWeapon weapon)
    {
        weapon = null;
        if (weaponCatalog == null)
        {
            return false;
        }

        return weaponCatalog.TryGetSecondary(weaponName, out weapon);
    }

    private void InitializeAmmoFromCatalog()
    {
        if (weaponCatalog == null || weaponCatalog.secondaryWeapons == null)
        {
            return;
        }

        foreach (SecondaryWeapon weapon in weaponCatalog.secondaryWeapons)
        {
            if (weapon == null)
            {
                continue;
            }

            PlayerStats.InitializeSecondaryWeaponAmmo(weapon.name, weapon.maxAmmo);
        }
    }

    private void LoadLastEquippedSecondaryWeapon()
    {
        if (weaponCatalog != null
            && PlayerStats.SecondaryWeapon != null
            && weaponCatalog.TryGetSecondary(PlayerStats.SecondaryWeapon, out SecondaryWeapon weapon))
        {
            EquipWeapon(weapon);
        }
    }

    private void EquipNextAvailableWeapon()
    {
        if (PlayerStats.SecondaryWeapons.Count <= 0) return;

        _currentWeaponIndex = (_currentWeaponIndex + 1) % PlayerStats.SecondaryWeapons.Count;
        string weaponName = PlayerStats.SecondaryWeapons[_currentWeaponIndex];
        if (weaponCatalog != null && weaponCatalog.TryGetSecondary(weaponName, out SecondaryWeapon weapon))
        {
            EquipWeapon(weapon);
        }
    }
}
