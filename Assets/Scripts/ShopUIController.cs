using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopUIController : MonoBehaviour
{
    [SerializeField] private ShopCatalog catalog;
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Transform itemListContainer;
    [SerializeField] private ShopUIRow itemRowPrefab;
    [SerializeField] private CraftingUIDoneButton doneButton;
    [SerializeField] private Image menuCursor;
    [SerializeField] private GameObject descriptionPanel;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private float descriptionSpacing = -7f;
    [SerializeField] private GameObject confirmPanel;
    [SerializeField] private TMP_Text confirmText;
    [SerializeField] private List<MainMenuOptionRow> confirmOptions = new List<MainMenuOptionRow>();
    [SerializeField] private Image confirmCursor;
    [SerializeField] private GameObject messagePanel;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private float cursorOffsetFromOptionLeft = 6f;

    private readonly List<ShopUIRow> rows = new List<ShopUIRow>();
    private readonly List<ItemDefinition> items = new List<ItemDefinition>();
    private readonly List<ShopListing> activeListings = new List<ShopListing>();
    private UIListNavigator navigator;
    private int selectedIndex;
    private int confirmIndex;
    private bool confirmOpen;
    private bool messageOpen;
    private Vector2 cursorSize = new Vector2(8f, 8f);
    private RectTransform panelRect;
    private RectTransform descriptionPanelRect;

    private void Awake()
    {
        navigator = new UIListNavigator(this);
        if (panelRoot != null)
        {
            panelRect = panelRoot.transform as RectTransform;
        }

        if (descriptionPanel != null)
        {
            descriptionPanelRect = descriptionPanel.transform as RectTransform;
        }

        if (menuCursor != null && menuCursor.rectTransform.sizeDelta.x > 0f)
        {
            cursorSize = menuCursor.rectTransform.sizeDelta;
        }
        else if (confirmCursor != null && confirmCursor.rectTransform.sizeDelta.x > 0f)
        {
            cursorSize = confirmCursor.rectTransform.sizeDelta;
        }

        CloseImmediate();
    }

    public void Open()
    {
        Open(catalog);
    }

    public void Open(ShopCatalog shopCatalog)
    {
        if (shopCatalog != null)
        {
            catalog = shopCatalog;
        }

        if (catalog == null)
        {
            return;
        }

        PlayerStats.Initialize();
        selectedIndex = 0;
        confirmOpen = false;
        CloseMessageImmediate();
        if (confirmPanel != null)
        {
            confirmPanel.SetActive(false);
        }

        panelRoot.SetActive(true);
        PopulateItems();
        navigator.Prepare(0);
        GameplayUI.IsShopOpen = true;
        GameplayUI.FreezePlayer();
        Canvas.ForceUpdateCanvases();
        RebuildItemListLayout();
        RefreshSelection();
    }

    public void Close()
    {
        CloseMessageImmediate();
        if (confirmPanel != null)
        {
            confirmPanel.SetActive(false);
        }

        if (descriptionPanel != null)
        {
            descriptionPanel.SetActive(false);
        }

        confirmOpen = false;
        panelRoot.SetActive(false);
        GameplayUI.IsShopOpen = false;
        GameplayUI.NotifyClosed();
        GameplayUI.UnfreezePlayer();
    }

    private void CloseImmediate()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }

        if (confirmPanel != null)
        {
            confirmPanel.SetActive(false);
        }

        if (descriptionPanel != null)
        {
            descriptionPanel.SetActive(false);
        }

        CloseMessageImmediate();
        confirmOpen = false;
        GameplayUI.IsShopOpen = false;
    }

    private void Update()
    {
        if (!GameplayUI.IsShopOpen)
        {
            return;
        }

        if (messageOpen)
        {
            HandleMessageDialog();
            return;
        }

        if (confirmOpen)
        {
            HandleConfirmDialog();
            return;
        }

        if (navigator.TickVertical(rows.Count + 1))
        {
            selectedIndex = navigator.Index;
            RefreshSelection();
        }

        if (GameplayUI.ConfirmPressed())
        {
            HandleConfirm();
        }
    }

    private void PopulateItems()
    {
        foreach (ShopUIRow row in rows)
        {
            if (row != null)
            {
                row.gameObject.SetActive(false);
                Destroy(row.gameObject);
            }
        }

        rows.Clear();
        items.Clear();
        activeListings.Clear();

        foreach (ShopListing listing in catalog.listings)
        {
            if (listing == null || listing.item == null)
            {
                continue;
            }

            ShopUIRow row = Instantiate(itemRowPrefab, itemListContainer);
            row.Bind(listing.item, false);
            rows.Add(row);
            items.Add(listing.item);
            activeListings.Add(listing);
        }

        RebuildItemListLayout();
    }

    private void HandleConfirm()
    {
        if (IsDoneSelected())
        {
            Close();
            return;
        }

        ItemDefinition item = items[selectedIndex];
        if (item == null)
        {
            return;
        }

        string blockedMessage = item.GetBlockedPurchaseMessage();
        if (!string.IsNullOrEmpty(blockedMessage))
        {
            ShowMessage(blockedMessage);
            return;
        }

        if (!CanPurchase(selectedIndex))
        {
            return;
        }

        confirmOpen = true;
        confirmIndex = 0;
        navigator.Prepare(0);
        if (confirmPanel != null)
        {
            confirmPanel.SetActive(true);
        }

        if (confirmText != null)
        {
            confirmText.text = $"Purchase {item.displayName}?";
        }

        RefreshConfirm();
        PositionListCursor();
    }

    private void HandleConfirmDialog()
    {
        if (GameplayUI.CancelPressed())
        {
            CloseConfirm();
            return;
        }

        if (navigator.TickVertical(confirmOptions.Count) || navigator.TickHorizontal(confirmOptions.Count))
        {
            confirmIndex = navigator.Index;
            RefreshConfirm();
        }

        if (!GameplayUI.ConfirmPressed())
        {
            return;
        }

        if (confirmIndex == 0)
        {
            TryPurchase();
        }

        CloseConfirm();
    }

    private void HandleMessageDialog()
    {
        if (GameplayUI.ConfirmPressed())
        {
            CloseMessage();
        }
    }

    private void ShowMessage(string message)
    {
        messageOpen = true;
        if (messageText != null)
        {
            messageText.text = message;
        }

        if (messagePanel != null)
        {
            messagePanel.SetActive(true);
        }

        PositionListCursor();
    }

    private void CloseMessage()
    {
        CloseMessageImmediate();
        RefreshSelection();
    }

    private void CloseMessageImmediate()
    {
        messageOpen = false;
        if (messagePanel != null)
        {
            messagePanel.SetActive(false);
        }
    }

    private void TryPurchase()
    {
        ItemDefinition item = items[selectedIndex];
        if (!CanPurchase(selectedIndex))
        {
            return;
        }

        if (!PlayerStats.SpendDenarius(item.cost))
        {
            return;
        }

        PlayerStats.TryAddItem(item);
    }

    private bool CanPurchase(int index)
    {
        if (index < 0 || index >= items.Count)
        {
            return false;
        }

        ItemDefinition item = items[index];
        if (item == null || PlayerStats.Denarius < item.cost || !PlayerStats.HasEmptySlot())
        {
            return false;
        }

        if (index < activeListings.Count && activeListings[index] != null && activeListings[index].stockLimit == 0)
        {
            return false;
        }

        return true;
    }

    private void CloseConfirm()
    {
        confirmOpen = false;
        if (confirmPanel != null)
        {
            confirmPanel.SetActive(false);
        }

        navigator.Prepare(selectedIndex, false);
        RefreshSelection();
    }

    private void RefreshSelection()
    {
        for (int i = 0; i < rows.Count; i++)
        {
            rows[i].Bind(items[i], !IsDoneSelected() && i == selectedIndex);
        }

        if (doneButton != null)
        {
            doneButton.SetSelected(IsDoneSelected());
        }

        bool showDescription = !IsDoneSelected() && selectedIndex >= 0 && selectedIndex < items.Count;
        ItemDefinition focusedItem = showDescription ? items[selectedIndex] : null;

        if (descriptionPanel != null)
        {
            descriptionPanel.SetActive(showDescription);
            if (showDescription)
            {
                PositionDescriptionPanel();
            }
        }

        if (descriptionText != null)
        {
            descriptionText.text = focusedItem != null ? focusedItem.description : string.Empty;
        }

        PositionListCursor();
    }

    private void PositionListCursor()
    {
        if (menuCursor == null)
        {
            return;
        }

        menuCursor.enabled = !confirmOpen && !messageOpen;
        if (confirmOpen || messageOpen)
        {
            return;
        }

        RebuildItemListLayout();
        UIMenuCursor.PositionLeftOf(menuCursor, SelectedListOptionRect(), cursorOffsetFromOptionLeft, cursorSize);
    }

    private RectTransform SelectedListOptionRect()
    {
        if (IsDoneSelected())
        {
            return doneButton != null ? doneButton.transform as RectTransform : null;
        }

        if (selectedIndex < 0 || selectedIndex >= rows.Count || rows[selectedIndex] == null)
        {
            return null;
        }

        return rows[selectedIndex].RectTransform;
    }

    private void RebuildItemListLayout()
    {
        if (itemListContainer is RectTransform listRect && listRect.gameObject.activeInHierarchy)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(listRect);
        }
    }

    private void PositionDescriptionPanel()
    {
        if (panelRect == null || descriptionPanelRect == null)
        {
            return;
        }

        // Align with the shop panel and sit just underneath it.
        descriptionPanelRect.anchorMin = new Vector2(panelRect.anchorMax.x, panelRect.anchorMax.y);
        descriptionPanelRect.anchorMax = new Vector2(panelRect.anchorMax.x, panelRect.anchorMax.y);
        descriptionPanelRect.pivot = panelRect.pivot;

        Vector2 size = descriptionPanelRect.sizeDelta;
        size.x = panelRect.sizeDelta.x;
        descriptionPanelRect.sizeDelta = size;

        float underneathY = panelRect.anchoredPosition.y - panelRect.rect.height - descriptionSpacing;
        descriptionPanelRect.anchoredPosition = new Vector2(panelRect.anchoredPosition.x, underneathY);
    }

    private void RefreshConfirm()
    {
        for (int i = 0; i < confirmOptions.Count; i++)
        {
            if (confirmOptions[i] != null)
            {
                confirmOptions[i].Refresh(i == confirmIndex, false);
            }
        }

        RebuildConfirmLayout();
        PositionConfirmCursor();
    }

    private void RebuildConfirmLayout()
    {
        if (confirmOptions.Count == 0 || confirmOptions[0] == null)
        {
            return;
        }

        for (int i = 0; i < confirmOptions.Count; i++)
        {
            if (confirmOptions[i] == null)
            {
                continue;
            }

            RectTransform labelRect = confirmOptions[i].LabelRectTransform;
            if (labelRect != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(labelRect);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(confirmOptions[i].RectTransform);
        }

        RectTransform optionsRoot = confirmOptions[0].RectTransform.parent as RectTransform;
        if (optionsRoot != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(optionsRoot);
        }
    }

    private void PositionConfirmCursor()
    {
        if (confirmCursor == null || confirmIndex < 0 || confirmIndex >= confirmOptions.Count)
        {
            return;
        }

        MainMenuOptionRow option = confirmOptions[confirmIndex];
        if (option == null)
        {
            return;
        }

        UIMenuCursor.PositionLeftOf(
            confirmCursor,
            option.LabelRectTransform,
            cursorOffsetFromOptionLeft,
            cursorSize
        );
    }

    private bool IsDoneSelected()
    {
        return selectedIndex >= rows.Count;
    }
}
