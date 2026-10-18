using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using PixelCrushers;
using PixelCrushers.DialogueSystem;

/// <summary>
/// Measures dialog height at the final width (with full subtitle text), locks that height,
/// then plays the panel show animation so horizontal expand does not reflow/jerk height
/// via Content Size Fitter.
/// Keeps the panel in Opening until that show animation finishes so Dialogue System
/// does not treat the panel as Open mid-animation.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class DialogPanelFlexibleHeightHelper : MonoBehaviour
{
    [SerializeField] private float finalSizeDeltaX = -224f;
    [SerializeField] private float startSizeDeltaX = -434f;
    [SerializeField] private string showTriggerName = "Show";
    [Tooltip("Frames to wait after open so subtitle text/typewriter can populate before measuring.")]
    [SerializeField] private int settleFrames = 1;

    private RectTransform rectTransform;
    private ContentSizeFitter contentSizeFitter;
    private LayoutElement layoutElement;
    private Animator animator;
    private CanvasGroup canvasGroup;
    private UIPanel uiPanel;
    private StandardUISubtitlePanel subtitlePanel;

    private float lockedHeight;
    private bool heightLocked;
    private bool hideUntilMeasured;
    private Coroutine prepareCoroutine;
    private float savedAnimatorSpeed = 1f;
    private bool animatorSpeedOverridden;

    private void Awake()
    {
        rectTransform = (RectTransform)transform;
        contentSizeFitter = GetComponent<ContentSizeFitter>();
        layoutElement = GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = gameObject.AddComponent<LayoutElement>();
        }

        animator = GetComponent<Animator>();
        canvasGroup = GetComponent<CanvasGroup>();
        uiPanel = GetComponent<UIPanel>();
        subtitlePanel = GetComponent<StandardUISubtitlePanel>();

        // Open() runs before subtitle text is set. Keep the show trigger so
        // waitForShowAnimationToSetOpen can hold Opening while we freeze the
        // animator, measure at full width, replay Show, then mark Open.
        if (uiPanel != null)
        {
            if (string.IsNullOrEmpty(showTriggerName) && !string.IsNullOrEmpty(uiPanel.showAnimationTrigger))
            {
                showTriggerName = uiPanel.showAnimationTrigger;
            }

            uiPanel.onOpen.AddListener(OnPanelOpen);
            uiPanel.onClose.AddListener(OnPanelClose);
        }
    }

    private void OnDestroy()
    {
        if (uiPanel == null)
        {
            return;
        }

        uiPanel.onOpen.RemoveListener(OnPanelOpen);
        uiPanel.onClose.RemoveListener(OnPanelClose);
    }

    private void LateUpdate()
    {
        // Keep hidden until height is locked so pre-measure CSF size never flashes.
        // Also fights the frozen Show clip, which keys CanvasGroup alpha to 1.
        if (hideUntilMeasured && canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }

        // Animator Write Defaults + clips that only key SizeDelta.x can overwrite height.
        if (heightLocked)
        {
            ApplyLockedHeight();
        }
    }

    private void OnPanelOpen()
    {
        if (prepareCoroutine != null)
        {
            StopCoroutine(prepareCoroutine);
        }

        // Ensure Open()'s upcoming SetTrigger waits, so panelState stays Opening
        // long enough for FocusWhenOpen / other Open gates.
        if (!string.IsNullOrEmpty(showTriggerName))
        {
            uiPanel.showAnimationTrigger = showTriggerName;
        }

        uiPanel.waitForShowAnimation = true;

        hideUntilMeasured = true;
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }

        // Freeze so the Show clip started by Open does not advance before we measure/replay.
        FreezeAnimator();

        prepareCoroutine = StartCoroutine(PrepareThenPlayShow());
    }

    private void OnPanelClose()
    {
        if (prepareCoroutine != null)
        {
            StopCoroutine(prepareCoroutine);
            prepareCoroutine = null;
        }

        if (uiPanel != null)
        {
            uiPanel.animatorMonitor.CancelCurrentAnimation();
        }

        hideUntilMeasured = false;
        RestoreAnimatorSpeed();
        UnlockHeight();
    }

    private IEnumerator PrepareThenPlayShow()
    {
        // Let Open() start WaitForAnimation(Show) and FocusWhenOpen begin waiting.
        yield return null;

        if (uiPanel == null)
        {
            prepareCoroutine = null;
            yield break;
        }

        // Stop Open's wait so OnVisible is not invoked from the frozen premature Show.
        uiPanel.animatorMonitor.CancelCurrentAnimation();
        EnsureOpening();

        int frames = Mathf.Max(0, settleFrames);
        for (int i = 0; i < frames; i++)
        {
            yield return null;
        }

        // Typewriter may need an extra frame to place full text with transparent remainder.
        int safety = 0;
        while (string.IsNullOrEmpty(GetSubtitleText()) && safety < 8)
        {
            safety++;
            yield return null;
        }

        if (uiPanel.panelState == UIPanel.PanelState.Closing ||
            uiPanel.panelState == UIPanel.PanelState.Closed)
        {
            prepareCoroutine = null;
            yield break;
        }

        MeasureAndLockHeight();
        RestoreAnimatorSpeed();
        EnsureOpening();

        // Reveal only after height is locked; Show will drive alpha from here.
        hideUntilMeasured = false;

        if (animator == null || !animator.enabled || string.IsNullOrEmpty(showTriggerName))
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }

            MarkOpen();
            prepareCoroutine = null;
            yield break;
        }

        // Restart Show from t=0 without going through Start/Hidden, which keys
        // SizeDelta.y to a fixed placeholder height and causes a visible snap.
        if (animator != null && !string.IsNullOrEmpty(showTriggerName))
        {
            animator.ResetTrigger(showTriggerName);
        }

        animator.Play(showTriggerName, 0, 0f);
        ApplyLockedHeight();
        yield return null;
        ApplyLockedHeight();

        float timeout = Time.realtimeSinceStartup + UIAnimatorMonitor.MaxWaitDuration;
        while (Time.realtimeSinceStartup < timeout)
        {
            if (uiPanel == null ||
                uiPanel.panelState == UIPanel.PanelState.Closing ||
                uiPanel.panelState == UIPanel.PanelState.Closed)
            {
                prepareCoroutine = null;
                yield break;
            }

            AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
            if (info.IsName(showTriggerName) && info.normalizedTime >= 1f)
            {
                break;
            }

            yield return null;
        }

        MarkOpen();
        prepareCoroutine = null;
    }

    private void EnsureOpening()
    {
        if (uiPanel == null)
        {
            return;
        }

        if (uiPanel.panelState != UIPanel.PanelState.Closing &&
            uiPanel.panelState != UIPanel.PanelState.Closed)
        {
            uiPanel.panelState = UIPanel.PanelState.Opening;
        }
    }

    private void MarkOpen()
    {
        if (uiPanel == null)
        {
            return;
        }

        if (uiPanel.panelState == UIPanel.PanelState.Opening)
        {
            uiPanel.panelState = UIPanel.PanelState.Open;
            uiPanel.RefreshSelectablesList();
        }
    }

    private void FreezeAnimator()
    {
        if (animator == null)
        {
            return;
        }

        if (!animatorSpeedOverridden)
        {
            savedAnimatorSpeed = animator.speed > 0f ? animator.speed : 1f;
            animatorSpeedOverridden = true;
        }

        animator.speed = 0f;
    }

    private void RestoreAnimatorSpeed()
    {
        if (animator == null || !animatorSpeedOverridden)
        {
            return;
        }

        animator.speed = savedAnimatorSpeed;
        animatorSpeedOverridden = false;
    }

    private string GetSubtitleText()
    {
        if (subtitlePanel == null || subtitlePanel.subtitleText == null)
        {
            return null;
        }

        return subtitlePanel.subtitleText.text;
    }

    private void MeasureAndLockHeight()
    {
        if (animator != null && !string.IsNullOrEmpty(showTriggerName))
        {
            animator.ResetTrigger(showTriggerName);
        }

        if (contentSizeFitter != null)
        {
            contentSizeFitter.enabled = true;
        }

        Vector2 size = rectTransform.sizeDelta;
        size.x = finalSizeDeltaX;
        rectTransform.sizeDelta = size;

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);

        float height = rectTransform.rect.height;
        if (height <= 0.01f)
        {
            height = LayoutUtility.GetPreferredHeight(rectTransform);
        }

        lockedHeight = height;
        heightLocked = true;

        layoutElement.minHeight = lockedHeight;
        layoutElement.preferredHeight = lockedHeight;

        if (contentSizeFitter != null)
        {
            contentSizeFitter.enabled = false;
        }

        size.x = startSizeDeltaX;
        size.y = lockedHeight;
        rectTransform.sizeDelta = size;
    }

    private void ApplyLockedHeight()
    {
        Vector2 size = rectTransform.sizeDelta;
        if (!Mathf.Approximately(size.y, lockedHeight))
        {
            size.y = lockedHeight;
            rectTransform.sizeDelta = size;
        }
    }

    private void UnlockHeight()
    {
        heightLocked = false;
        lockedHeight = 0f;

        if (layoutElement != null)
        {
            layoutElement.minHeight = -1f;
            layoutElement.preferredHeight = -1f;
        }

        if (contentSizeFitter != null)
        {
            contentSizeFitter.enabled = true;
        }
    }
}
