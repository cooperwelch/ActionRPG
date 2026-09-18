using PixelCrushers.DialogueSystem;
using UnityEngine;

public static class GameplayUI
{
    public static bool IsCraftingOpen { get; set; }
    public static bool IsMainMenuOpen { get; set; }
    public static bool IsShopOpen { get; set; }
    public static int LastClosedFrame { get; private set; } = -1;

    public static bool IsBlocking => IsCraftingOpen || IsMainMenuOpen || IsShopOpen;

    public static bool ConfirmPressed()
    {
        return Input.GetButtonDown("Fire1")
            || Input.GetKeyDown(KeyCode.Space)
            || Input.GetKeyDown(KeyCode.Return);
    }

    public static bool CancelPressed()
    {
        return Input.GetButtonDown("Cancel");
    }

    public static bool MenuPressed()
    {
        return Input.GetButtonDown("Menu");
    }

    public static void NotifyClosed()
    {
        LastClosedFrame = Time.frameCount;
    }

    public static bool CanOpenMainMenu()
    {
        if (IsBlocking || DialogueManager.IsConversationActive)
        {
            return false;
        }

        if (GameManager.sharedInstance != null
            && (GameManager.sharedInstance.IsPaused || GameManager.sharedInstance.IsGameOver))
        {
            return false;
        }

        if (Time.timeScale == 0f)
        {
            return false;
        }

        if (PlayerStats.Health <= 0)
        {
            return false;
        }

        PlayerMovement playerMovement = Object.FindObjectOfType<PlayerMovement>();
        if (playerMovement != null)
        {
            return playerMovement.canMove;
        }

        TopDownMovement overworldMovement = Object.FindObjectOfType<TopDownMovement>();
        return overworldMovement != null && overworldMovement.CanMove;
    }

    public static void FreezePlayer()
    {
        PlayerMovement playerMovement = Object.FindObjectOfType<PlayerMovement>();
        if (playerMovement != null)
        {
            playerMovement.StopForDialogue();
            return;
        }

        TopDownMovement overworldMovement = Object.FindObjectOfType<TopDownMovement>();
        if (overworldMovement != null)
        {
            overworldMovement.StopMovement();
        }
    }

    public static void UnfreezePlayer()
    {
        PlayerMovement playerMovement = Object.FindObjectOfType<PlayerMovement>();
        if (playerMovement != null)
        {
            playerMovement.AllowMovement();
            return;
        }

        TopDownMovement overworldMovement = Object.FindObjectOfType<TopDownMovement>();
        if (overworldMovement != null)
        {
            overworldMovement.AllowMovement();
        }
    }
}
