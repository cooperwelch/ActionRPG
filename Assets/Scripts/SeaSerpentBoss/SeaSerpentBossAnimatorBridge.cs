using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SeaSerpentBossAnimatorBridge : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer head;
    [SerializeField] private SeaSerpentBossBiteHitbox biteHitbox;
    [Tooltip("Mouth position for the side-facing spit sprites, placed for facing RIGHT. Mirrored when facing left.")]
    [SerializeField] private Transform spitOrigin;

    private bool biteFinished;
    private bool spitFired;
    private bool spitFinished;
    private int sortingOrder;
    private Vector3 spitOriginLocal;

    public Vector3 SpitOriginPosition => spitOrigin != null ? spitOrigin.position : transform.position;

    private void Awake()
    {
        sortingOrder = head.sortingOrder;
        if (biteHitbox == null) biteHitbox = GetComponentInChildren<SeaSerpentBossBiteHitbox>(true);
        if (spitOrigin != null) spitOriginLocal = spitOrigin.localPosition;
    }

    public void PlaySpit(bool faceLeft)
    {
        spitFired = false;
        spitFinished = false;

        // Side sprites face right; flip to face left.
        head.flipX = faceLeft;
        if (spitOrigin != null)
        {
            float x = Mathf.Abs(spitOriginLocal.x);
            spitOrigin.localPosition = new Vector3(faceLeft ? -x : x, spitOriginLocal.y, spitOriginLocal.z);
        }

        animator.SetTrigger("Spit");
    }

    public bool HasSpitFired()
    {
        return spitFired;
    }

    public bool IsSpitFinished()
    {
        return spitFinished;
    }

    // Animation event (seaserpent_spit): mouth fully open.
    public void OnSpitFire()
    {
        spitFired = true;
    }

    // Animation event (seaserpent_spit): last frame, about to return to the front idle.
    public void OnSpitEnd()
    {
        head.flipX = false;
        spitFinished = true;
    }

    public void PlayBite()
    {
        biteFinished = false;
        head.sortingOrder = 12;
        animator.SetTrigger("Bite");
    }

    public bool IsBiteFinished()
    {
        return biteFinished;
    }

    // Animation event (seaserpent_bite): head starts lunging down.
    public void BiteHitboxOn()
    {
        SetBiteHitboxActive(true);
    }

    // Animation event (seaserpent_bite): head starts pulling back up.
    public void BiteHitboxOff()
    {
        SetBiteHitboxActive(false);
    }

    public void SetBiteHitboxActive(bool active)
    {
        if (biteHitbox != null) biteHitbox.SetActive(active);
    }

    public void OnBiteEnd()
    {
        SetBiteHitboxActive(false);
        StartCoroutine(Complete());
    }

    private IEnumerator Complete()
    {
        yield return new WaitForSeconds(0.5f);
        head.sortingOrder = sortingOrder;
        biteFinished = true;
    }
}
