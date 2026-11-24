using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SeaSerpentBossAttackController : MonoBehaviour
{
    [SerializeField] private SeaSerpentBossController boss;
    [SerializeField] private float biteRangeOffsetX = 1.5f;

    [Header("Acid Spit")]
    [SerializeField] private SeaSerpentAcidProjectile acidProjectilePrefab;
    [SerializeField] private Transform spitLeftAnchor;
    [SerializeField] private Transform spitRightAnchor;
    [Tooltip("The ship's deck collider. Projectiles land on its top edge, in the ship's local space so they follow the bob.")]
    [SerializeField] private BoxCollider2D shipFloor;
    [Tooltip("Raise the landing point above the deck (e.g. if the ship's rail hides the fire).")]
    [SerializeField] private float landingHeightOffset = 0f;
    [Tooltip("Keep landing spots this far in from each end of the deck collider.")]
    [SerializeField] private float deckEdgeMargin = 2.5f;
    [SerializeField] private int projectileCount = 3;
    [SerializeField] private float projectileFlightTime = 1.1f;
    [SerializeField] private float projectileGravity = 20f;
    [SerializeField] private float delayBetweenProjectiles = 0.15f;
    [SerializeField] private float spitWindupDelay = 0.4f;

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

    public void StartSpitAttack(Action onComplete)
    {
        if (attackRunning) return;

        activeAttackRoutine = StartCoroutine(SpitRoutine(onComplete));
    }

    private IEnumerator SpitRoutine(Action onComplete)
    {
        attackRunning = true;

        bool fromLeft = UnityEngine.Random.value < 0.5f;
        Transform anchor = fromLeft ? spitLeftAnchor : spitRightAnchor;

        boss.Motor.MoveTo(anchor.position);

        while (!boss.Motor.IsAtTarget())
        {
            yield return null;
        }

        yield return new WaitForSeconds(spitWindupDelay);

        // Face the ship: from the left side look right, from the right side look left.
        boss.AnimatorBridge.PlaySpit(faceLeft: !fromLeft);

        while (!boss.AnimatorBridge.HasSpitFired())
        {
            yield return null;
        }

        List<Vector2> targets = GetDeckTargets();
        // Spit the nearest spot first so the volley sweeps away from the serpent.
        if (!fromLeft) targets.Reverse();

        for (int i = 0; i < targets.Count; i++)
        {
            SpawnAcid(targets[i]);
            if (i < targets.Count - 1 && delayBetweenProjectiles > 0f)
            {
                yield return new WaitForSeconds(delayBetweenProjectiles);
            }
        }

        while (!boss.AnimatorBridge.IsSpitFinished())
        {
            yield return null;
        }

        attackRunning = false;
        activeAttackRoutine = null;
        onComplete?.Invoke();
    }

    // Evenly spaced landing points across the deck, in the ship's local space (left to right).
    private List<Vector2> GetDeckTargets()
    {
        var targets = new List<Vector2>(projectileCount);

        Vector2 center = shipFloor.offset;
        Vector2 half = shipFloor.size * 0.5f;
        float left = center.x - half.x + deckEdgeMargin;
        float right = center.x + half.x - deckEdgeMargin;
        float top = center.y + half.y + landingHeightOffset;

        for (int i = 0; i < projectileCount; i++)
        {
            float t = projectileCount > 1 ? (float)i / (projectileCount - 1) : 0.5f;
            targets.Add(new Vector2(Mathf.Lerp(left, right, t), top));
        }

        return targets;
    }

    private void SpawnAcid(Vector2 localTarget)
    {
        Vector3 origin = boss.AnimatorBridge.SpitOriginPosition;
        SeaSerpentAcidProjectile acid = Instantiate(acidProjectilePrefab, origin, Quaternion.identity);
        acid.Launch(origin, shipFloor.transform, localTarget, projectileFlightTime, projectileGravity);
    }

    private void OnDrawGizmosSelected()
    {
        if (shipFloor == null) return;

        Gizmos.color = Color.green;
        foreach (Vector2 local in GetDeckTargets())
        {
            Gizmos.DrawWireSphere(shipFloor.transform.TransformPoint(local), 0.3f);
        }
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
