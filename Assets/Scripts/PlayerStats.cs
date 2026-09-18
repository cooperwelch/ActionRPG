using System.Collections.Generic;
using UnityEngine;
public static class PlayerStats
{
    public static int Attack { get; private set; }
    public static int Defense { get; private set; }
    public static int DefenseCapacity { get; private set; }
    public static int Health { get; private set; }
    public static int HealthCapacity { get; private set; }
    public static string MeleeWeapon { get; private set; }
    public static List<string> MeleeWeapons { get; private set; }
    public static string SecondaryWeapon { get; private set; }
    public static List<string> SecondaryWeapons { get; private set; }
    public static int Ore { get; private set; }
    public static int Denarius { get; private set; }
    public static bool HasCraftingHammer { get; private set; }
    public static int CraftingHammerLevel { get; private set; }
    public static bool IsCyclopsDefeated { get; private set; }
    public const int DefaultInventoryCapacity = 4;
    public static int InventoryCapacity { get; private set; }
    public static List<ItemDefinition> Inventory { get; private set; }

    private static Dictionary<string, int> SecondaryWeaponAmmo;

    public static List<string> PowerupDestroy { get; private set; }

    public static List<string> OpenedTreasureChests { get; private set; }

    public static List<string> OverworldDestroyList { get; private set; }

    private static bool Initialized;

    public static void Initialize()
    {
        if (!Initialized)
        {
            Attack = 0;
            DefenseCapacity = 1;
            Defense = DefenseCapacity;
            HealthCapacity = 14;
            Health = HealthCapacity;
            Initialized = true;
            PowerupDestroy = new List<string>();
            OpenedTreasureChests = new List<string>();
            OverworldDestroyList = new List<string>();
            // Default unarmed primary — persists across scenes; Fists always acquired
            MeleeWeapons = new List<string> { MeleeController.UnarmedWeaponName };
            MeleeWeapon = MeleeController.UnarmedWeaponName;
            Attack = 1;
            SecondaryWeapons = new List<string>();
            //SecondaryWeapons.Add("ThrowingAxe");
            //SecondaryWeapons.Add("Plumbata");
            //SecondaryWeapon = "ThrowingAxe";
            SecondaryWeapon = null;
            SecondaryWeaponAmmo = new Dictionary<string, int>();
            //Ore = 99;
            Denarius = 25;
            HasCraftingHammer = false;
            CraftingHammerLevel = 1;
            IsCyclopsDefeated = false;
            InventoryCapacity = DefaultInventoryCapacity;
            Inventory = CreateEmptyInventory(InventoryCapacity);
        }
    }

    public static void AcquireCraftingHammer()
    {
        Initialize();
        HasCraftingHammer = true;
        Debug.Log("AcquireCraftingHammer");
    }

    public static bool SpendOre(int amount)
    {
        Initialize();
        if (amount <= 0 || Ore < amount)
        {
            return false;
        }

        Ore -= amount;
        return true;
    }

    public static void AddOre(int amount)
    {
        Initialize();
        if (amount <= 0)
        {
            return;
        }

        Ore += amount;
    }

    public static void AddDenarius(int amount)
    {
        Initialize();
        if (amount <= 0)
        {
            return;
        }

        Denarius += amount;
    }

    public static bool SpendDenarius(int amount)
    {
        Initialize();
        if (amount <= 0 || Denarius < amount)
        {
            return false;
        }

        Denarius -= amount;
        return true;
    }

    public static bool HasEmptySlot()
    {
        Initialize();
        EnsureInventory();
        for (int i = 0; i < InventoryCapacity; i++)
        {
            if (Inventory[i] == null)
            {
                return true;
            }
        }

        return false;
    }

    public static bool TryAddItem(ItemDefinition item)
    {
        Initialize();
        EnsureInventory();
        if (item == null)
        {
            return false;
        }

        for (int i = 0; i < InventoryCapacity; i++)
        {
            if (Inventory[i] == null)
            {
                Inventory[i] = item;
                return true;
            }
        }

        return false;
    }

