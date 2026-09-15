using System.Collections;
using PixelCrushers.DialogueSystem;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public enum LevelExitTransition
{
    Automatic,
    Door
}

public class LevelExit : MonoBehaviour
{
    // Override position for Overworld placement when exiting this level
    public Vector3 levelExitOverworldPositionOverride;
    public bool usePositionOverride;

    // Overrides if loading a Subzone from a Subzone
    public Vector3 subzoneLevelStartPositionOverride;
    public bool useSubzoneLevelStartPositionOverride;
    public OverworldSubzoneContainer.PlayerDirection subzoneLevelStartDirectionOverride = OverworldSubzoneContainer.PlayerDirection.Left;

    public string subzone;
    public string levelToLoadOnExit = "Overworld";
    [FormerlySerializedAs("direction")]
    public OverworldSubzoneContainer.PlayerDirection overworldDirectionOverride;
    public LevelExitTransition transitionType = LevelExitTransition.Automatic;

    [SerializeField] private Fader fader;
    [SerializeField] private GameObject doorTooltipPrefab;
    [SerializeField] private float doorEnterDuration = 0.5f;

    private SpriteRenderer tooltipSpriteRenderer;
    private Transform playerInRange;
    private bool isTransitioning;

    private void Awake()
    {
        if (transitionType != LevelExitTransition.Door || doorTooltipPrefab == null)
        {
            return;
        }

        GameObject tooltip = Instantiate(doorTooltipPrefab, transform);
        tooltip.name = "Tooltip";
        tooltipSpriteRenderer = tooltip.GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        if (transitionType != LevelExitTransition.Door || isTransitioning || playerInRange == null)
        {
            return;
        }

        if (!UpPressed() || DialogueManager.IsConversationActive || GameplayUI.IsBlocking)
        {
            return;
        }

        PlayerMovement movement = playerInRange.GetComponent<PlayerMovement>();
        if (movement == null || !movement.canMove || !movement.grounded)
        {
            return;
        }

        BeginExit(playerInRange.gameObject);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.gameObject.CompareTag("Player") || isTransitioning)
        {
            return;
        }

        if (transitionType == LevelExitTransition.Automatic)
        {
            BeginExit(collision.gameObject);
            return;
        }

        playerInRange = collision.transform;
        StopAllCoroutines();
        StartCoroutine(ShowTooltip());
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (transitionType != LevelExitTransition.Door || isTransitioning || !collision.gameObject.CompareTag("Player"))
        {
            return;
        }

        playerInRange = null;
        if (!isActiveAndEnabled)
        {
            HideTooltipImmediate();
            return;
        }

        StopAllCoroutines();
        StartCoroutine(HideTooltip());
    }

    private void BeginExit(GameObject player)
    {
        if (isTransitioning)
        {
            return;
        }

        isTransitioning = true;
        playerInRange = null;

        StopAllCoroutines();
        HideTooltipImmediate();

        PlayerMovement movement = player.GetComponent<PlayerMovement>();
        if (transitionType == LevelExitTransition.Door)
        {
            movement.BeginEnteringDoor();
        }
        else
        {
            movement.FreezeWalking();
            player.GetComponent<Animator>().speed = 0;
        }

        if (usePositionOverride)
        {
            OverworldSubzoneContainer.AddEncounter(
                levelExitOverworldPositionOverride.x,
                levelExitOverworldPositionOverride.y,
                subzone,
                overworldDirectionOverride
            );
        }

        if (useSubzoneLevelStartPositionOverride)
        {
            OverworldSubzoneContainer.AddSubzoneStartPosition(
                subzoneLevelStartPositionOverride.x,
                subzoneLevelStartPositionOverride.y,
                subzoneLevelStartDirectionOverride
            );
        }

        StartCoroutine(DoSceneExit());
    }

    private IEnumerator DoSceneExit()
    {
        if (transitionType == LevelExitTransition.Door)
        {
            yield return new WaitForSeconds(doorEnterDuration);
        }

        yield return StartCoroutine(fader.DoSteppedFadeIn());

        SceneManager.LoadScene(levelToLoadOnExit);

        yield return null;
    }

    private static bool UpPressed()
    {
        return Input.GetKeyDown(KeyCode.UpArrow)
            || Input.GetAxisRaw("DPadY") == 1
            || Input.GetAxisRaw("Vertical") == 1;
    }

    private void HideTooltipImmediate()
    {
        if (tooltipSpriteRenderer == null) return;

        tooltipSpriteRenderer.color = new Color(
            tooltipSpriteRenderer.color.r,
            tooltipSpriteRenderer.color.g,
            tooltipSpriteRenderer.color.b,
            0
        );
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
