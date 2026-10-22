using UnityEngine;

/// <summary>
/// Environmental-hazard antlion: head lunges within a world-space arc using
/// telescoping body segments. Immortal for now; only the head damages the player.
/// Expected hierarchy: stationary root with sibling children Head (this component) and Ground.
/// Body segments are spawned as siblings under that root so they are not parented to the head.
/// </summary>
public class SubzoneAntlion : SubzoneEnemy
{
    public enum AntlionState
    {
        Idle,
        Telegraphing,
        Lunging,
        Paused,
        Retreating,
        Cooldown
    }

    [Header("References")]
    [SerializeField] private Transform player;

    [Header("Attack Arc")]
    [Tooltip("World-space degrees. 0 = right, 90 = up. Supports wrapping (e.g. 315 to 45).")]
    [SerializeField] private float minAngleDegrees = 45f;
    [SerializeField] private float maxAngleDegrees = 135f;
    [SerializeField] private float maxReach = 10f;
    [Tooltip("How far the lunge curve bulges from the straight chord, along the world-upward perpendicular.")]
    [SerializeField] private float arcHeight = 1.5f;

    [Header("Timing")]
    [SerializeField] private float lungeDuration = 0.35f;
    [SerializeField] private float pauseDuration = 0.25f;
    [SerializeField] private float retreatDuration = 0.45f;
    [SerializeField] private float cooldownDuration = 1.5f;
    [Tooltip("If > 0 and the Animator has no controller, telegraph advances after this many seconds.")]
    [SerializeField] private float telegraphFallbackDuration = 0.4f;

    [Header("Body Segments")]
    [SerializeField] private int bodySegmentCount = 4;
    [SerializeField] private Sprite bodySegmentSprite;
    [SerializeField] private Color bodySegmentColor = new Color(0.55f, 0.35f, 0.2f, 1f);
    [SerializeField] private Vector2 bodySegmentScale = new Vector2(0.45f, 0.45f);
    [SerializeField] private int bodySortingOrderOffset = -1;

    [Header("Head Rotation")]
    [Tooltip("Added to Atan2 when aiming the head. Use -90 if the sprite faces up by default.")]
    [SerializeField] private float spriteFacingOffsetDegrees = -90f;

    private AntlionState _state = AntlionState.Idle;
    private Vector3 _restPosition;
    private float _restRotationZ;
    private Vector2 _curveP0;
    private Vector2 _curveP1;
    private Vector2 _curveP2;
    private float _phaseTimer;
    private float _headT;
    private float _rotationBlend;
    private float[] _segmentTs;
    private Transform[] _segmentTransforms;
    private SpriteRenderer[] _segmentRenderers;
    private static Sprite _fallbackSegmentSprite;

    public AntlionState State => _state;

    public override void Awake()
    {
        base.Awake();

        CacheRestPose();

        rigidBody.bodyType = RigidbodyType2D.Kinematic;
        rigidBody.gravityScale = 0f;
        rigidBody.velocity = Vector2.zero;

        CreateBodySegments();
        SetBodySegmentsVisible(false);
    }

    private void LateUpdate()
    {
        if (_isDying)
            return;

        // Apply after Animator so body segments stay independent of the head transform.
        if (_state == AntlionState.Lunging
            || _state == AntlionState.Paused
            || _state == AntlionState.Retreating)
        {
            ApplyBodySegmentTransforms();
        }
    }

    public override void Update()
    {
        base.Update();

        if (_isDying)
            return;

        switch (_state)
        {
            case AntlionState.Idle:
                if (IsPlayerInAttackZone())
                    BeginTelegraph();
                break;

            case AntlionState.Telegraphing:
                if (_animator.runtimeAnimatorController == null && telegraphFallbackDuration > 0f)
                {
                    _phaseTimer += Time.deltaTime;
                    if (_phaseTimer >= telegraphFallbackDuration)
                        TelegraphAnimationComplete();
                }
                break;

            case AntlionState.Lunging:
                UpdateLunge();
                break;

            case AntlionState.Paused:
                _phaseTimer += Time.deltaTime;
                _rotationBlend = 1f;
                if (_phaseTimer >= pauseDuration)
                    BeginRetreat();
                break;

            case AntlionState.Retreating:
                UpdateRetreat();
                break;

            case AntlionState.Cooldown:
                _phaseTimer += Time.deltaTime;
                if (_phaseTimer >= cooldownDuration)
                    _state = AntlionState.Idle;
                break;
        }
    }

