using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EquipmentUIController : MonoBehaviour
{
    public const int SlotsPerRow = 5;
    public const int RowCount = 2;
    public const int PrimaryRow = 0;
    public const int SecondaryRow = 1;

    [SerializeField] private RectTransform panelRect;
    [SerializeField] private Transform primarySlotContainer;
    [SerializeField] private Transform secondarySlotContainer;
    [SerializeField] private EquipmentSlotView slotPrefab;
    [SerializeField] private Sprite primaryEquippedFrame;
    [SerializeField] private Sprite secondaryEquippedFrame;
    [SerializeField] private GameObject descriptionPanel;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text attackText;
    [SerializeField] private float descriptionSpacing = -7f;
    [SerializeField] private Image menuCursor;
    [SerializeField] private float cursorOffsetFromOptionLeft = 4f;

    private readonly List<EquipmentSlotView> primarySlots = new List<EquipmentSlotView>();
    private readonly List<EquipmentSlotView> secondarySlots = new List<EquipmentSlotView>();
    private RectTransform descriptionPanelRect;
    private Vector2 cursorSize = new Vector2(8f, 8f);
    private MeleeController meleeController;
    private SecondaryWeaponController secondaryWeaponController;

    private void Awake()
    {
        if (panelRect == null)
        {
            panelRect = transform as RectTransform;
        }

        if (descriptionPanel != null)
        {
            descriptionPanelRect = descriptionPanel.transform as RectTransform;
        }

        if (menuCursor != null && menuCursor.rectTransform.sizeDelta.x > 0f)
        {
            cursorSize = menuCursor.rectTransform.sizeDelta;
        }
    }

    public void EnsureSlots()
    {
        EnsureRow(primarySlots, primarySlotContainer, primaryEquippedFrame);
        EnsureRow(secondarySlots, secondarySlotContainer, secondaryEquippedFrame);
    }

    public void Refresh(int focusedRow, int focusedColumn, bool hasFocus)
    {
        PlayerStats.Initialize();
        PlayerStats.EnsureMeleeWeaponsList();
        CacheControllers();
        EnsureSlots();

        BindRow(primarySlots, PlayerStats.MeleeWeapons, PlayerStats.MeleeWeapon, PrimaryRow, focusedRow, focusedColumn, hasFocus, true);
        BindRow(secondarySlots, PlayerStats.SecondaryWeapons, PlayerStats.SecondaryWeapon, SecondaryRow, focusedRow, focusedColumn, hasFocus, false);

        string displayName = string.Empty;
        int attackDamage = 0;
        bool showDescription = hasFocus
            && TryGetFocusedWeapon(focusedRow, focusedColumn, out displayName, out attackDamage);
        if (descriptionPanel != null)
        {
            descriptionPanel.SetActive(showDescription);
            if (showDescription)
            {
                PositionDescriptionPanel();
            }
        }

        if (nameText != null)
        {
            nameText.text = showDescription ? displayName : string.Empty;
        }

        if (attackText != null)
        {
            attackText.text = showDescription ? attackDamage.ToString() : string.Empty;
        }

        PositionCursor(focusedRow, focusedColumn, hasFocus);
    }

    public bool TryEquipFocused(int focusedRow, int focusedColumn)
    {
        PlayerStats.Initialize();
        CacheControllers();

        if (focusedColumn < 0 || focusedColumn >= SlotsPerRow)
        {
            return false;
        }

        if (focusedRow == PrimaryRow)
        {
            string weaponName = GetAcquiredName(PlayerStats.MeleeWeapons, focusedColumn);
            if (string.IsNullOrEmpty(weaponName) || weaponName == PlayerStats.MeleeWeapon)
            {
                return false;
            }

            if (meleeController == null || !meleeController.TryGetWeapon(weaponName, out MeleeWeapon weapon))
            {
                return false;
            }

            meleeController.EquipMeleeWeapon(weapon);
            return true;
        }

        if (focusedRow == SecondaryRow)
        {
            string weaponName = GetAcquiredName(PlayerStats.SecondaryWeapons, focusedColumn);
            if (string.IsNullOrEmpty(weaponName) || weaponName == PlayerStats.SecondaryWeapon)
            {
                return false;
            }

            if (secondaryWeaponController == null
                || !secondaryWeaponController.TryGetWeapon(weaponName, out SecondaryWeapon weapon))
            {
                return false;
            }

            secondaryWeaponController.EquipWeapon(weapon);
            return true;
        }

        return false;
    }

    public void HideDescription()
    {
        if (descriptionPanel != null)
        {
            descriptionPanel.SetActive(false);
        }

        if (menuCursor != null)
        {
            menuCursor.enabled = false;
        }
    }

    private void EnsureRow(List<EquipmentSlotView> slots, Transform container, Sprite equippedFrame)
    {
        if (container == null || slotPrefab == null)
        {
            return;
        }

        while (slots.Count < SlotsPerRow)
        {
            EquipmentSlotView slot = Instantiate(slotPrefab, container);
            slot.SetEquippedFrame(equippedFrame);
            slots.Add(slot);
        }
    }

    private void BindRow(
        List<EquipmentSlotView> slots,
        List<string> acquired,
        string equippedName,
        int row,
        int focusedRow,
        int focusedColumn,
        bool hasFocus,
        bool isPrimary)
    {
        for (int i = 0; i < slots.Count && i < SlotsPerRow; i++)
        {
            if (slots[i] == null)
            {
                continue;
            }

            string weaponName = GetAcquiredName(acquired, i);
            bool focused = hasFocus && focusedRow == row && focusedColumn == i;
            bool equipped = !string.IsNullOrEmpty(weaponName) && weaponName == equippedName;
            Sprite icon = null;
            if (!string.IsNullOrEmpty(weaponName))
            {
                icon = GetWeaponIcon(weaponName, isPrimary);
            }

            slots[i].Bind(icon, equipped, focused);
        }
    }

    private Sprite GetWeaponIcon(string weaponName, bool isPrimary)
    {
        if (isPrimary)
        {
            if (meleeController != null && meleeController.TryGetWeapon(weaponName, out MeleeWeapon melee))
            {
                return melee.itemFrameImage;
            }
        }
        else if (secondaryWeaponController != null
            && secondaryWeaponController.TryGetWeapon(weaponName, out SecondaryWeapon secondary))
        {
            return secondary.itemFrameImage;
        }

        return null;
    }

    private bool TryGetFocusedWeapon(int row, int column, out string displayName, out int attackDamage)
    {
        displayName = string.Empty;
        attackDamage = 0;

        if (row == PrimaryRow)
        {
            string weaponName = GetAcquiredName(PlayerStats.MeleeWeapons, column);
            if (string.IsNullOrEmpty(weaponName)
                || meleeController == null
                || !meleeController.TryGetWeapon(weaponName, out MeleeWeapon weapon))
            {
                return false;
            }

            displayName = string.IsNullOrEmpty(weapon.displayName) ? weapon.name : weapon.displayName;
            attackDamage = weapon.attackDamage;
            return true;
        }

        if (row == SecondaryRow)
        {
            string weaponName = GetAcquiredName(PlayerStats.SecondaryWeapons, column);
            if (string.IsNullOrEmpty(weaponName)
                || secondaryWeaponController == null
                || !secondaryWeaponController.TryGetWeapon(weaponName, out SecondaryWeapon weapon))
            {
                return false;
            }

            displayName = string.IsNullOrEmpty(weapon.displayName) ? weapon.name : weapon.displayName;
            attackDamage = weapon.attackDamage;
            return true;
        }

        return false;
    }

    private static string GetAcquiredName(List<string> acquired, int index)
    {
        if (acquired == null || index < 0 || index >= acquired.Count)
        {
            return null;
        }

        return acquired[index];
    }

    private void PositionCursor(int focusedRow, int focusedColumn, bool showCursor)
    {
        if (menuCursor == null)
        {
            return;
        }

        EquipmentSlotView slot = GetSlot(focusedRow, focusedColumn);
        bool visible = showCursor && slot != null;
        menuCursor.enabled = visible;
        if (!visible)
        {
            return;
        }

        RebuildEquipmentLayout();
        UIMenuCursor.PositionLeftOf(menuCursor, slot.RectTransform, cursorOffsetFromOptionLeft, cursorSize);
    }

    private void RebuildEquipmentLayout()
    {
        Canvas.ForceUpdateCanvases();

        if (panelRect != null && panelRect.gameObject.activeInHierarchy)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(panelRect);
        }

        if (primarySlotContainer is RectTransform primaryRect)
        {
            RectTransform contentRoot = primaryRect.parent as RectTransform;
            if (contentRoot != null && contentRoot.gameObject.activeInHierarchy)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(contentRoot);
            }

            if (primaryRect.gameObject.activeInHierarchy)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(primaryRect);
            }
        }

        if (secondarySlotContainer is RectTransform secondaryRect && secondaryRect.gameObject.activeInHierarchy)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(secondaryRect);
        }
    }

    private EquipmentSlotView GetSlot(int row, int column)
    {
        List<EquipmentSlotView> slots = row == PrimaryRow ? primarySlots : secondarySlots;
        if (slots == null || column < 0 || column >= slots.Count)
        {
            return null;
        }

        return slots[column];
    }

    private void PositionDescriptionPanel()
    {
        if (panelRect == null || descriptionPanelRect == null)
        {
            return;
        }

        descriptionPanelRect.anchorMin = panelRect.anchorMin;
        descriptionPanelRect.anchorMax = panelRect.anchorMax;
        descriptionPanelRect.pivot = panelRect.pivot;

        Vector2 size = descriptionPanelRect.sizeDelta;
        size.x = panelRect.sizeDelta.x;
        descriptionPanelRect.sizeDelta = size;

        float underneathY = panelRect.anchoredPosition.y - panelRect.rect.height - descriptionSpacing;
        descriptionPanelRect.anchoredPosition = new Vector2(panelRect.anchoredPosition.x, underneathY);
    }

    private void CacheControllers()
    {
        if (meleeController == null)
        {
            meleeController = FindObjectOfType<MeleeController>();
        }

        if (secondaryWeaponController == null)
        {
            secondaryWeaponController = FindObjectOfType<SecondaryWeaponController>();
        }
    }
}
