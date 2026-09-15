using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryUIController : MonoBehaviour
{
    [SerializeField] private RectTransform panelRect;
    [SerializeField] private Transform slotContainer;
    [SerializeField] private InventorySlotRow slotPrefab;
    [SerializeField] private GameObject descriptionPanel;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private float slotHeight = 20f;
    [SerializeField] private float slotSpacing = 2f;
    [SerializeField] private float paddingTop = 8f;
    [SerializeField] private float paddingBottom = 8f;
    [SerializeField] private float descriptionSpacing = 0f;
    [SerializeField] private Image menuCursor;
    [SerializeField] private float cursorOffsetFromOptionLeft = 6f;

    private readonly List<InventorySlotRow> slots = new List<InventorySlotRow>();
    private RectTransform descriptionPanelRect;
    private Vector2 cursorSize = new Vector2(8f, 8f);

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
        PlayerStats.Initialize();
        while (slots.Count < PlayerStats.InventoryCapacity)
        {
            InventorySlotRow row = Instantiate(slotPrefab, slotContainer);
            slots.Add(row);
        }

        // Sizing/rebuild while inactive corrupts stretch-child offsets (e.g. Left/Right -6/6).
        if (gameObject.activeInHierarchy)
        {
            ResizePanelToSlots();
        }
    }

    public void Refresh(int focusedIndex, bool showDescription)
    {
        EnsureSlots();
        PlayerStats.Initialize();

        for (int i = 0; i < slots.Count; i++)
        {
            ItemDefinition item = PlayerStats.GetItemAt(i);
            bool focused = showDescription && i == focusedIndex;
            slots[i].Bind(item, focused);
        }

        ItemDefinition focusedItem = showDescription && focusedIndex >= 0
            ? PlayerStats.GetItemAt(focusedIndex)
            : null;
        bool showPanel = focusedItem != null;

        if (descriptionPanel != null)
        {
            descriptionPanel.SetActive(showPanel);
            if (showPanel)
            {
                PositionDescriptionPanel();
            }
        }

        if (descriptionText != null)
        {
            descriptionText.text = showPanel ? focusedItem.description : string.Empty;
        }

        PositionCursor(focusedIndex, showDescription);
    }

    private void PositionCursor(int focusedIndex, bool showCursor)
    {
        if (menuCursor == null)
        {
            return;
        }

        bool visible = showCursor && focusedIndex >= 0 && focusedIndex < slots.Count;
        menuCursor.enabled = visible;
        if (!visible)
        {
            return;
        }

        InventorySlotRow slot = slots[focusedIndex];
        if (slot == null)
        {
            menuCursor.enabled = false;
            return;
        }

        if (slotContainer is RectTransform slotListRect && slotListRect.gameObject.activeInHierarchy)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(slotListRect);
        }

        UIMenuCursor.PositionLeftOf(menuCursor, slot.RectTransform, cursorOffsetFromOptionLeft, cursorSize);
    }

    public bool UseFocused(int index)
    {
        ItemDefinition item = PlayerStats.GetItemAt(index);
        if (item == null)
        {
            return false;
        }

        int healed = item.Execute();
        PlayerStats.RemoveAt(index);

        if (healed > 0)
        {
            SubzoneHUD hud = FindObjectOfType<SubzoneHUD>();
            if (hud != null)
            {
                hud.IncreasePlayerHealthMeter(healed);
            }
        }

        return true;
    }

    public bool DropFocused(int index)
    {
        return PlayerStats.RemoveAt(index);
    }

    private void ResizePanelToSlots()
    {
        if (panelRect == null || !gameObject.activeInHierarchy)
        {
            return;
        }

        int count = slots.Count;
        float height = paddingTop + paddingBottom;
        if (count > 0)
        {
            height += count * slotHeight + (count - 1) * slotSpacing;
        }

        Vector2 size = panelRect.sizeDelta;
        size.y = height;
        panelRect.sizeDelta = size;

        if (slotContainer is RectTransform slotListRect)
        {
            // Parent height changes can shift stretch-child offsets (Left/Right -6/6).
            slotListRect.offsetMin = Vector2.zero;
            slotListRect.offsetMax = Vector2.zero;
            LayoutRebuilder.ForceRebuildLayoutImmediate(slotListRect);
        }

        PositionDescriptionPanel();
    }

    private void PositionDescriptionPanel()
    {
        if (panelRect == null || descriptionPanelRect == null)
        {
            return;
        }

        // Match inventory anchors/pivot so "underneath" is a simple Y offset.
        descriptionPanelRect.anchorMin = panelRect.anchorMin;
        descriptionPanelRect.anchorMax = panelRect.anchorMax;
        descriptionPanelRect.pivot = panelRect.pivot;

        Vector2 size = descriptionPanelRect.sizeDelta;
        size.x = panelRect.sizeDelta.x;
        descriptionPanelRect.sizeDelta = size;

        float underneathY = panelRect.anchoredPosition.y - panelRect.rect.height - descriptionSpacing;
        descriptionPanelRect.anchoredPosition = new Vector2(panelRect.anchoredPosition.x, underneathY);
    }
}
