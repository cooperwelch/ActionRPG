using System.Collections;
using UnityEngine;

public class UIListNavigator
{
    public int Index { get; private set; }

    private readonly MonoBehaviour host;
    private readonly float axisInputDelayDuration;
    private bool acceptingAxisInputUp = true;
    private bool acceptingAxisInputDown = true;
    private bool acceptingAxisInputLeft = true;
    private bool acceptingAxisInputRight = true;
    private Coroutine delayDownCoroutine;
    private Coroutine delayUpCoroutine;
    private Coroutine delayLeftCoroutine;
    private Coroutine delayRightCoroutine;

    public UIListNavigator(MonoBehaviour host, float axisInputDelayDuration = 0.25f)
    {
        this.host = host;
        this.axisInputDelayDuration = axisInputDelayDuration;
    }

    public void Prepare(int startIndex, bool ignoreHeldInput = true)
    {
        Index = startIndex;
        if (ignoreHeldInput)
        {
            acceptingAxisInputUp = false;
            acceptingAxisInputDown = false;
            acceptingAxisInputLeft = false;
            acceptingAxisInputRight = false;
        }
        else
        {
            acceptingAxisInputUp = true;
            acceptingAxisInputDown = true;
            acceptingAxisInputLeft = true;
            acceptingAxisInputRight = true;
        }
    }

    public void SetIndex(int index)
    {
        Index = index;
    }

    public bool TickVertical(int count)
    {
        if (count <= 0)
        {
            ResetHeldWhenReleased();
            return false;
        }

        float inputVertical = Input.GetAxisRaw("Vertical") + Input.GetAxisRaw("DPadY");
        bool changed = false;

        if (inputVertical < 0 && acceptingAxisInputDown)
        {
            acceptingAxisInputDown = false;
            acceptingAxisInputUp = true;
            RestartDelay(ref delayDownCoroutine, DelayAxisInputDown());
            Index = (Index + 1) % count;
            changed = true;
        }
        else if (inputVertical > 0 && acceptingAxisInputUp)
        {
            acceptingAxisInputUp = false;
            acceptingAxisInputDown = true;
            RestartDelay(ref delayUpCoroutine, DelayAxisInputUp());
            Index = (Index - 1 + count) % count;
            changed = true;
        }

        if (inputVertical == 0)
        {
            acceptingAxisInputDown = true;
            acceptingAxisInputUp = true;
        }

        return changed;
    }

    public bool TickHorizontal(int count)
    {
        if (count <= 0)
        {
            ResetHeldWhenReleased();
            return false;
        }

        float inputHorizontal = Input.GetAxisRaw("Horizontal") + Input.GetAxisRaw("DPadX");
        bool changed = false;

        if (inputHorizontal > 0 && acceptingAxisInputRight)
        {
            acceptingAxisInputRight = false;
            acceptingAxisInputLeft = true;
            RestartDelay(ref delayRightCoroutine, DelayAxisInputRight());
            Index = (Index + 1) % count;
            changed = true;
        }
        else if (inputHorizontal < 0 && acceptingAxisInputLeft)
        {
            acceptingAxisInputLeft = false;
            acceptingAxisInputRight = true;
            RestartDelay(ref delayLeftCoroutine, DelayAxisInputLeft());
            Index = (Index - 1 + count) % count;
            changed = true;
        }

        if (inputHorizontal == 0)
        {
            acceptingAxisInputLeft = true;
            acceptingAxisInputRight = true;
        }

        return changed;
    }

    private void ResetHeldWhenReleased()
    {
        float inputVertical = Input.GetAxisRaw("Vertical") + Input.GetAxisRaw("DPadY");
        float inputHorizontal = Input.GetAxisRaw("Horizontal") + Input.GetAxisRaw("DPadX");
        if (inputVertical == 0)
        {
            acceptingAxisInputUp = true;
            acceptingAxisInputDown = true;
        }
        if (inputHorizontal == 0)
        {
            acceptingAxisInputLeft = true;
            acceptingAxisInputRight = true;
        }
    }

    private void RestartDelay(ref Coroutine coroutine, IEnumerator routine)
    {
        if (coroutine != null)
        {
            host.StopCoroutine(coroutine);
        }

        coroutine = host.StartCoroutine(routine);
    }

    private IEnumerator DelayAxisInputDown()
    {
        yield return new WaitForSeconds(axisInputDelayDuration);
        acceptingAxisInputDown = true;
        delayDownCoroutine = null;
    }

    private IEnumerator DelayAxisInputUp()
    {
        yield return new WaitForSeconds(axisInputDelayDuration);
        acceptingAxisInputUp = true;
        delayUpCoroutine = null;
    }

    private IEnumerator DelayAxisInputLeft()
    {
        yield return new WaitForSeconds(axisInputDelayDuration);
        acceptingAxisInputLeft = true;
        delayLeftCoroutine = null;
    }

    private IEnumerator DelayAxisInputRight()
    {
        yield return new WaitForSeconds(axisInputDelayDuration);
        acceptingAxisInputRight = true;
        delayRightCoroutine = null;
    }
}