    public static ItemDefinition GetItemAt(int index)
    {
        Initialize();
        EnsureInventory();
        if (index < 0 || index >= InventoryCapacity)
        {
            return null;
        }

        return Inventory[index];
    }

    public static bool RemoveAt(int index)
    {
        Initialize();
        EnsureInventory();
        if (index < 0 || index >= InventoryCapacity || Inventory[index] == null)
        {
            return false;
        }

        Inventory[index] = null;
        CompactInventory();
        return true;
    }

    public static int Heal(int amount)
    {
        Initialize();
        if (amount <= 0)
        {
            return 0;
        }

        int before = Health;
        Health = Mathf.Min(HealthCapacity, Health + amount);
        return Health - before;
    }

    private static List<ItemDefinition> CreateEmptyInventory(int capacity)
    {
        List<ItemDefinition> slots = new List<ItemDefinition>(capacity);
        for (int i = 0; i < capacity; i++)
        {
            slots.Add(null);
        }

        return slots;
    }

    private static void EnsureInventory()
    {
        if (Inventory == null || Inventory.Count != InventoryCapacity)
        {
            Inventory = CreateEmptyInventory(InventoryCapacity > 0 ? InventoryCapacity : DefaultInventoryCapacity);
            InventoryCapacity = Inventory.Count;
        }
    }

    private static void CompactInventory()
    {
        List<ItemDefinition> occupied = new List<ItemDefinition>();
        for (int i = 0; i < InventoryCapacity; i++)
        {
            if (Inventory[i] != null)
            {
                occupied.Add(Inventory[i]);
            }
        }

        for (int i = 0; i < InventoryCapacity; i++)
        {
            Inventory[i] = i < occupied.Count ? occupied[i] : null;
        }
    }

    public static void MarkForOverworldDestroy(string id)
    {
        Initialize();
        if (string.IsNullOrEmpty(id) || OverworldDestroyList.Contains(id))
        {
            return;
        }

        OverworldDestroyList.Add(id);
    }

    public static void DefeatCyclops()
    {
        Initialize();
        IsCyclopsDefeated = true;
    }

    public static void UpgradetAttack(string tag)
    {
        Attack += 1;
        PowerupDestroy.Add(tag);
        Debug.Log("ADDED TAG: " + tag);
        Debug.Log("PowerupDestroy: " + PowerupDestroy.Count);
    }

    public static void UpgradeDefense(string tag)
    {
        DefenseCapacity += 1;
        Defense = DefenseCapacity;
        PowerupDestroy.Add(tag);
        Debug.Log("ADDED TAG: " + tag);
        Debug.Log("PowerupDestroy: " + PowerupDestroy.Count);
    }

    public static void UpgradeHealth(string tag)
    {
        HealthCapacity += 1;
        Health = HealthCapacity;
        PowerupDestroy.Add(tag);
        Debug.Log("ADDED TAG: " + tag);
        Debug.Log("PowerupDestroy: " + PowerupDestroy.Count);
    }

    public static bool IsTreasureChestOpened(string uniqueId)
    {
        Initialize();
        return !string.IsNullOrEmpty(uniqueId) && OpenedTreasureChests.Contains(uniqueId);
    }

    public static void OpenTreasureChest(string uniqueId)
    {
        Initialize();
        if (string.IsNullOrEmpty(uniqueId) || OpenedTreasureChests.Contains(uniqueId))
        {
            return;
        }

        OpenedTreasureChests.Add(uniqueId);
    }

    public static void PickUpWeapon(string weaponSOPath, int attack)
    {
        Initialize();
        EnsureMeleeWeapons();
        Debug.Log("PickUpWeapon: " + weaponSOPath);
        if (!string.IsNullOrEmpty(weaponSOPath) && !MeleeWeapons.Contains(weaponSOPath))
        {
            MeleeWeapons.Add(weaponSOPath);
        }

        EquipMeleeWeapon(weaponSOPath, attack);
    }

