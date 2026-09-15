using System.Collections.Generic;
using UnityEngine;

public class SecondaryWeaponController : MonoBehaviour
{
    [SerializeField] private List<SecondaryWeapon> weapons;

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

    private Dictionary<string, SecondaryWeapon> _weaponMap;
    private float _cooldownTimer;
    private int _currentWeaponIndex = -1;

    private void Awake()
    {
        PlayerStats.Initialize();
        InitializeWeaponMap();
        LoadLastEquippedSecondaryWeapon();
    }

    private void Update()
    {
        if (_cooldownTimer > 0f)
            _cooldownTimer -= Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.T) && weapons.Count > 0 && !GameplayUI.IsBlocking)
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

    private void EquipWeapon(SecondaryWeapon weapon)
    {
        currentWeapon = weapon;
        PlayerStats.EquipSecondaryWeapon(weapon.name);
        _currentWeaponIndex = PlayerStats.SecondaryWeapons.IndexOf(weapon.name);
        FindObjectOfType<SubzoneHUD>().SetSecondaryItemFrameImage(weapon.itemFrameImage);
    }

    private void InitializeWeaponMap()
    {
        _weaponMap = new Dictionary<string, SecondaryWeapon>();
        foreach (SecondaryWeapon weapon in weapons)
        {
            _weaponMap[weapon.name] = weapon;
            PlayerStats.InitializeSecondaryWeaponAmmo(weapon.name, weapon.maxAmmo);
        }
    }

    private void LoadLastEquippedSecondaryWeapon()
    {
        if (PlayerStats.SecondaryWeapon != null && _weaponMap.ContainsKey(PlayerStats.SecondaryWeapon))
        {
            EquipWeapon(_weaponMap[PlayerStats.SecondaryWeapon]);
        }
    }

    private void EquipNextAvailableWeapon()
    {
        if (PlayerStats.SecondaryWeapons.Count <= 0) return;

        _currentWeaponIndex = (_currentWeaponIndex + 1) % PlayerStats.SecondaryWeapons.Count;
        string weaponName = PlayerStats.SecondaryWeapons[_currentWeaponIndex];
        if (_weaponMap.ContainsKey(weaponName))
        {
            EquipWeapon(_weaponMap[weaponName]);
        }
    }
}
