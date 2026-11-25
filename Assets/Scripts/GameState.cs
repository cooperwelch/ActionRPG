using System;
using System.Collections.Generic;
using UnityEngine;

// All data that carries between scenes for one playthrough.
// PlayerStats and OverworldSubzoneContainer are the public API over this data; game code
// should keep using those rather than touching GameState directly.
//
// The current state is created automatically on first use, so any scene can be played
// directly without an init step. Call NewGame() to wipe it (e.g. starting a new run).
[Serializable]
public class GameState
{
    public PlayerState player = new PlayerState();
    public WorldState world = new WorldState();

    private static GameState current;

    public static GameState Current
    {
        get
        {
            if (current == null) current = new GameState();
            return current;
        }
    }

    public static void NewGame()
    {
        current = new GameState();
    }

    // Also clears the state at the start of each play session in the editor if
    // "Enter Play Mode Options" ever disables domain reload (static data would otherwise survive).
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay()
    {
        current = null;
    }
}

[Serializable]
public class PlayerState
{
    public const int DefaultInventoryCapacity = 4;

    public int attack = 1;
    public int defense = 1;
    public int defenseCapacity = 1;
    public int health = 6;
    public int healthCapacity = 6;

    // Fists are always acquired and are the default primary.
    public string meleeWeapon = MeleeController.UnarmedWeaponName;
    public List<string> meleeWeapons = new List<string> { MeleeController.UnarmedWeaponName };
    public string secondaryWeapon = null;
    public List<string> secondaryWeapons = new List<string>();
    public List<AmmoEntry> secondaryWeaponAmmo = new List<AmmoEntry>();

    public int ore = 0;
    public int denarius = 25;
    public bool hasCraftingHammer = false;
    public int craftingHammerLevel = 1;
    public bool isCyclopsDefeated = false;

    public int inventoryCapacity = DefaultInventoryCapacity;
    // Item assets. Fine for carrying between scenes; saving to disk will need stable item IDs instead.
    public List<ItemDefinition> inventory = CreateEmptyInventory(DefaultInventoryCapacity);

    public List<string> powerupDestroy = new List<string>();
    public List<string> openedTreasureChests = new List<string>();
    public List<string> overworldDestroyList = new List<string>();

    public static List<ItemDefinition> CreateEmptyInventory(int capacity)
    {
        List<ItemDefinition> slots = new List<ItemDefinition>(capacity);
        for (int i = 0; i < capacity; i++)
        {
            slots.Add(null);
        }

        return slots;
    }
}

// Dictionaries aren't serializable by Unity, so ammo is stored as a list of pairs.
[Serializable]
public class AmmoEntry
{
    public string weaponName;
    public int ammo;

    public AmmoEntry(string weaponName, int ammo)
    {
        this.weaponName = weaponName;
        this.ammo = ammo;
    }
}

[Serializable]
public class WorldState
{
    // Where the player re-enters the overworld after a subzone. Default: near the wrecked ship.
    public Vector2 lastEncounterPosition = new Vector2(6.3f, -2.8f);
    // Other handy start points:
    //   (21.54, 16.3) near Intro Forest South
    //   (16.5, 26)    near Intro Forest North
    //   (16.5, 32.5)  near Boat
    public OverworldSubzoneContainer.PlayerDirection lastEncounterDirection = OverworldSubzoneContainer.PlayerDirection.Down;
    public string lastEncounterSubzoneName = "";

    public Vector2 subzoneLevelStartPosition = Vector2.zero;
    public OverworldSubzoneContainer.PlayerDirection subzoneLevelStartDirection = OverworldSubzoneContainer.PlayerDirection.Up;
    public bool useSubzoneLevelStartPosition = false;

    public bool hasShownWreckedShipIntro = false;
}
