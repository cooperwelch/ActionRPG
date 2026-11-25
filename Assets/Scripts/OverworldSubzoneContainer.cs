using UnityEngine;

// Public API for overworld <-> subzone transition data.
// The data itself lives in GameState.Current.world, which is created automatically on first use.
public class OverworldSubzoneContainer
{
    public enum PlayerDirection { Up, Down, Left, Right }

    private static WorldState W => GameState.Current.world;

    public static Vector2 LastEncounterPosition => W.lastEncounterPosition;
    public static PlayerDirection LastEncounterDirection => W.lastEncounterDirection;
    public static string LastEncounterSubzoneName => W.lastEncounterSubzoneName;

    public static Vector2 SubzoneLevelStartPosition => W.subzoneLevelStartPosition;
    public static PlayerDirection SubzoneLevelStartDirection => W.subzoneLevelStartDirection;
    public static bool UseSubzoneLevelStartPosition
    {
        get => W.useSubzoneLevelStartPosition;
        set => W.useSubzoneLevelStartPosition = value;
    }

    public static bool HasShownWreckedShipIntro
    {
        get => W.hasShownWreckedShipIntro;
        set => W.hasShownWreckedShipIntro = value;
    }

    public static void AddEncounter(Vector2 position, string subzoneName, PlayerDirection direction)
    {
        W.lastEncounterPosition = position;
        W.lastEncounterSubzoneName = subzoneName;
        W.lastEncounterDirection = direction;
    }

    public static void AddSubzoneStartPosition(Vector2 position, PlayerDirection direction)
    {
        W.subzoneLevelStartPosition = position;
        W.subzoneLevelStartDirection = direction;
        W.useSubzoneLevelStartPosition = true;
    }
}