    /// <summary>
    /// Animation event: add to the end of the telegraph clip.
    /// </summary>
    public void TelegraphAnimationComplete()
    {
        if (_state != AntlionState.Telegraphing)
            return;

        if (_animator != null)
            _animator.SetBool("IsTelegraphing", false);

        BeginLunge();
    }

    public override void Damage(int damage, float damageDirection)
    {
        // Environmental hazard — currently immortal.
    }

    protected override bool CanBeStunned() => false;

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        maxReach = Mathf.Max(0.01f, maxReach);
        arcHeight = Mathf.Max(0f, arcHeight);
        bodySegmentCount = Mathf.Max(0, bodySegmentCount);
        lungeDuration = Mathf.Max(0.01f, lungeDuration);
        retreatDuration = Mathf.Max(0.01f, retreatDuration);
        pauseDuration = Mathf.Max(0f, pauseDuration);
        cooldownDuration = Mathf.Max(0f, cooldownDuration);
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 origin = Application.isPlaying ? _restPosition : transform.position;
        float reach = Mathf.Max(0.01f, maxReach);

        Gizmos.color = new Color(1f, 0.4f, 0.1f, 0.35f);
        DrawArcGizmo(origin, minAngleDegrees, maxAngleDegrees, reach);

        Gizmos.color = new Color(1f, 0.4f, 0.1f, 0.9f);
        Vector3 minDir = DirFromDegrees(minAngleDegrees);
        Vector3 maxDir = DirFromDegrees(maxAngleDegrees);
        Gizmos.DrawLine(origin, origin + minDir * reach);
        Gizmos.DrawLine(origin, origin + maxDir * reach);
    }
