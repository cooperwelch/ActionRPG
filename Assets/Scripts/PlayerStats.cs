using System.Collections.Generic;
using UnityEngine;

// Public API for the player's carried-over stats, inventory and progress.
// The data itself lives in GameState.Current.player, which is created automatically on first use.
public static class PlayerStats
{
    private static PlayerState S => GameState.Current.player;

    public static int Attack => S.attack;
    public static int Defense => S.defense;
    public static int DefenseCapacity => S.defenseCapacity;
    public static int Health => S.health;
    public static int HealthCapacity => S.healthCapacity;
    public static string MeleeWeapon => S.meleeWeapon;
    public static List<string> MeleeWeapons => S.meleeWeapons;
    public static string SecondaryWeapon => S.secondaryWeapon;
    public static List<string> SecondaryWeapons => S.secondaryWeapons;
    public static int Ore => S.ore;
    public static int Denarius => S.denarius;
    public static bool HasCraftingHammer => S.hasCraftingHammer;
    public static int CraftingHammerLevel => S.craftingHammerLevel;
    public static bool IsCyclopsDefeated => S.isCyclopsDefeated;
    public const int DefaultInventoryCapacity = PlayerState.DefaultInventoryCapacity;
    public static int InventoryCapacity => S.inventoryCapacity;
    public static List<ItemDefinition> Inventory => S.inventory;

    public static List<string> PowerupDestroy => S.powerupDestroy;

    public static List<string> OpenedTreasureChests => S.openedTreasureChests;

    public static List<string> OverworldDestroyList => S.overworldDestroyList;

    public static void AcquireCraftingHammer()
    {
        S.hasCraftingHammer = true;
        Debug.Log("AcquireCraftingHammer");
    }

    public static bool SpendOre(int amount)
    {
        if (amount <= 0 || S.ore < amount)
        {
            return false;
        }

        S.ore -= amount;
        return true;
    }

    public static void AddOre(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        S.ore += amount;
    }

    public static void AddDenarius(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        S.denarius += amount;
    }

    public static bool SpendDenarius(int amount)
    {
        if (amount <= 0 || S.denarius < amount)
        {
            return false;
        }

        S.denarius -= amount;
        return true;
    }

    public static bool HasEmptySlot()
    {
        EnsureInventory();
        for (int i = 0; i < S.inventoryCapacity; i++)
        {
            if (S.inventory[i] == null)
            {
                return true;
            }
        }

        return false;
    }

    public static bool TryAddItem(ItemDefinition item)
    {
        EnsureInventory();
        if (item == null)
        {
            return false;
        }

        for (int i = 0; i < S.inventoryCapacity; i++)
        {
            if (S.inventory[i] == null)
            {
                S.inventory[i] = item;
                return true;
            }
        }

        return false;
    }

    public static ItemDefinition GetItemAt(int index)
    {
        EnsureInventory();
        if (index < 0 || index >= S.inventoryCapacity)
        {
            return null;
        }

        return S.inventory[index];
    }

    public static bool RemoveAt(int index)
    {
        EnsureInventory();
        if (index < 0 || index >= S.inventoryCapacity || S.inventory[index] == null)
        {
            return false;
        }

        S.inventory[index] = null;
        CompactInventory();
        return true;
    }

    public static int Heal(int amount)
    {
        if (amount <= 0)
        {
            return 0;
        }

        int before = S.health;
        S.health = Mathf.Min(S.healthCapacity, S.health + amount);
        return S.health - before;
    }

    private static void EnsureInventory()
    {
        if (S.inventory == null || S.inventory.Count != S.inventoryCapacity)
        {
            S.inventory = PlayerState.CreateEmptyInventory(S.inventoryCapacity > 0 ? S.inventoryCapacity : DefaultInventoryCapacity);
            S.inventoryCapacity = S.inventory.Count;
        }
    }

    private static void CompactInventory()
    {
        List<ItemDefinition> occupied = new List<ItemDefinition>();
        for (int i = 0; i < S.inventoryCapacity; i++)
        {
            if (S.inventory[i] != null)
            {
                occupied.Add(S.inventory[i]);
            }
        }

        for (int i = 0; i < S.inventoryCapacity; i++)
        {
            S.inventory[i] = i < occupied.Count ? occupied[i] : null;
        }
    }

    public static void MarkForOverworldDestroy(string id)
    {
        if (string.IsNullOrEmpty(id) || S.overworldDestroyList.Contains(id))
        {
            return;
        }

        S.overworldDestroyList.Add(id);
    }

    public static void DefeatCyclops()
    {
        S.isCyclopsDefeated = true;
    }

    public static void UpgradetAttack(string tag)
    {
        S.attack += 1;
        S.powerupDestroy.Add(tag);
        Debug.Log("ADDED TAG: " + tag);
        Debug.Log("PowerupDestroy: " + S.powerupDestroy.Count);
    }

