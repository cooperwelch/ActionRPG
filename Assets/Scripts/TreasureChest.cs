using System.Collections;
using PixelCrushers.DialogueSystem;
using UnityEngine;

public enum TreasureChestItemType
{
    PrimaryWeapon,
    SecondaryWeapon
}

public class TreasureChest : MonoBehaviour
{
    [SerializeField] private string uniqueId;
    [SerializeField] private Sprite closedSprite;
    [SerializeField] private Sprite openSprite;
    [SerializeField] private TreasureChestItemType itemType;
    [SerializeField] private MeleeWeapon primaryWeapon;
    [SerializeField] private SecondaryWeapon secondaryWeapon;
    [SerializeField] private GameObject toolTipIcon;

    private SpriteRenderer spriteRenderer;
    private DialogueSystemTrigger dialogueTrigger;
    private SpriteRenderer tooltipSpriteRenderer;
    private Transform playerInRange;
    private bool isOpen;

    public bool IsOpen => isOpen;

    private void Awake()
    {
        PlayerStats.Initialize();

        spriteRenderer = GetComponent<SpriteRenderer>();
        dialogueTrigger = GetComponent<DialogueSystemTrigger>();
        tooltipSpriteRenderer = toolTipIcon.GetComponent<SpriteRenderer>();

        if (PlayerStats.IsTreasureChestOpened(uniqueId))
        {
            SetOpenState();
        }
        else if (closedSprite != null)
        {
            spriteRenderer.sprite = closedSprite;
        }
    }

    private void Update()
    {
        if (!isOpen && playerInRange != null && UpPressed() && !DialogueManager.IsConversationActive && !GameplayUI.IsBlocking)
        {
            TryOpen(playerInRange);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isOpen || !IsPlayer(other)) return;

        playerInRange = other.transform;
        StopAllCoroutines();
        StartCoroutine(ShowTooltip());
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsPlayer(other)) return;

        playerInRange = null;
        StopAllCoroutines();
        StartCoroutine(HideTooltip());
    }

    public void TryOpen(Transform player)
    {
        if (isOpen || DialogueManager.IsConversationActive) return;

        StopAllCoroutines();
        tooltipSpriteRenderer.color = new Color(
            tooltipSpriteRenderer.color.r,
            tooltipSpriteRenderer.color.g,
            tooltipSpriteRenderer.color.b,
            0
        );

        GrantItem(player);
        PlayerStats.OpenTreasureChest(uniqueId);
        SetOpenState();
        RunItemDialogue(player);
    }

    private void GrantItem(Transform player)
    {
        switch (itemType)
        {
            case TreasureChestItemType.PrimaryWeapon:
                if (primaryWeapon == null)
                {
                    Debug.LogWarning($"TreasureChest '{name}' has no primary weapon assigned.", this);
                    return;
                }

                MeleeController meleeController = player.GetComponentInChildren<MeleeController>();
                if (meleeController != null)
                {
                    meleeController.PickUpMeleeWeapon(primaryWeapon);
                }
                break;

            case TreasureChestItemType.SecondaryWeapon:
                if (secondaryWeapon == null)
                {
                    Debug.LogWarning($"TreasureChest '{name}' has no secondary weapon assigned.", this);
                    return;
                }

                SecondaryWeaponController secondaryWeaponController = player.GetComponentInChildren<SecondaryWeaponController>();
                if (secondaryWeaponController != null)
                {
                    secondaryWeaponController.AcquireSecondaryWeapon(secondaryWeapon);
                }
                break;
        }
    }

    private void SetOpenState()
    {
        isOpen = true;

        if (openSprite != null)
        {
            spriteRenderer.sprite = openSprite;
        }

        spriteRenderer.material = new Material(Shader.Find("Sprites/Default"));

        tooltipSpriteRenderer.color = new Color(
            tooltipSpriteRenderer.color.r,
            tooltipSpriteRenderer.color.g,
            tooltipSpriteRenderer.color.b,
            0
        );
    }

    private void RunItemDialogue(Transform player)
    {
        if (dialogueTrigger == null) return;

        dialogueTrigger.conversationConversant = dialogueTrigger.transform;
        dialogueTrigger.OnUse(player);
    }

    private static bool IsPlayer(Collider2D other)
    {
        return other.CompareTag("Player");
    }

    private static bool UpPressed()
    {
        return Input.GetKeyDown(KeyCode.UpArrow)
            || Input.GetAxisRaw("DPadY") == 1
            || Input.GetAxisRaw("Vertical") == 1;
    }

    private IEnumerator ShowTooltip()
    {
        float duration = 0.2f;
        float elapsedTime = 0;
        float colorAlphaStartingValue = tooltipSpriteRenderer.color.a;

        while (tooltipSpriteRenderer.color.a <= 1)
        {
            float colorAlpha = Mathf.Lerp(colorAlphaStartingValue, 1.0f, elapsedTime / duration);
            tooltipSpriteRenderer.color = new Color(
                tooltipSpriteRenderer.color.r,
                tooltipSpriteRenderer.color.g,
                tooltipSpriteRenderer.color.b,
                colorAlpha
            );
            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator HideTooltip()
    {
        float duration = 0.2f;
        float elapsedTime = 0;
        float colorAlphaStartingValue = tooltipSpriteRenderer.color.a;

        while (tooltipSpriteRenderer.color.a >= 0)
        {
            float colorAlpha = Mathf.Lerp(colorAlphaStartingValue, 0, elapsedTime / duration);
            tooltipSpriteRenderer.color = new Color(
                tooltipSpriteRenderer.color.r,
                tooltipSpriteRenderer.color.g,
                tooltipSpriteRenderer.color.b,
                colorAlpha
            );
            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(uniqueId))
        {
            Debug.LogWarning($"TreasureChest '{name}' needs a uniqueId for persistence.", this);
        }
    }
}
