using System.Collections;
using PixelCrushers.DialogueSystem;
using UnityEngine;

public class CraftingBench : MonoBehaviour
{
    [SerializeField] private DialogueSystemTrigger noHammerDialogueTrigger;
    [SerializeField] private GameObject toolTipIcon;

    private SpriteRenderer tooltipSpriteRenderer;
    private Transform playerInRange;
    private CraftingUIController craftingUIController;

    private void Awake()
    {
        PlayerStats.Initialize();
        craftingUIController = FindObjectOfType<CraftingUIController>();

        if (toolTipIcon != null)
        {
            tooltipSpriteRenderer = toolTipIcon.GetComponent<SpriteRenderer>();
        }
    }

    private void Update()
    {
        if (playerInRange == null || GameplayUI.IsBlocking) return;

        if (UpPressed() && !DialogueManager.IsConversationActive)
        {
            TryInteract(playerInRange);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsPlayer(other)) return;

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

    public void TryInteract(Transform player)
    {
        if (DialogueManager.IsConversationActive || GameplayUI.IsBlocking) return;

        StopAllCoroutines();
        if (tooltipSpriteRenderer != null)
        {
            tooltipSpriteRenderer.color = new Color(
                tooltipSpriteRenderer.color.r,
                tooltipSpriteRenderer.color.g,
                tooltipSpriteRenderer.color.b,
                0
            );
        }

        if (!PlayerStats.HasCraftingHammer)
        {
            if (noHammerDialogueTrigger != null)
            {
                noHammerDialogueTrigger.conversationConversant = noHammerDialogueTrigger.transform;
                noHammerDialogueTrigger.OnUse(player);
            }
            return;
        }

        if (craftingUIController != null)
        {
            craftingUIController.Open();
        }
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
        if (tooltipSpriteRenderer == null) yield break;

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
        if (tooltipSpriteRenderer == null) yield break;

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
}
