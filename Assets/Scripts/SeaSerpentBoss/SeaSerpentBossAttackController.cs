using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SeaSerpentBossAttackController : MonoBehaviour
{
    [SerializeField] private SeaSerpentBossController boss;
    [SerializeField] private float biteRangeOffsetX = 1.5f;

    private Coroutine activeAttackRoutine;
    private bool attackRunning;

    public bool IsAttackRunning => attackRunning;

    private void Awake()
    {
        if (boss == null) boss = GetComponent<SeaSerpentBossController>();
    }

    public void StartBiteAttack(Action onComplete)
    {
        if (attackRunning) return;

        activeAttackRoutine = StartCoroutine(BiteRoutine(onComplete));
    }

    public void CancelAttack()
    {
        if (activeAttackRoutine != null)
        {
            StopCoroutine(activeAttackRoutine);
            activeAttackRoutine = null;
        }

        attackRunning = false;
        boss.AnimatorBridge.SetBiteHitboxActive(false);
    }

    private IEnumerator BiteRoutine(Action onComplete)
    {
        attackRunning = true;

        Vector3 bitePosition = GetBitePosition();

        boss.Motor.MoveTo(bitePosition);

        while (!boss.Motor.IsAtTarget())
        {
            yield return null;
        }

        yield return new WaitForSeconds(1.0f);

        boss.AnimatorBridge.PlayBite();

        while (!boss.AnimatorBridge.IsBiteFinished())
        {
            yield return null;
        }

        attackRunning = false;
        activeAttackRoutine = null;
        onComplete?.Invoke();
    }

    private Vector3 GetBitePosition()
    {
        Transform player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (player == null)
        {
            return boss.transform.position;
        }

        Vector3 playerPos = player.position;

        //float offset = playerPos.x < boss.transform.position.x
        //    ? -biteRangeOffsetX
        //    : biteRangeOffsetX;

        return new Vector3(
            playerPos.x,
            boss.transform.position.y,
            boss.transform.position.z
        );
    }
}