    public static void EquipMeleeWeapon(string weaponName, int attack)
    {
        Initialize();
        EnsureMeleeWeapons();
        if (string.IsNullOrEmpty(weaponName))
        {
            return;
        }

        if (!MeleeWeapons.Contains(weaponName))
        {
            MeleeWeapons.Add(weaponName);
        }

        Debug.Log("EquipMeleeWeapon: " + weaponName);
        MeleeWeapon = weaponName;
        Attack = attack;
    }

    public static void ClearMeleeWeapon()
    {
        Initialize();
        EnsureMeleeWeapons();
        // Persist unarmed primary (Fists) rather than a null equip state
        if (!MeleeWeapons.Contains(MeleeController.UnarmedWeaponName))
        {
            MeleeWeapons.Insert(0, MeleeController.UnarmedWeaponName);
        }

        MeleeWeapon = MeleeController.UnarmedWeaponName;
        Attack = 1;
    }

    private static void EnsureMeleeWeapons()
    {
        if (MeleeWeapons == null)
        {
            MeleeWeapons = new List<string>();
        }

        if (!MeleeWeapons.Contains(MeleeController.UnarmedWeaponName))
        {
            MeleeWeapons.Insert(0, MeleeController.UnarmedWeaponName);
        }
    }

    public static void EnsureMeleeWeaponsList()
    {
        Initialize();
        EnsureMeleeWeapons();
    }

    public static void AcquireSecondaryWeapon(string weaponName)
    {
        Debug.Log("AcquireSecondaryWeapon: " + weaponName);
        if (!SecondaryWeapons.Contains(weaponName))
        {
            SecondaryWeapons.Add(weaponName);
        }

        EquipSecondaryWeapon(weaponName);
    }

    public static void EquipSecondaryWeapon(string weaponName)
    {
        Debug.Log("EquipSecondaryWeapon: " + weaponName);
        SecondaryWeapon = weaponName;
    }

    public static int GetSecondaryWeaponAmmo(string weaponName)
    {
        return SecondaryWeaponAmmo != null && SecondaryWeaponAmmo.TryGetValue(weaponName, out int ammo) ? ammo : 0;
    }

    public static void InitializeSecondaryWeaponAmmo(string weaponName, int maxAmmo)
    {
        if (SecondaryWeaponAmmo == null)
        {
            SecondaryWeaponAmmo = new Dictionary<string, int>();
        }

        if (!SecondaryWeaponAmmo.ContainsKey(weaponName))
        {
            SecondaryWeaponAmmo[weaponName] = maxAmmo;
        }
    }

    public static void ConsumeSecondaryWeaponAmmo(string weaponName)
    {
        if (SecondaryWeaponAmmo == null || !SecondaryWeaponAmmo.ContainsKey(weaponName))
        {
            return;
        }

        SecondaryWeaponAmmo[weaponName] = Mathf.Max(0, SecondaryWeaponAmmo[weaponName] - 1);
    }

    public static void AddSecondaryWeaponAmmo(string weaponName, int amount, int maxAmmo)
    {
        Initialize();
        if (SecondaryWeaponAmmo == null)
        {
            SecondaryWeaponAmmo = new Dictionary<string, int>();
        }

        if (!SecondaryWeaponAmmo.ContainsKey(weaponName))
        {
            SecondaryWeaponAmmo[weaponName] = 0;
        }

        SecondaryWeaponAmmo[weaponName] = Mathf.Min(maxAmmo, SecondaryWeaponAmmo[weaponName] + amount);
    }

    public static void ApplyDamage(int amount)
    {
        int newHealth = Health - amount;
        if (newHealth < 0)
        {
            newHealth = 0;
        }
        Health = newHealth;
    }

    public static void Reset()
    {
        Initialized = false;
    }

    public static void ResetHealthForContinue()
    {
        Health = HealthCapacity;
    }
}
