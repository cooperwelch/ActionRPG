using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuUIController : MonoBehaviour
{
    private enum MenuState
    {
        Closed,
        Main,
        Mode,
        Use,
        Drop,
        Equipment
    }

    private const int MainOptionItems = 0;
    private const int MainOptionEquipment = 1;
    private const int MainOptionQuit = 3;

    [SerializeField] private GameObject panelRoot;
    [SerializeField] private GameObject modePanel;
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private GameObject equipmentPanel;
    [SerializeField] private List<MainMenuOptionRow> mainOptions = new List<MainMenuOptionRow>();
    [SerializeField] private List<MainMenuOptionRow> modeOptions = new List<MainMenuOptionRow>();
    [SerializeField] private Image mainCursor;
    [SerializeField] private Image modeCursor;
    [SerializeField] private InventoryUIController inventoryUI;
    [SerializeField] private EquipmentUIController equipmentUI;
    [SerializeField] private float cursorOffsetFromOptionLeft = 6f;

    private UIListNavigator navigator;
    private MenuState state = MenuState.Closed;
    private int mainIndex;
    private int modeIndex;
    private int inventoryIndex;
    private int equipmentRow;
    private int equipmentColumn;
    private SubzoneHUD subzoneHUD;
    private Vector2 cursorSize = new Vector2(8f, 8f);

    private void Awake()
    {
        navigator = new UIListNavigator(this);
        subzoneHUD = GetComponentInParent<SubzoneHUD>();
        if (mainCursor != null && mainCursor.rectTransform.sizeDelta.x > 0f)
        {
            cursorSize = mainCursor.rectTransform.sizeDelta;
        }
        else if (modeCursor != null && modeCursor.rectTransform.sizeDelta.x > 0f)
        {
            cursorSize = modeCursor.rectTransform.sizeDelta;
        }

        CloseImmediate();
    }

    private void Update()
    {
        if (state == MenuState.Closed)
        {
            if (GameplayUI.MenuPressed() && GameplayUI.CanOpenMainMenu())
            {
                Open();
            }

            return;
        }

        if (GameplayUI.MenuPressed())
        {
            Close();
            return;
        }

        if (GameplayUI.CancelPressed() || Input.GetButtonDown("Fire2"))
        {
            HandleCancel();
            return;
        }

        if (GameplayUI.ConfirmPressed())
        {
            HandleConfirm();
            return;
        }

        HandleNavigation();
    }

    public void Open()
    {
        PlayerStats.Initialize();
        panelRoot.SetActive(true);
        SetItemsVisible(false);
        SetEquipmentVisible(false);
        Canvas.ForceUpdateCanvases();
        RebuildLayout(LayoutRootOf(mainOptions));
        state = MenuState.Main;
        mainIndex = FirstEnabledIndex(mainOptions);
        navigator.Prepare(mainIndex);
        GameplayUI.IsMainMenuOpen = true;
        GameplayUI.FreezePlayer();
        if (subzoneHUD != null)
        {
            subzoneHUD.SetHudVisibleForMenu(true);
        }

        RefreshMain(false);
    }

    public void Close()
    {
        CloseImmediate();
        GameplayUI.NotifyClosed();
        GameplayUI.UnfreezePlayer();
        if (subzoneHUD != null)
        {
            subzoneHUD.SetHudVisibleForMenu(false);
        }
    }

    private void CloseImmediate()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }

        SetItemsVisible(false);
        SetEquipmentVisible(false);
        state = MenuState.Closed;
        GameplayUI.IsMainMenuOpen = false;
    }

    private void HandleNavigation()
    {
        switch (state)
        {
            case MenuState.Main:
                if (TickEnabled(mainOptions, ref mainIndex))
                {
                    RefreshMain(false);
                }
                break;
            case MenuState.Mode:
                if (TickEnabled(modeOptions, ref modeIndex))
                {
                    RefreshMode();
                }
                break;
            case MenuState.Use:
            case MenuState.Drop:
                if (inventoryUI != null && navigator.TickVertical(PlayerStats.InventoryCapacity))
                {
                    inventoryIndex = navigator.Index;
                    inventoryUI.Refresh(inventoryIndex, true);
                }
                break;
            case MenuState.Equipment:
                HandleEquipmentNavigation();
                break;
        }
    }

    private void HandleEquipmentNavigation()
    {
        if (equipmentUI == null)
        {
            return;
        }

        bool changed = false;

        navigator.SetIndex(equipmentColumn);
        if (navigator.TickHorizontal(EquipmentUIController.SlotsPerRow))
        {
            equipmentColumn = navigator.Index;
            changed = true;
        }

        navigator.SetIndex(equipmentRow);
        if (navigator.TickVertical(EquipmentUIController.RowCount))
        {
            equipmentRow = navigator.Index;
            changed = true;
        }

        navigator.SetIndex(equipmentColumn);

        if (changed)
        {
            equipmentUI.Refresh(equipmentRow, equipmentColumn, true);
        }
    }

    private void HandleConfirm()
    {
        switch (state)
        {
            case MenuState.Main:
                if (mainIndex < 0 || mainIndex >= mainOptions.Count || !mainOptions[mainIndex].IsEnabled)
                {
                    return;
                }

                if (mainIndex == MainOptionItems)
                {
                    OpenItems();
                }
                else if (mainIndex == MainOptionEquipment)
                {
                    OpenEquipment();
                }
                else if (mainIndex == MainOptionQuit)
                {
                    Close();
                }
                break;
            case MenuState.Mode:
                if (modeIndex < 0 || modeIndex >= modeOptions.Count || !modeOptions[modeIndex].IsEnabled)
                {
                    return;
                }

                inventoryIndex = 0;
                state = modeIndex == 0 ? MenuState.Use : MenuState.Drop;
                navigator.Prepare(inventoryIndex);
                inventoryUI.Refresh(inventoryIndex, true);
                RefreshMode();
                break;
            case MenuState.Use:
                if (inventoryUI != null)
                {
                    inventoryUI.UseFocused(inventoryIndex);
                    inventoryUI.Refresh(inventoryIndex, true);
                }
                break;
            case MenuState.Drop:
                if (inventoryUI != null)
                {
                    inventoryUI.DropFocused(inventoryIndex);
                    inventoryUI.Refresh(inventoryIndex, true);
                }
                break;
            case MenuState.Equipment:
                if (equipmentUI != null)
                {
                    equipmentUI.TryEquipFocused(equipmentRow, equipmentColumn);
                    equipmentUI.Refresh(equipmentRow, equipmentColumn, true);
                }
                break;
        }
    }

    private void HandleCancel()
    {
        switch (state)
        {
            case MenuState.Main:
                Close();
                break;
            case MenuState.Mode:
                SetItemsVisible(false);
                state = MenuState.Main;
                navigator.Prepare(mainIndex, false);
                RefreshMain(false);
                break;
            case MenuState.Use:
            case MenuState.Drop:
                state = MenuState.Mode;
                navigator.Prepare(modeIndex, false);
                if (inventoryUI != null)
                {
                    inventoryUI.Refresh(-1, false);
                }
                Canvas.ForceUpdateCanvases();
                RebuildLayout(LayoutRootOf(modeOptions));
                RefreshMode();
                break;
            case MenuState.Equipment:
                SetEquipmentVisible(false);
                state = MenuState.Main;
                navigator.Prepare(mainIndex, false);
                RefreshMain(false);
                break;
        }
    }

    private void OpenItems()
    {
        SetEquipmentVisible(false);
        SetItemsVisible(true);
        modeIndex = FirstEnabledIndex(modeOptions);
        state = MenuState.Mode;
        navigator.Prepare(modeIndex);
        if (inventoryUI != null)
        {
            inventoryUI.Refresh(-1, false);
        }

        RefreshMain(true);
        Canvas.ForceUpdateCanvases();
        RebuildLayout(LayoutRootOf(modeOptions));
        RefreshMode();
    }

    private void OpenEquipment()
    {
        SetItemsVisible(false);
        SetEquipmentVisible(true);
        equipmentRow = EquipmentUIController.PrimaryRow;
        equipmentColumn = 0;
        state = MenuState.Equipment;
        navigator.Prepare(equipmentColumn);
        RefreshMain(true);
        Canvas.ForceUpdateCanvases();
        if (equipmentUI != null)
        {
            equipmentUI.Refresh(equipmentRow, equipmentColumn, true);
        }
    }

    private void SetItemsVisible(bool visible)
    {
        if (modePanel != null)
        {
            modePanel.SetActive(visible);
        }

        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(visible);
        }

        // Description panel is a sibling of InventoryPanel; hide it when leaving Items.
        if (!visible && inventoryUI != null)
        {
            inventoryUI.Refresh(-1, false);
        }
    }

    private void SetEquipmentVisible(bool visible)
    {
        if (equipmentPanel != null)
        {
            equipmentPanel.SetActive(visible);
        }

        if (!visible && equipmentUI != null)
        {
            equipmentUI.HideDescription();
            equipmentUI.Refresh(0, 0, false);
        }
    }

    private void RefreshMain(bool secondaryOpen)
    {
        for (int i = 0; i < mainOptions.Count; i++)
        {
            bool focused = !secondaryOpen && i == mainIndex;
            bool disabled = !mainOptions[i].IsEnabled || (secondaryOpen && i != mainIndex);
            mainOptions[i].Refresh(focused || (secondaryOpen && i == mainIndex), disabled);
        }

        if (mainCursor != null)
        {
            mainCursor.enabled = !secondaryOpen;
        }

        if (!secondaryOpen)
        {
            PositionCursor(mainCursor, mainOptions, mainIndex);
        }
    }

    private void RefreshMode()
    {
        bool inventoryHasFocus = state == MenuState.Use || state == MenuState.Drop;
        for (int i = 0; i < modeOptions.Count; i++)
        {
            bool focused = !inventoryHasFocus && i == modeIndex;
            bool disabled = !modeOptions[i].IsEnabled || (inventoryHasFocus && i != modeIndex);
            modeOptions[i].Refresh(focused || (inventoryHasFocus && i == modeIndex), disabled);
        }

        if (modeCursor != null)
        {
            modeCursor.enabled = !inventoryHasFocus;
        }

        if (!inventoryHasFocus)
        {
            PositionCursor(modeCursor, modeOptions, modeIndex);
        }
    }

    private bool TickEnabled(List<MainMenuOptionRow> options, ref int currentIndex)
    {
        int enabledCount = CountEnabled(options);
        if (enabledCount <= 0)
        {
            return false;
        }

        int previous = currentIndex;
        if (!navigator.TickVertical(options.Count))
        {
            return false;
        }

        int direction = navigator.Index >= previous || (previous == options.Count - 1 && navigator.Index == 0)
            ? 1
            : -1;
        if (previous == 0 && navigator.Index == options.Count - 1)
        {
            direction = -1;
        }

        int next = currentIndex;
        do
        {
            next = (next + direction + options.Count) % options.Count;
        }
        while (!options[next].IsEnabled && next != currentIndex);

        currentIndex = next;
        navigator.SetIndex(currentIndex);
        return currentIndex != previous || options[currentIndex].IsEnabled;
    }

    private static int FirstEnabledIndex(List<MainMenuOptionRow> options)
    {
        for (int i = 0; i < options.Count; i++)
        {
            if (options[i] != null && options[i].IsEnabled)
            {
                return i;
            }
        }

        return 0;
    }

    private static int CountEnabled(List<MainMenuOptionRow> options)
    {
        int count = 0;
        for (int i = 0; i < options.Count; i++)
        {
            if (options[i] != null && options[i].IsEnabled)
            {
                count++;
            }
        }

        return count;
    }

    private void PositionCursor(Image cursor, List<MainMenuOptionRow> options, int index)
    {
        if (cursor == null || index < 0 || index >= options.Count || options[index] == null)
        {
            return;
        }

        RectTransform cursorRect = cursor.rectTransform;
        RectTransform optionRect = options[index].RectTransform;
        RectTransform parent = cursorRect.parent as RectTransform;
        if (parent == null)
        {
            return;
        }

        // Keep top-left anchors so Pos Y matches panel space (top-left pivot).
        // Bottom-left anchors + a top-relative Y shifts the cursor down by the panel height.
        cursorRect.anchorMin = new Vector2(0f, 1f);
        cursorRect.anchorMax = new Vector2(0f, 1f);
        cursorRect.pivot = new Vector2(0.5f, 0.5f);
        // Activating ModePanel + changing anchors can collapse sizeDelta to 0; restore it.
        cursorRect.sizeDelta = cursorSize;

        Vector3 worldTarget = optionRect.TransformPoint(new Vector3(
            optionRect.rect.xMin - cursorOffsetFromOptionLeft,
            optionRect.rect.center.y,
            0f
        ));
        Vector3 localPoint = parent.InverseTransformPoint(worldTarget);
        cursorRect.localPosition = new Vector3(localPoint.x, localPoint.y, cursorRect.localPosition.z);
    }

    private static void RebuildLayout(RectTransform layoutRoot)
    {
        if (layoutRoot != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(layoutRoot);
        }
    }

    private static RectTransform LayoutRootOf(List<MainMenuOptionRow> options)
    {
        if (options == null || options.Count == 0 || options[0] == null)
        {
            return null;
        }

        return options[0].RectTransform.parent as RectTransform;
    }
}
