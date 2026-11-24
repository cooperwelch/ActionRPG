using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SeaSerpentBossFightDirector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SeaSerpentBossController boss;
    [SerializeField] private HUDHealthMeter bossHealthBarUI;

    [Header("Intro Movement")]
    [SerializeField] private Transform bossStartPosition;
    [SerializeField] private float introDuration = 1.5f;
    [SerializeField] private AnimationCurve easeOutCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Optional Timing")]
    [SerializeField] private float delayBeforeStart = 0.5f;
    [SerializeField] private float delayAfterLanding = 2.5f;

    private bool hasStarted;

    private void OnEnable()
    {
        if (boss != null && boss.Health != null)
        {
            boss.Health.OnHealthChanged += HandleBossHealthChanged;
        }
    }

    private void OnDisable()
    {
        if (boss != null && boss.Health != null)
        {
            boss.Health.OnHealthChanged -= HandleBossHealthChanged;
        }
    }

    private void HandleBossHealthChanged(int current, int max)
    {
        if (bossHealthBarUI != null)
        {
            bossHealthBarUI.SetHealth(current);
        }
    }

    public void StartFight()
    {
        if (hasStarted) return;

        hasStarted = true;
        StartCoroutine(IntroSequence());
    }

    private IEnumerator IntroSequence()
    {
        if (delayBeforeStart > 0f)
            yield return new WaitForSeconds(delayBeforeStart);

        yield return MoveBossToStart();

        boss.Motor.SetBasePosition(boss.transform.position);

        bossHealthBarUI.FillMeter();

        if (delayAfterLanding > 0f)
            yield return new WaitForSeconds(delayAfterLanding);

        boss.StartBoss();
    }

    private IEnumerator MoveBossToStart()
    {
        Vector3 start = boss.transform.position;
        Vector3 target = bossStartPosition.position;

        float time = 0f;

        while (time < introDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / introDuration);

            float easedT = easeOutCurve.Evaluate(t);

            boss.transform.position = Vector3.Lerp(start, target, easedT);

            yield return null;
        }

        boss.transform.position = target;
    }
}
