using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CraftingUIController : MonoBehaviour
{
    public static bool IsOpen => GameplayUI.IsCraftingOpen;
    public static int LastClosedFrame => GameplayUI.LastClosedFrame;

    [SerializeField] private CraftingRecipeCatalog recipeCatalog;
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TMP_Text hammerLevelText;
    [SerializeField] private Transform recipeListContainer;
    [SerializeField] private CraftingUIRecipeRow recipeRowPrefab;
    [SerializeField] private CraftingUIDoneButton doneButton;
    [SerializeField] private Image menuCursor;
    [SerializeField] private GameObject messagePanel;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private float cursorOffsetFromOptionLeft = 6f;

    private readonly List<CraftingUIRecipeRow> recipeRows = new List<CraftingUIRecipeRow>();
    private readonly List<CraftingRecipe> activeRecipes = new List<CraftingRecipe>();
    private int selectedIndex;
    private bool messageOpen;
    private Vector2 cursorSize = new Vector2(8f, 8f);
    private float axisInputDelayDuration = 0.25f;
    private bool acceptingAxisInputUp = true;
    private bool acceptingAxisInputDown = true;
    private Coroutine delayDownCoroutine;
    private Coroutine delayUpCoroutine;

    private void Awake()
    {
        if (menuCursor != null && menuCursor.rectTransform.sizeDelta.x > 0f)
        {
            cursorSize = menuCursor.rectTransform.sizeDelta;
        }

        CloseImmediate();
    }

    public void Open()
    {
        PlayerStats.Initialize();
        selectedIndex = 0;
        CloseMessageImmediate();
        // Ignore held Up from opening the bench until the axis is released.
        acceptingAxisInputUp = false;
        acceptingAxisInputDown = false;
        if (hammerLevelText != null)
        {
            hammerLevelText.text = $"LVL{PlayerStats.CraftingHammerLevel}";
        }

        panelRoot.SetActive(true);
        PopulateRecipes();
        GameplayUI.IsCraftingOpen = true;
        GameplayUI.FreezePlayer();
        Canvas.ForceUpdateCanvases();
        RebuildRecipeListLayout();
        RefreshSelection();
    }

    public void Close()
    {
        CloseMessageImmediate();
        panelRoot.SetActive(false);
        GameplayUI.IsCraftingOpen = false;
        GameplayUI.NotifyClosed();
        GameplayUI.UnfreezePlayer();
    }

    private void CloseImmediate()
    {
        CloseMessageImmediate();
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }

        GameplayUI.IsCraftingOpen = false;
    }

    private void Update()
    {
        if (!IsOpen) return;

        if (messageOpen)
        {
            HandleMessageDialog();
            return;
        }

        HandleNavigation();
        HandleConfirm();
    }

    private void PopulateRecipes()
    {
        foreach (CraftingUIRecipeRow row in recipeRows)
        {
            if (row != null)
            {
                row.gameObject.SetActive(false);
                Destroy(row.gameObject);
            }
        }

        recipeRows.Clear();
        activeRecipes.Clear();

        List<CraftingRecipe> recipes = recipeCatalog.GetRecipesForHammerLevel(PlayerStats.CraftingHammerLevel);
        foreach (CraftingRecipe recipe in recipes)
        {
            CraftingUIRecipeRow row = Instantiate(recipeRowPrefab, recipeListContainer);
            row.BindRecipe(recipe);
            recipeRows.Add(row);
            activeRecipes.Add(recipe);
        }

        RebuildRecipeListLayout();
    }

    private void HandleNavigation()
    {
        float inputVertical = Input.GetAxisRaw("Vertical") + Input.GetAxisRaw("DPadY");
        int itemCount = recipeRows.Count + 1;

        if (inputVertical < 0 && acceptingAxisInputDown)
        {
            acceptingAxisInputDown = false;
            acceptingAxisInputUp = true;
            if (delayDownCoroutine != null)
            {
                StopCoroutine(delayDownCoroutine);
            }
            delayDownCoroutine = StartCoroutine(DelayAxisInputDown());
            selectedIndex = (selectedIndex + 1) % itemCount;
            RefreshSelection();
        }
        else if (inputVertical > 0 && acceptingAxisInputUp)
        {
            acceptingAxisInputUp = false;
            acceptingAxisInputDown = true;
            if (delayUpCoroutine != null)
            {
                StopCoroutine(delayUpCoroutine);
            }
            delayUpCoroutine = StartCoroutine(DelayAxisInputUp());
            selectedIndex = (selectedIndex - 1 + itemCount) % itemCount;
            RefreshSelection();
        }

        if (inputVertical == 0)
        {
            acceptingAxisInputDown = true;
            acceptingAxisInputUp = true;
        }
    }

    private void HandleConfirm()
    {
        if (!GameplayUI.ConfirmPressed())
        {
            return;
        }

        if (IsDoneSelected())
        {
            Close();
            return;
        }

        CraftingRecipe recipe = activeRecipes[selectedIndex];
        string blockedMessage = recipe.GetBlockedCraftMessage();
        if (!string.IsNullOrEmpty(blockedMessage))
        {
            ShowMessage(blockedMessage);
            return;
        }

        if (recipe.Execute())
        {
            RefreshSelection();
        }
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

        PositionCursor();
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

    private void RefreshSelection()
    {
        for (int i = 0; i < recipeRows.Count; i++)
        {
            recipeRows[i].RefreshState(activeRecipes[i], i == selectedIndex);
        }

        if (doneButton != null)
        {
            doneButton.SetSelected(IsDoneSelected());
        }

        PositionCursor();
    }

    private void PositionCursor()
    {
        if (menuCursor == null)
        {
            return;
        }

        menuCursor.enabled = !messageOpen;
        if (messageOpen)
        {
            return;
        }

        RebuildRecipeListLayout();
        UIMenuCursor.PositionLeftOf(menuCursor, SelectedOptionRect(), cursorOffsetFromOptionLeft, cursorSize);
    }

    private RectTransform SelectedOptionRect()
    {
        if (IsDoneSelected())
        {
            return doneButton != null ? doneButton.transform as RectTransform : null;
        }

        if (selectedIndex < 0 || selectedIndex >= recipeRows.Count || recipeRows[selectedIndex] == null)
        {
            return null;
        }

        return recipeRows[selectedIndex].RectTransform;
    }

    private bool IsDoneSelected()
    {
        return selectedIndex >= recipeRows.Count;
    }

    private void RebuildRecipeListLayout()
    {
        if (recipeListContainer is RectTransform listRect && listRect.gameObject.activeInHierarchy)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(listRect);
        }
    }

    private IEnumerator DelayAxisInputDown()
    {
        float elapsedTime = 0f;
        while (elapsedTime < axisInputDelayDuration)
        {
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        acceptingAxisInputDown = true;
        delayDownCoroutine = null;
    }

    private IEnumerator DelayAxisInputUp()
    {
        float elapsedTime = 0f;
        while (elapsedTime < axisInputDelayDuration)
        {
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        acceptingAxisInputUp = true;
        delayUpCoroutine = null;
    }
}
