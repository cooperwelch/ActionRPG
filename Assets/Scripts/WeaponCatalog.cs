using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "WeaponCatalog", menuName = "ScriptableObjects/Weapon/Weapon Catalog")]
public class WeaponCatalog : ScriptableObject
{
    public List<MeleeWeapon> meleeWeapons = new List<MeleeWeapon>();
    public List<SecondaryWeapon> secondaryWeapons = new List<SecondaryWeapon>();

    private Dictionary<string, MeleeWeapon> meleeByName;
    private Dictionary<string, SecondaryWeapon> secondaryByName;

    private void OnEnable()
    {
        meleeByName = null;
        secondaryByName = null;
    }

    public bool TryGetMelee(string weaponName, out MeleeWeapon weapon)
    {
        weapon = null;
        if (string.IsNullOrEmpty(weaponName))
        {
            return false;
        }

        EnsureMaps();
        return meleeByName.TryGetValue(weaponName, out weapon) && weapon != null;
    }

    public bool TryGetSecondary(string weaponName, out SecondaryWeapon weapon)
    {
        weapon = null;
        if (string.IsNullOrEmpty(weaponName))
        {
            return false;
        }

        EnsureMaps();
        return secondaryByName.TryGetValue(weaponName, out weapon) && weapon != null;
    }

    private void EnsureMaps()
    {
        if (meleeByName != null && secondaryByName != null)
        {
            return;
        }

        meleeByName = new Dictionary<string, MeleeWeapon>();
        if (meleeWeapons != null)
        {
            foreach (MeleeWeapon weapon in meleeWeapons)
            {
                if (weapon == null)
                {
                    continue;
                }

                meleeByName[weapon.name] = weapon;
            }
        }

        secondaryByName = new Dictionary<string, SecondaryWeapon>();
        if (secondaryWeapons != null)
        {
            foreach (SecondaryWeapon weapon in secondaryWeapons)
            {
                if (weapon == null)
                {
                    continue;
                }

                secondaryByName[weapon.name] = weapon;
            }
        }
    }
}
