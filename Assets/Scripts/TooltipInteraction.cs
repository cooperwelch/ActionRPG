using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PixelCrushers.DialogueSystem;

public class TooltipInteraction : MonoBehaviour
{
    [SerializeField] public GameObject toolTipIcon;

    private bool isInTrigger = false;
    private SpriteRenderer tooltipSpriteRenderer;
    
    // Start is called before the first frame update
    void Start()
    {
        tooltipSpriteRenderer = toolTipIcon.GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (isInTrigger && UpPressed() && DialogueManager.IsConversationActive == false && !GameplayUI.IsBlocking)
        {
            // Get the dialogue trigger component from this gameObject
            DialogueSystemTrigger dialogueTrigger = this.GetComponent<DialogueSystemTrigger>();
            
            if (dialogueTrigger != null)
            {
                StopAllCoroutines();
                HideTooltipImmediate();
                dialogueTrigger.OnUse(transform);
            }
        }
    }

    // When the player enters the trigger, the tooltip icon is enabled
    private void OnTriggerEnter2D(Collider2D other)
    {
        // if the tag is "OverworldHero" or "Hero", the tooltip icon is enabled
        if (other.CompareTag("OverworldHero") || other.CompareTag("Hero") || other.CompareTag("Player"))
        {
            isInTrigger = true;
            if (!isActiveAndEnabled)
            {
                return;
            }

            StopAllCoroutines();
            StartCoroutine(ShowTooltip());
        }
    }

    // When the player exits the trigger, the tooltip icon is disabled
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("OverworldHero") || other.CompareTag("Hero") || other.CompareTag("Player"))
        {
            isInTrigger = false;
            if (!isActiveAndEnabled)
            {
                HideTooltipImmediate();
                return;
            }

            StopAllCoroutines();
            StartCoroutine(HideTooltip());
        }
    }

    private void HideTooltipImmediate()
    {
        if (tooltipSpriteRenderer == null)
        {
            return;
        }

        tooltipSpriteRenderer.color = new Color(
            tooltipSpriteRenderer.color.r,
            tooltipSpriteRenderer.color.g,
            tooltipSpriteRenderer.color.b,
            0
        );
    }

    private bool UpPressed()
    {
        return Input.GetKeyDown(KeyCode.UpArrow) || Input.GetAxisRaw("DPadY") == 1 || Input.GetAxisRaw("Vertical") == 1;
    }

    private IEnumerator ShowTooltip()
    {
        float duration = .2f;
        float elapsedTime = 0;
        float colorAlphaStartingValue = tooltipSpriteRenderer.color.a;
        while (tooltipSpriteRenderer.color.a <= 1)
        {
            float colorAlpha = Mathf.Lerp(colorAlphaStartingValue, 1.0f, elapsedTime / duration);
            tooltipSpriteRenderer.color = new Color(tooltipSpriteRenderer.color.r, tooltipSpriteRenderer.color.g, tooltipSpriteRenderer.color.b, colorAlpha);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        yield return null;
    }

    private IEnumerator HideTooltip()
    {
        float duration = .2f;
        float elapsedTime = 0;
        float colorAlphaStartingValue = tooltipSpriteRenderer.color.a;
        while (tooltipSpriteRenderer.color.a >= 0)
        {
            float colorAlpha = Mathf.Lerp(colorAlphaStartingValue, 0, elapsedTime / duration);
            tooltipSpriteRenderer.color = new Color(tooltipSpriteRenderer.color.r, tooltipSpriteRenderer.color.g, tooltipSpriteRenderer.color.b, colorAlpha);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        yield return null;
    }
}