#endif

    private void BeginTelegraph()
    {
        _state = AntlionState.Telegraphing;
        _phaseTimer = 0f;

        if (_animator != null && _animator.runtimeAnimatorController != null)
            _animator.SetBool("IsTelegraphing", true);
    }

    private void BeginLunge()
    {
        // Capture rest pose at lunge start (after telegraph), not only at Awake.
        CacheRestPose();

        Vector2 origin2 = _restPosition;
        Vector2 target;
        if (player == null)
        {
            target = origin2 + DirFromDegrees(minAngleDegrees) * maxReach;
        }
        else
        {
            Vector2 toPlayer = (Vector2)player.position - origin2;
            float distance = toPlayer.magnitude;

            if (distance < 0.001f)
                target = origin2;
            else
                target = origin2 + toPlayer.normalized * Mathf.Min(distance, maxReach);
        }

        _curveP0 = origin2;
        _curveP2 = target;
        _curveP1 = ComputeArcControlPoint(_curveP0, _curveP2, arcHeight);

        _headT = 0f;
        _rotationBlend = 0f;
        ApplyEvenSegmentSpacing();

        SetBodySegmentsVisible(bodySegmentCount > 0);
        _phaseTimer = 0f;
        _state = AntlionState.Lunging;
    }

    private void UpdateLunge()
    {
        _phaseTimer += Time.deltaTime;
        float t = Mathf.Clamp01(_phaseTimer / lungeDuration);
        float eased = CubicEaseIn(t);

        _headT = eased;
        _rotationBlend = eased;
        ApplyEvenSegmentSpacing();
        ApplyHeadTransform();

        if (t >= 1f)
        {
            _headT = 1f;
            _rotationBlend = 1f;
            ApplyEvenSegmentSpacing();
            ApplyHeadTransform();
            _phaseTimer = 0f;
            _state = AntlionState.Paused;
        }
    }

    private void BeginRetreat()
    {
        _phaseTimer = 0f;
        _state = AntlionState.Retreating;
    }

    private void UpdateRetreat()
    {
        _phaseTimer += Time.deltaTime;
        float t = Mathf.Clamp01(_phaseTimer / retreatDuration);
        float eased = CubicEaseOut(t);

        _headT = 1f - eased;
        _rotationBlend = 1f - eased;
        ApplyEvenSegmentSpacing();
        ApplyHeadTransform();

        if (t >= 1f)
        {
            _headT = 0f;
            _rotationBlend = 0f;
            ApplyEvenSegmentSpacing();

            transform.SetPositionAndRotation(
                _restPosition,
                Quaternion.Euler(0f, 0f, _restRotationZ)
            );
            SetBodySegmentsVisible(false);

            _phaseTimer = 0f;
            _state = AntlionState.Cooldown;
        }
    }

    private void CacheRestPose()
    {
        _restPosition = transform.position;
        _restRotationZ = transform.eulerAngles.z;
    }

    /// <summary>
    /// Evenly subdivide the curve from the head (t = headT) down to the locked base (t = 0).
    /// </summary>
    private void ApplyEvenSegmentSpacing()
    {
        if (_segmentTs == null || _segmentTs.Length == 0)
            return;

        int n = _segmentTs.Length;
        for (int i = 0; i < n - 1; i++)
            _segmentTs[i] = _headT * (n - 1 - i) / n;

        _segmentTs[n - 1] = 0f;
    }

    private void ApplyHeadTransform()
    {
        Vector2 headPos = QuadraticBezier(_curveP0, _curveP1, _curveP2, _headT);
        Vector2 headTangent = QuadraticBezierTangent(_curveP0, _curveP1, _curveP2, _headT);
        float attackRotationZ = Mathf.Atan2(headTangent.y, headTangent.x) * Mathf.Rad2Deg
            + spriteFacingOffsetDegrees;
        float z = Mathf.LerpAngle(_restRotationZ, attackRotationZ, Mathf.Clamp01(_rotationBlend));

        transform.SetPositionAndRotation(
            new Vector3(headPos.x, headPos.y, _restPosition.z),
            Quaternion.Euler(0f, 0f, z)
        );
    }

    /// <summary>
    /// Body segments are hierarchy siblings of the head. Positions are written in world space
    /// from the attack curve; the last segment is always locked to the rest pose.
    /// </summary>
    private void ApplyBodySegmentTransforms()
    {
        if (_segmentTransforms == null || _segmentTransforms.Length == 0)
            return;

        int last = _segmentTransforms.Length - 1;
        for (int i = 0; i < last; i++)
        {
            Transform seg = _segmentTransforms[i];
            if (seg == null)
                continue;

            // Safety: never allow a segment to remain parented under the head.
            if (seg.IsChildOf(transform))
                seg.SetParent(transform.parent, true);

            float segT = _segmentTs[i];
            Vector2 pos = QuadraticBezier(_curveP0, _curveP1, _curveP2, segT);
            Vector2 tangent = QuadraticBezierTangent(_curveP0, _curveP1, _curveP2, segT);
            float segRotZ = Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg
                + spriteFacingOffsetDegrees;

            seg.SetPositionAndRotation(
                new Vector3(pos.x, pos.y, _restPosition.z),
                Quaternion.Euler(
                    0f,
                    0f,
                    Mathf.LerpAngle(_restRotationZ, segRotZ, Mathf.Clamp01(_rotationBlend))
                )
            );
        }

        Transform anchor = _segmentTransforms[last];
        if (anchor == null)
            return;

        if (anchor.IsChildOf(transform))
            anchor.SetParent(transform.parent, true);

        anchor.SetPositionAndRotation(
            _restPosition,
            Quaternion.Euler(0f, 0f, _restRotationZ)
        );
    }

    /// <summary>
    /// Control point at the chord midpoint, offset along the perpendicular that points more world-up.
    /// </summary>
    private static Vector2 ComputeArcControlPoint(Vector2 start, Vector2 end, float height)
    {
        Vector2 chord = end - start;
        Vector2 mid = start + chord * 0.5f;

        if (height <= 0f || chord.sqrMagnitude < 0.0001f)
            return mid;

        Vector2 perp = new Vector2(-chord.y, chord.x);
        if (Vector2.Dot(perp, Vector2.up) < 0f)
            perp = -perp;

        // Near-vertical lunges: both perpendiculars are horizontal — prefer +X for stability.
        if (Mathf.Abs(Vector2.Dot(perp.normalized, Vector2.up)) < 0.01f && perp.x < 0f)
            perp = -perp;

        return mid + perp.normalized * height;
    }

    private static Vector2 QuadraticBezier(Vector2 p0, Vector2 p1, Vector2 p2, float t)
    {
        float u = 1f - t;
        return u * u * p0 + 2f * u * t * p1 + t * t * p2;
    }

    private static Vector2 QuadraticBezierTangent(Vector2 p0, Vector2 p1, Vector2 p2, float t)
    {
        Vector2 tangent = 2f * (1f - t) * (p1 - p0) + 2f * t * (p2 - p1);
        if (tangent.sqrMagnitude < 0.0001f)
            return (p2 - p0).sqrMagnitude > 0.0001f ? (p2 - p0).normalized : Vector2.up;
        return tangent.normalized;
    }

    private bool IsPlayerInAttackZone()
    {
        if (player == null)
            return false;

        Vector2 toPlayer = (Vector2)player.position - (Vector2)_restPosition;
        float distance = toPlayer.magnitude;
        if (distance > maxReach || distance < 0.001f)
            return false;

        float angle = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg;
        return IsAngleInRange(angle, minAngleDegrees, maxAngleDegrees);
    }

    private static bool IsAngleInRange(float angleDegrees, float minDegrees, float maxDegrees)
    {
        float angle = NormalizeAngle360(angleDegrees);
        float min = NormalizeAngle360(minDegrees);
        float max = NormalizeAngle360(maxDegrees);

        if (min <= max)
            return angle >= min && angle <= max;

        // Wrapping range (e.g. 315 → 45).
        return angle >= min || angle <= max;
    }

    private static float NormalizeAngle360(float degrees)
    {
        degrees %= 360f;
        if (degrees < 0f)
            degrees += 360f;
        return degrees;
    }

    private static Vector2 DirFromDegrees(float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
    }

    private void CreateBodySegments()
    {
        if (bodySegmentCount <= 0)
        {
            _segmentTs = System.Array.Empty<float>();
            _segmentTransforms = System.Array.Empty<Transform>();
            _segmentRenderers = System.Array.Empty<SpriteRenderer>();
            return;
        }

        _segmentTs = new float[bodySegmentCount];
        _segmentTransforms = new Transform[bodySegmentCount];
        _segmentRenderers = new SpriteRenderer[bodySegmentCount];

        Sprite sprite = bodySegmentSprite != null ? bodySegmentSprite : GetFallbackSegmentSprite();
        string sortingLayer = spriteRenderer != null ? spriteRenderer.sortingLayerName : "Default";
        int headSorting = spriteRenderer != null ? spriteRenderer.sortingOrder : 0;
        // Same hierarchy level as the head — never parented under the head transform.
        Transform hierarchyParent = transform.parent;

        for (int i = 0; i < bodySegmentCount; i++)
        {
            var go = new GameObject($"BodySegment_{i}");
            go.transform.SetParent(hierarchyParent, false);
            go.transform.SetPositionAndRotation(
                _restPosition,
                Quaternion.Euler(0f, 0f, _restRotationZ)
            );
            go.transform.localScale = new Vector3(bodySegmentScale.x, bodySegmentScale.y, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = bodySegmentColor;
            sr.sortingLayerName = sortingLayer;
            // Descending order toward the origin so the locked base renders behind the chain.
            sr.sortingOrder = headSorting + bodySortingOrderOffset - i;

            _segmentTransforms[i] = go.transform;
            _segmentRenderers[i] = sr;
            _segmentTs[i] = 0f;
        }
    }

    private void SetBodySegmentsVisible(bool visible)
    {
        if (_segmentRenderers == null)
            return;

        for (int i = 0; i < _segmentRenderers.Length; i++)
        {
            if (_segmentRenderers[i] != null)
                _segmentRenderers[i].enabled = visible;
        }
    }

    private static Sprite GetFallbackSegmentSprite()
    {
        if (_fallbackSegmentSprite != null)
            return _fallbackSegmentSprite;

        var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        var pixels = new Color[16];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.white;
        tex.SetPixels(pixels);
        tex.Apply();
        tex.filterMode = FilterMode.Point;

        _fallbackSegmentSprite = Sprite.Create(
            tex,
            new Rect(0f, 0f, 4f, 4f),
            new Vector2(0.5f, 0.5f),
            4f
        );
        return _fallbackSegmentSprite;
    }

    private static float CubicEaseIn(float t)
    {
        return t * t * t;
    }

    private static float CubicEaseOut(float t)
    {
        float u = 1f - t;
        return 1f - u * u * u;
    }

#if UNITY_EDITOR
    private static void DrawArcGizmo(Vector3 origin, float minDeg, float maxDeg, float radius)
    {
        const int segments = 48;
        float min = NormalizeAngle360(minDeg);
        float max = NormalizeAngle360(maxDeg);

        float sweep;
        if (min <= max)
            sweep = max - min;
        else
            sweep = (360f - min) + max;

        Vector3 prev = origin + (Vector3)DirFromDegrees(min) * radius;
        for (int i = 1; i <= segments; i++)
        {
            float angle = min + sweep * (i / (float)segments);
            Vector3 next = origin + (Vector3)DirFromDegrees(angle) * radius;
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }
#endif

    private void OnDestroy()
    {
        if (_segmentTransforms == null)
            return;

        for (int i = 0; i < _segmentTransforms.Length; i++)
        {
            if (_segmentTransforms[i] != null)
                Destroy(_segmentTransforms[i].gameObject);
        }
    }
}
