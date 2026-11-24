using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SeaSerpentBossAnimatorBridge : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer head;
    [SerializeField] private SeaSerpentBossBiteHitbox biteHitbox;

    private bool biteFinished;
    private int sortingOrder;

    private void Awake()
    {
        sortingOrder = head.sortingOrder;
        if (biteHitbox == null) biteHitbox = GetComponentInChildren<SeaSerpentBossBiteHitbox>(true);
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
