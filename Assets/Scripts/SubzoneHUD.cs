using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class SubzoneHUD : MonoBehaviour
{
    [SerializeField] private HUDHealthMeter playerHealthMeter;
    [SerializeField] private HUDHealthMeter bossHealthMeter;
    [SerializeField] private TMP_Text attackValueText;
    [SerializeField] private TMP_Text defenseValueText;
    [SerializeField] private TMP_Text ammoValueText;
    [SerializeField] private TMP_Text oreValueText;
    [SerializeField] private TMP_Text denariusValueText;
    [SerializeField] private Image itemFrame;
    [SerializeField] private Image secondaryItemFrame;
    [SerializeField] private Color ammoNormalColor = Color.white;
    [SerializeField] private Color ammoEmptyColor = new Color(0.5f, 0.5f, 0.5f, 1f);
    [SerializeField] private GameObject hudChrome;
    [SerializeField] private bool hudVisibleWithMenuOnly;
    [SerializeField] private WeaponCatalog weaponCatalog;

    private void Awake()
    {
        ApplyHudChromeVisibility(false);
        if (hudVisibleWithMenuOnly && bossHealthMeter != null)
        {
            bossHealthMeter.gameObject.SetActive(false);
        }
    }

    private void Start()
    {
        PlayerStats.Initialize();
        if (weaponCatalog == null)
        {
            Debug.LogWarning("SubzoneHUD: Weapon Catalog is not assigned.");
        }

        RefreshEquippedFrames();
        UpdateStatTexts();

        if (attackValueText != null)
        {
            attackValueText.ForceMeshUpdate();
        }
        if (defenseValueText != null)
        {
            defenseValueText.ForceMeshUpdate();
        }
        if (ammoValueText != null)
        {
            ammoValueText.ForceMeshUpdate();
        }
        if (oreValueText != null)
        {
            oreValueText.ForceMeshUpdate();
        }
        if (denariusValueText != null)
        {
            denariusValueText.ForceMeshUpdate();
        }
        if (attackValueText != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(attackValueText.transform.parent as RectTransform);
        }
        if (defenseValueText != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(defenseValueText.transform.parent as RectTransform);
        }
        if (ammoValueText != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(ammoValueText.transform.parent as RectTransform);
        }
        if (oreValueText != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(oreValueText.transform.parent as RectTransform);
        }
        if (denariusValueText != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(denariusValueText.transform.parent as RectTransform);
        }
    }

    private void Update()
    {
        UpdateStatTexts();
    }

    private void UpdateStatTexts()
    {
        if (attackValueText != null)
        {
            attackValueText.text = PlayerStats.Attack.ToString();
        }
        if (defenseValueText != null)
        {
            defenseValueText.text = PlayerStats.Defense.ToString();
        }

        if (ammoValueText != null)
        {
            int ammo = GetSecondaryWeaponAmmo();
            bool hasSecondary = !string.IsNullOrEmpty(PlayerStats.SecondaryWeapon);
            ammoValueText.text = hasSecondary ? ammo.ToString("D2") : "00";
            ammoValueText.color = !hasSecondary || ammo <= 0 ? ammoEmptyColor : ammoNormalColor;
        }

        if (oreValueText != null)
        {
            oreValueText.text = PlayerStats.Ore.ToString("D2");
        }

        if (denariusValueText != null)
        {
            denariusValueText.text = PlayerStats.Denarius.ToString("D2");
        }
    }

    private void RefreshEquippedFrames()
    {
        Sprite primaryIcon = null;
        if (weaponCatalog != null
            && weaponCatalog.TryGetMelee(PlayerStats.MeleeWeapon, out MeleeWeapon melee)
            && melee != null)
        {
            primaryIcon = melee.itemFrameImage;
        }

        SetItemFrameImage(primaryIcon);

        Sprite secondaryIcon = null;
        if (weaponCatalog != null
            && weaponCatalog.TryGetSecondary(PlayerStats.SecondaryWeapon, out SecondaryWeapon secondary)
            && secondary != null)
        {
            secondaryIcon = secondary.itemFrameImage;
        }

        SetSecondaryItemFrameImage(secondaryIcon);
    }

    private static int GetSecondaryWeaponAmmo()
    {
        if (string.IsNullOrEmpty(PlayerStats.SecondaryWeapon))
        {
            return 0;
        }

        return PlayerStats.GetSecondaryWeaponAmmo(PlayerStats.SecondaryWeapon);
    }

    public void FillBossHealthMeter()
    {
        if (bossHealthMeter != null)
        {
            bossHealthMeter.FillMeter();
        }
    }

    public void FillPlayerHealthMeter()
    {
        if (playerHealthMeter != null)
        {
            playerHealthMeter.FillMeter();
        }
    }

    public void ReducePlayerHealthMeter(int amount)
    {
        if (playerHealthMeter != null)
        {
            playerHealthMeter.Decrement(amount);
        }
    }

    public void IncreasePlayerHealthMeter(int amount)
    {
        if (playerHealthMeter != null && amount > 0)
        {
            playerHealthMeter.Increment(amount);
        }
    }

    public void ReduceBossHealthMeter(int amount)
    {
        if (bossHealthMeter != null)
        {
            bossHealthMeter.Decrement(amount);
        }
    }

    public void SetHudVisibleForMenu(bool menuOpen)
    {
        ApplyHudChromeVisibility(menuOpen);
    }

    public void SetItemFrameImage(Sprite image)
    {
        if (itemFrame == null)
        {
            return;
        }

        itemFrame.sprite = image;
        itemFrame.color = image != null ? Color.white : Color.clear;
    }

    public void SetSecondaryItemFrameImage(Sprite image)
    {
        if (secondaryItemFrame == null)
        {
            return;
        }

        secondaryItemFrame.sprite = image;
        secondaryItemFrame.color = image != null ? Color.white : Color.clear;
    }

    private void ApplyHudChromeVisibility(bool menuOpen)
    {
        if (hudChrome == null)
        {
            return;
        }

        hudChrome.SetActive(!hudVisibleWithMenuOnly || menuOpen);
    }
}