    public static void UpgradeDefense(string tag)
    {
        S.defenseCapacity += 1;
        S.defense = S.defenseCapacity;
        S.powerupDestroy.Add(tag);
        Debug.Log("ADDED TAG: " + tag);
        Debug.Log("PowerupDestroy: " + S.powerupDestroy.Count);
    }

    public static void UpgradeHealth(string tag)
    {
        S.healthCapacity += 1;
        S.health = S.healthCapacity;
        S.powerupDestroy.Add(tag);
        Debug.Log("ADDED TAG: " + tag);
        Debug.Log("PowerupDestroy: " + S.powerupDestroy.Count);
    }

    public static bool IsTreasureChestOpened(string uniqueId)
    {
        return !string.IsNullOrEmpty(uniqueId) && S.openedTreasureChests.Contains(uniqueId);
    }

    public static void OpenTreasureChest(string uniqueId)
    {
        if (string.IsNullOrEmpty(uniqueId) || S.openedTreasureChests.Contains(uniqueId))
        {
            return;
        }

        S.openedTreasureChests.Add(uniqueId);
    }

    public static void PickUpWeapon(string weaponSOPath, int attack)
    {
        EnsureMeleeWeapons();
        Debug.Log("PickUpWeapon: " + weaponSOPath);
        if (!string.IsNullOrEmpty(weaponSOPath) && !S.meleeWeapons.Contains(weaponSOPath))
        {
            S.meleeWeapons.Add(weaponSOPath);
        }

        EquipMeleeWeapon(weaponSOPath, attack);
    }

    public static void EquipMeleeWeapon(string weaponName, int attack)
    {
        EnsureMeleeWeapons();
        if (string.IsNullOrEmpty(weaponName))
        {
            return;
        }

        if (!S.meleeWeapons.Contains(weaponName))
        {
            S.meleeWeapons.Add(weaponName);
        }

        Debug.Log("EquipMeleeWeapon: " + weaponName);
        S.meleeWeapon = weaponName;
        S.attack = attack;
    }

    public static void ClearMeleeWeapon()
    {
        EnsureMeleeWeapons();
        // Persist unarmed primary (Fists) rather than a null equip state
        S.meleeWeapon = MeleeController.UnarmedWeaponName;
        S.attack = 1;
    }

    private static void EnsureMeleeWeapons()
    {
        if (S.meleeWeapons == null)
        {
            S.meleeWeapons = new List<string>();
        }

        if (!S.meleeWeapons.Contains(MeleeController.UnarmedWeaponName))
        {
            S.meleeWeapons.Insert(0, MeleeController.UnarmedWeaponName);
        }
    }

    public static void EnsureMeleeWeaponsList()
    {
        EnsureMeleeWeapons();
    }

    public static void AcquireSecondaryWeapon(string weaponName)
    {
        Debug.Log("AcquireSecondaryWeapon: " + weaponName);
        if (!S.secondaryWeapons.Contains(weaponName))
        {
            S.secondaryWeapons.Add(weaponName);
        }

        EquipSecondaryWeapon(weaponName);
    }

    public static void EquipSecondaryWeapon(string weaponName)
    {
        Debug.Log("EquipSecondaryWeapon: " + weaponName);
        S.secondaryWeapon = weaponName;
    }

    public static int GetSecondaryWeaponAmmo(string weaponName)
    {
        AmmoEntry entry = FindAmmo(weaponName);
        return entry != null ? entry.ammo : 0;
    }

    public static void InitializeSecondaryWeaponAmmo(string weaponName, int maxAmmo)
    {
        if (FindAmmo(weaponName) == null)
        {
            S.secondaryWeaponAmmo.Add(new AmmoEntry(weaponName, maxAmmo));
        }
    }

    public static void ConsumeSecondaryWeaponAmmo(string weaponName)
    {
        AmmoEntry entry = FindAmmo(weaponName);
        if (entry == null)
        {
            return;
        }

        entry.ammo = Mathf.Max(0, entry.ammo - 1);
    }

    public static void AddSecondaryWeaponAmmo(string weaponName, int amount, int maxAmmo)
    {
        AmmoEntry entry = FindAmmo(weaponName);
        if (entry == null)
        {
            entry = new AmmoEntry(weaponName, 0);
            S.secondaryWeaponAmmo.Add(entry);
        }

        entry.ammo = Mathf.Min(maxAmmo, entry.ammo + amount);
    }

    private static AmmoEntry FindAmmo(string weaponName)
    {
        foreach (AmmoEntry entry in S.secondaryWeaponAmmo)
        {
            if (entry.weaponName == weaponName)
            {
                return entry;
            }
        }

        return null;
    }

    public static void ApplyDamage(int amount)
    {
        S.health = Mathf.Max(0, S.health - amount);
    }

    public static void SetHealth(int amount)
    {
        S.health = Mathf.Clamp(amount, 0, S.healthCapacity);
    }

    public static void ResetHealthForContinue()
    {
        S.health = S.healthCapacity;
    }
}
