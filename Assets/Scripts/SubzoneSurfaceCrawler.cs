using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Immortal obstacle enemy that raycast-clings to a nearby Ground collider
/// (Box or Composite) and loops forever around its outer perimeter (CW or CCW).
/// For composites with disconnected islands, locks onto the nearest path so
/// cling rays cannot jump to a different island on the same tilemap.
/// </summary>
public class SubzoneSurfaceCrawler : SubzoneEnemy
{
    public enum CrawlDirection
    {
        Clockwise,
        CounterClockwise
    }

    [Header("Crawling")]
    [SerializeField] private CrawlDirection crawlDirection = CrawlDirection.Clockwise;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float surfaceSearchRadius = 2f;
    [SerializeField] private float surfaceOffset = 0.5f;
    [SerializeField] private float skinWidth = 0.02f;
    [SerializeField] private float clingRayLength = 1f;
    [Tooltip("Optional explicit surface. When set, skips nearby discovery.")]
    [SerializeField] private Collider2D surfaceOverride;
    [Tooltip("Max distance from the locked composite path for a ray hit to count.")]
    [SerializeField] private float pathLockTolerance = 0.25f;

    [Header("Visuals")]
    [Tooltip("When enabled, rotates so the sprite's feet stay on the surface. Off by default.")]
    [SerializeField] private bool rotateSpriteToSurface = false;
    [Tooltip("Added to the surface-facing rotation. Use -90 if the sprite faces up by default.")]
    [SerializeField] private float spriteFacingOffsetDegrees = -90f;

    private enum OuterCornerPhase
    {
        None,
        /// <summary>Slide past the geometric edge along the old face lane by surfaceOffset.</summary>
        Overhang,
        /// <summary>After an outer or inner turn, slide along the new lane until cling succeeds.</summary>
        SeekNewFace
    }

    private Collider2D _surfaceCollider;
    private Collider2D _hitCollider;
    private Vector2 _intoSurface;
    private Vector2 _alongSurface;
    private bool _bound;
    private OuterCornerPhase _outerCornerPhase;
    private float _outerCornerOverhangRemaining;
    private readonly RaycastHit2D[] _rayHits = new RaycastHit2D[8];
    private readonly Collider2D[] _overlapHits = new Collider2D[8];
    private readonly List<Vector2> _pathScratch = new List<Vector2>(64);
    private readonly List<Vector2> _lockedPathWorld = new List<Vector2>(64);
    private bool _hasPathLock;

    private static readonly Vector2[] CardinalOutwards =
    {
        Vector2.up,
        Vector2.down,
        Vector2.left,
        Vector2.right
    };

    private static readonly int IsClockwiseHash = Animator.StringToHash("IsClockwise");

    public Collider2D BoundSurface => _surfaceCollider;
    public bool IsBound => _bound;

    public override void Awake()
    {
        base.Awake();

        rigidBody.bodyType = RigidbodyType2D.Kinematic;
        rigidBody.gravityScale = 0f;
        rigidBody.velocity = Vector2.zero;
        rigidBody.constraints = RigidbodyConstraints2D.FreezeRotation;

        _hitCollider = GetComponent<CapsuleCollider2D>();
        if (_hitCollider == null || !_hitCollider.enabled)
        {
            var box = GetComponent<BoxCollider2D>();
            if (box != null && box.enabled)
                _hitCollider = box;
        }

        if (groundLayer.value == 0)
            groundLayer = LayerMask.GetMask("Ground");

        ApplyCrawlDirectionAnimation();
    }

    private void Start()
    {
        ApplyCrawlDirectionAnimation();

        if (!TryBindNearestSurface())
        {
            Debug.LogWarning(
                $"{nameof(SubzoneSurfaceCrawler)} on '{name}' could not bind to a Ground Box/Composite collider within {surfaceSearchRadius}.",
                this
            );
            enabled = false;
        }
    }

    private void ApplyCrawlDirectionAnimation()
    {
        if (_animator == null || _animator.runtimeAnimatorController == null)
            return;

        _animator.SetBool(IsClockwiseHash, crawlDirection == CrawlDirection.Clockwise);
    }

    public override void Update()
    {
        base.Update();

        if (!_bound || _isDying)
            return;

        Crawl(moveSpeed * Time.deltaTime);
        ApplyVisualOrientation();
    }

    public override void Damage(int damage, float damageDirection)
    {
        // Environmental obstacle — immortal, same as antlion.
    }

    protected override bool CanBeStunned() => false;

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        moveSpeed = Mathf.Max(0f, moveSpeed);
        surfaceSearchRadius = Mathf.Max(0.05f, surfaceSearchRadius);
        surfaceOffset = Mathf.Max(0.01f, surfaceOffset);
        skinWidth = Mathf.Max(0.001f, skinWidth);
        clingRayLength = Mathf.Max(surfaceOffset + skinWidth, clingRayLength);
        pathLockTolerance = Mathf.Max(0.01f, pathLockTolerance);

        if (_animator == null)
            _animator = GetComponent<Animator>();
        ApplyCrawlDirectionAnimation();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.6f, 0.6f, 0.6f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, surfaceSearchRadius);

        if (_hasPathLock && _lockedPathWorld.Count > 1)
        {
            Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.9f);
            for (int i = 0; i < _lockedPathWorld.Count; i++)
            {
                Vector2 a = _lockedPathWorld[i];
                Vector2 b = _lockedPathWorld[(i + 1) % _lockedPathWorld.Count];
                Gizmos.DrawLine(a, b);
            }
        }

        if (!_bound)
            return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position, _alongSurface * 0.5f);
        Gizmos.color = Color.magenta;
        Gizmos.DrawRay(transform.position, _intoSurface * 0.5f);
    }
#endif

    private bool TryBindNearestSurface()
    {
        Collider2D best = surfaceOverride;
        if (best != null && !IsSupportedSurface(best))
        {
            Debug.LogWarning(
                $"{nameof(SubzoneSurfaceCrawler)} on '{name}' surfaceOverride must be a BoxCollider2D or CompositeCollider2D.",
                this
            );
            return false;
        }

        if (best == null)
        {
            Collider2D[] candidates = Physics2D.OverlapCircleAll(
                transform.position,
                surfaceSearchRadius,
                groundLayer
            );

            float bestDistSq = float.MaxValue;
            Vector2 origin = transform.position;

            for (int i = 0; i < candidates.Length; i++)
            {
                Collider2D candidate = candidates[i];
                if (candidate == null || candidate == _hitCollider)
                    continue;

                if (!IsSupportedSurface(candidate))
                    continue;

                Vector2 closest = candidate.ClosestPoint(origin);
                float distSq = (closest - origin).sqrMagnitude;
                if (distSq < bestDistSq)
                {
                    bestDistSq = distSq;
                    best = candidate;
                }
            }
        }

        if (best == null)
            return false;

        _surfaceCollider = best;

        if (_hitCollider != null)
            Physics2D.IgnoreCollision(_hitCollider, _surfaceCollider, true);

        if (_surfaceCollider is CompositeCollider2D composite)
        {
            if (!TryLockNearestPath(composite, transform.position))
                return false;
        }
        else
        {
            ClearPathLock();
        }

        if (!TrySnapToNearestFace())
            return false;

        _outerCornerPhase = OuterCornerPhase.None;
        _outerCornerOverhangRemaining = 0f;
        _bound = true;
        return true;
    }

    private static bool IsSupportedSurface(Collider2D collider)
    {
        return collider is BoxCollider2D || collider is CompositeCollider2D;
    }

    private bool TrySnapToNearestFace()
    {
        Vector2 surfacePoint = GetClosestSurfacePoint(transform.position);

        // Always cast from the exterior side so hit normals / placement stay outside.
        // Casting from inside a solid (or through a hollow outline) can yield inward
        // normals and park the crawler in the interior.
        return TrySnapFromOutside(surfacePoint);
    }

    private bool TrySnapFromOutside(Vector2 surfacePoint)
    {
        Vector2 estimatedOutward = EstimateExteriorNormal(surfacePoint);
        if (TrySnapWithOutward(surfacePoint, estimatedOutward))
            return true;

        for (int i = 0; i < CardinalOutwards.Length; i++)
        {
            Vector2 candidate = CardinalOutwards[i];
            if (!IsExteriorSide(surfacePoint, candidate))
                continue;

            if (TrySnapWithOutward(surfacePoint, candidate))
                return true;
        }

        // Last resort: try every cardinal (ApplySurfaceHit will still flip to exterior).
        for (int i = 0; i < CardinalOutwards.Length; i++)
        {
            if (TrySnapWithOutward(surfacePoint, CardinalOutwards[i]))
                return true;
        }

        return false;
    }

    private bool TrySnapWithOutward(Vector2 surfacePoint, Vector2 outward)
    {
        if (outward.sqrMagnitude < 0.0001f)
            return false;

        outward = EnsureExteriorNormal(surfacePoint, SnapToCardinal(outward.normalized));
        Vector2 origin = surfacePoint + outward * (surfaceOffset + skinWidth);

        // Origin must sit outside the solid; otherwise this is an interior cast.
        if (!IsOutsideSolid(origin))
            return false;

        RaycastHit2D hit = RaycastSurface(origin, -outward, surfaceOffset + clingRayLength);
        if (hit.collider == null)
            return false;

        ApplySurfaceHit(hit);
        return IsOutsideSolid(transform.position);
    }

    private Vector2 EstimateExteriorNormal(Vector2 surfacePoint)
    {
        Vector2 pos = transform.position;
        Vector2 fromSurface = pos - surfacePoint;
        if (fromSurface.sqrMagnitude > 0.0001f)
        {
            Vector2 fromPos = SnapToCardinal(fromSurface.normalized);
            if (IsExteriorSide(surfacePoint, fromPos))
                return fromPos;
        }

        if (_hasPathLock && TryGetNearestPathSegment(surfacePoint, out Vector2 a, out Vector2 b))
        {
            Vector2 tangent = b - a;
            if (tangent.sqrMagnitude > 0.0001f)
            {
                Vector2 n = SnapToCardinal(RotateCCW(tangent.normalized));
                if (IsExteriorSide(surfacePoint, n))
                    return n;

                n = -n;
                if (IsExteriorSide(surfacePoint, n))
                    return n;
            }
        }

        for (int i = 0; i < CardinalOutwards.Length; i++)
        {
            Vector2 n = CardinalOutwards[i];
            if (IsExteriorSide(surfacePoint, n))
                return n;
        }

        return Vector2.up;
    }

    private Vector2 EnsureExteriorNormal(Vector2 surfacePoint, Vector2 normal)
    {
        if (IsExteriorSide(surfacePoint, normal))
            return normal;

        Vector2 flipped = -normal;
        if (IsExteriorSide(surfacePoint, flipped))
            return flipped;

        return normal;
    }

    private bool IsExteriorSide(Vector2 surfacePoint, Vector2 outward)
    {
        return IsOutsideSolid(surfacePoint + outward.normalized * (skinWidth * 4f));
    }

    /// <summary>
    /// True when <paramref name="point"/> is outside the solid region of the bound surface.
    /// Uses path point-in-polygon for composites (works for hollow Outline geometry) and
    /// overlap tests for solid Box / Polygon composites.
    /// </summary>
    private bool IsOutsideSolid(Vector2 point)
    {
        if (_hasPathLock)
            return !IsPointInsideLockedPathPolygon(point);

        return !IsPointOverlappingSurface(point);
    }

    private void ApplySurfaceHit(RaycastHit2D hit)
    {
        Vector2 outward = EnsureExteriorNormal(hit.point, SnapToCardinal(hit.normal));
        _intoSurface = -outward;
        _alongSurface = PerpendicularForCrawl(_intoSurface);
        transform.position = hit.point + outward * surfaceOffset;
    }

    private void Crawl(float distance)
    {
        float remaining = distance;
        int iterations = 0;

        while (remaining > 0.0001f && iterations++ < 8)
        {
            // L-shaped outer corner: slide past the edge on the old lane, turn in place,
            // then slide along the new lane until cling works — never teleport.
            if (_outerCornerPhase == OuterCornerPhase.Overhang)
            {
                float step = Mathf.Min(remaining, _outerCornerOverhangRemaining);
                transform.position += (Vector3)(_alongSurface * step);
                remaining -= step;
                _outerCornerOverhangRemaining -= step;

                if (_outerCornerOverhangRemaining > 0.0001f)
                    continue;

                _outerCornerOverhangRemaining = 0f;
                RotateOuterCorner();
                _outerCornerPhase = OuterCornerPhase.SeekNewFace;

                if (StickToCurrentFace())
                    _outerCornerPhase = OuterCornerPhase.None;

                continue;
            }

            if (_outerCornerPhase == OuterCornerPhase.SeekNewFace)
            {
                if (StickToCurrentFace())
                {
                    _outerCornerPhase = OuterCornerPhase.None;
                    continue;
                }

                // Still clear of the new face (e.g. above the vertical face after a top→side
                // turn). Keep moving axis-aligned until cling succeeds.
                transform.position += (Vector3)(_alongSurface * remaining);
                remaining = 0f;

                if (StickToCurrentFace())
                    _outerCornerPhase = OuterCornerPhase.None;

                break;
            }

            // Inner corner: stop flush with the next face's exterior lane (surfaceOffset
            // from the wall), then turn — same L alignment as outer corners.
            RaycastHit2D forwardHit = RaycastSurface(
                (Vector2)transform.position,
                _alongSurface,
                remaining + surfaceOffset + skinWidth
            );

            if (forwardHit.collider != null)
            {
                float distanceToFlush = forwardHit.distance - surfaceOffset;

                // Only turn once this step reaches the flush lane; otherwise keep crawling.
                if (distanceToFlush <= remaining)
                {
                    if (distanceToFlush > 0f)
                    {
                        transform.position += (Vector3)(_alongSurface * distanceToFlush);
                        remaining -= distanceToFlush;
                    }
                    else
                    {
                        // Already past flush (embedded) — ease back onto the lane.
                        transform.position += (Vector3)(_alongSurface * distanceToFlush);
                    }

                    RotateInnerCorner();
                    if (!StickToCurrentFace())
                        _outerCornerPhase = OuterCornerPhase.SeekNewFace;
                    continue;
                }
            }

            Vector2 candidate = (Vector2)transform.position + _alongSurface * remaining;
            Vector2 clingOrigin = candidate - _intoSurface * skinWidth;
            RaycastHit2D clingHit = RaycastSurface(
                clingOrigin,
                _intoSurface,
                surfaceOffset + clingRayLength
            );

            if (clingHit.collider != null)
            {
                ApplySurfaceHit(clingHit);
                remaining = 0f;
                break;
            }

            // Outer corner: walk to the last in-contact pose, then begin the L overhang.
            // DistanceToOuterCorner lands on the last pose that still has contact, so we
            // detect a real corner by probing just past that edge.
            float toCorner = DistanceToOuterCorner(remaining);
            transform.position += (Vector3)(_alongSurface * toCorner);
            remaining -= toCorner;

            const float cornerProbe = 0.01f;
            bool lostContactPastEdge = !HasSurfaceContact(
                (Vector2)transform.position + _alongSurface * cornerProbe
            );

            if (!lostContactPastEdge)
            {
                // Still on the face for the rest of this step — not a corner.
                StickToCurrentFace();
                remaining = 0f;
                break;
            }

            _outerCornerPhase = OuterCornerPhase.Overhang;
            _outerCornerOverhangRemaining = surfaceOffset;
        }
    }

    private float DistanceToOuterCorner(float maxDistance)
    {
        float lo = 0f;
        float hi = maxDistance;
        Vector2 start = transform.position;

        // If even a tiny step loses contact, corner is immediate.
        if (!HasSurfaceContact(start + _alongSurface * 0.001f))
            return 0f;

        for (int i = 0; i < 12; i++)
        {
            float mid = (lo + hi) * 0.5f;
            if (HasSurfaceContact(start + _alongSurface * mid))
                lo = mid;
            else
                hi = mid;
        }

        return lo;
    }

    private bool HasSurfaceContact(Vector2 position)
    {
        Vector2 origin = position - _intoSurface * skinWidth;
        RaycastHit2D hit = RaycastSurface(origin, _intoSurface, surfaceOffset + clingRayLength);
        return hit.collider != null;
    }

    private bool StickToCurrentFace()
    {
        Vector2 origin = (Vector2)transform.position - _intoSurface * skinWidth;
        RaycastHit2D hit = RaycastSurface(origin, _intoSurface, surfaceOffset + clingRayLength);
        if (hit.collider == null)
        {
            // Cast from further outside after a corner wrap.
            origin = (Vector2)transform.position - _intoSurface * (surfaceOffset + skinWidth);
            hit = RaycastSurface(origin, _intoSurface, surfaceOffset + clingRayLength + 0.5f);
        }

        if (hit.collider == null)
            return false;

        ApplySurfaceHit(hit);
        return true;
    }

    private void RotateOuterCorner()
    {
        if (crawlDirection == CrawlDirection.Clockwise)
        {
            _intoSurface = RotateCW(_intoSurface);
            _alongSurface = RotateCW(_alongSurface);
        }
        else
        {
            _intoSurface = RotateCCW(_intoSurface);
            _alongSurface = RotateCCW(_alongSurface);
        }
    }

    private void RotateInnerCorner()
    {
        if (crawlDirection == CrawlDirection.Clockwise)
        {
            _intoSurface = RotateCCW(_intoSurface);
            _alongSurface = RotateCCW(_alongSurface);
        }
        else
        {
            _intoSurface = RotateCW(_intoSurface);
            _alongSurface = RotateCW(_alongSurface);
        }
    }

    private Vector2 PerpendicularForCrawl(Vector2 into)
    {
        // Keep the surface on the crawler's "right" for CW (right-hand rule), left for CCW.
        return crawlDirection == CrawlDirection.Clockwise
            ? RotateCCW(into)
            : RotateCW(into);
    }

    private void ApplyVisualOrientation()
    {
        if (!rotateSpriteToSurface)
        {
            transform.rotation = Quaternion.identity;
            return;
        }

        Vector2 outward = -_intoSurface;
        float z = Mathf.Atan2(outward.y, outward.x) * Mathf.Rad2Deg + spriteFacingOffsetDegrees;
        transform.rotation = Quaternion.Euler(0f, 0f, z);
    }

    private RaycastHit2D RaycastSurface(Vector2 origin, Vector2 direction, float distance)
    {
        if (_surfaceCollider == null || direction.sqrMagnitude < 0.0001f)
            return default;

        ContactFilter2D filter = new ContactFilter2D
        {
            useTriggers = false,
            useLayerMask = true,
            layerMask = groundLayer
        };

        int count = Physics2D.Raycast(origin, direction, filter, _rayHits, distance);
        float bestDist = float.MaxValue;
        RaycastHit2D best = default;

        for (int i = 0; i < count; i++)
        {
            RaycastHit2D hit = _rayHits[i];
            if (hit.collider != _surfaceCollider)
                continue;

            if (_hasPathLock && !IsPointOnLockedPath(hit.point))
                continue;

            if (hit.distance < bestDist)
            {
                bestDist = hit.distance;
                best = hit;
            }
        }

        return best;
    }

    private bool IsPointOverlappingSurface(Vector2 point)
    {
        ContactFilter2D filter = new ContactFilter2D
        {
            useTriggers = false,
            useLayerMask = true,
            layerMask = groundLayer
        };

        int count = Physics2D.OverlapPoint(point, filter, _overlapHits);
        for (int i = 0; i < count; i++)
        {
            if (_overlapHits[i] == _surfaceCollider)
                return true;
        }

        return false;
    }

    private void ClearPathLock()
    {
        _hasPathLock = false;
        _lockedPathWorld.Clear();
    }

    private bool TryLockNearestPath(CompositeCollider2D composite, Vector2 worldPos)
    {
        ClearPathLock();

        int pathCount = composite.pathCount;
        if (pathCount <= 0)
            return false;

        Transform compositeTransform = composite.transform;
        float bestDist = float.MaxValue;
        int bestPathIndex = -1;

        for (int pathIndex = 0; pathIndex < pathCount; pathIndex++)
        {
            _pathScratch.Clear();
            if (composite.GetPath(pathIndex, _pathScratch) <= 0 || _pathScratch.Count < 2)
                continue;

            float dist = DistanceToLocalPath(worldPos, _pathScratch, compositeTransform);
            if (dist < bestDist)
            {
                bestDist = dist;
                bestPathIndex = pathIndex;
            }
        }

        if (bestPathIndex < 0)
            return false;

        _pathScratch.Clear();
        composite.GetPath(bestPathIndex, _pathScratch);
        _lockedPathWorld.Clear();
        for (int i = 0; i < _pathScratch.Count; i++)
            _lockedPathWorld.Add(compositeTransform.TransformPoint(_pathScratch[i]));

        _hasPathLock = _lockedPathWorld.Count >= 2;
        return _hasPathLock;
    }

    private Vector2 GetClosestSurfacePoint(Vector2 pos)
    {
        if (_hasPathLock)
            return ClosestPointOnLockedPath(pos);

        return _surfaceCollider.ClosestPoint(pos);
    }

    private bool IsPointOnLockedPath(Vector2 worldPoint)
    {
        return DistanceToWorldPath(worldPoint, _lockedPathWorld) <= pathLockTolerance;
    }

    /// <summary>
    /// Even-odd point-in-polygon test against the locked composite path.
    /// Interior of the path = solid island (even when physics geometry is Outline/hollow).
    /// </summary>
    private bool IsPointInsideLockedPathPolygon(Vector2 point)
    {
        int count = _lockedPathWorld.Count;
        if (count < 3)
            return false;

        bool inside = false;
        for (int i = 0, j = count - 1; i < count; j = i++)
        {
            Vector2 a = _lockedPathWorld[i];
            Vector2 b = _lockedPathWorld[j];

            bool crosses = (a.y > point.y) != (b.y > point.y);
            if (!crosses)
                continue;

            float denom = b.y - a.y;
            if (Mathf.Abs(denom) < 0.0000001f)
                continue;

            float xIntersect = (b.x - a.x) * (point.y - a.y) / denom + a.x;
            if (point.x < xIntersect)
                inside = !inside;
        }

        return inside;
    }

    private Vector2 ClosestPointOnLockedPath(Vector2 worldPoint)
    {
        ClosestPointOnWorldPath(worldPoint, _lockedPathWorld, out Vector2 closest);
        return closest;
    }

    private bool TryGetNearestPathSegment(Vector2 worldPoint, out Vector2 a, out Vector2 b)
    {
        a = default;
        b = default;
        if (!_hasPathLock || _lockedPathWorld.Count < 2)
            return false;

        float bestDistSq = float.MaxValue;
        int bestIndex = -1;
        int count = _lockedPathWorld.Count;

        for (int i = 0; i < count; i++)
        {
            Vector2 p0 = _lockedPathWorld[i];
            Vector2 p1 = _lockedPathWorld[(i + 1) % count];
            Vector2 closest = ClosestPointOnSegment(worldPoint, p0, p1);
            float distSq = (worldPoint - closest).sqrMagnitude;
            if (distSq < bestDistSq)
            {
                bestDistSq = distSq;
                bestIndex = i;
            }
        }

        if (bestIndex < 0)
            return false;

        a = _lockedPathWorld[bestIndex];
        b = _lockedPathWorld[(bestIndex + 1) % count];
        return true;
    }

    private static float DistanceToLocalPath(
        Vector2 worldPoint,
        List<Vector2> localPath,
        Transform pathTransform)
    {
        float bestDistSq = float.MaxValue;
        int count = localPath.Count;
        for (int i = 0; i < count; i++)
        {
            Vector2 a = pathTransform.TransformPoint(localPath[i]);
            Vector2 b = pathTransform.TransformPoint(localPath[(i + 1) % count]);
            Vector2 closest = ClosestPointOnSegment(worldPoint, a, b);
            float distSq = (worldPoint - closest).sqrMagnitude;
            if (distSq < bestDistSq)
                bestDistSq = distSq;
        }

        return Mathf.Sqrt(bestDistSq);
    }

    private static float DistanceToWorldPath(Vector2 worldPoint, List<Vector2> worldPath)
    {
        ClosestPointOnWorldPath(worldPoint, worldPath, out Vector2 closest);
        return Vector2.Distance(worldPoint, closest);
    }

    private static void ClosestPointOnWorldPath(
        Vector2 worldPoint,
        List<Vector2> worldPath,
        out Vector2 closest)
    {
        closest = worldPoint;
        float bestDistSq = float.MaxValue;
        int count = worldPath.Count;
        for (int i = 0; i < count; i++)
        {
            Vector2 candidate = ClosestPointOnSegment(
                worldPoint,
                worldPath[i],
                worldPath[(i + 1) % count]
            );
            float distSq = (worldPoint - candidate).sqrMagnitude;
            if (distSq < bestDistSq)
            {
                bestDistSq = distSq;
                closest = candidate;
            }
        }
    }

    private static Vector2 ClosestPointOnSegment(Vector2 point, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float lengthSq = ab.sqrMagnitude;
        if (lengthSq < 0.0000001f)
            return a;

        float t = Mathf.Clamp01(Vector2.Dot(point - a, ab) / lengthSq);
        return a + ab * t;
    }

    private static Vector2 SnapToCardinal(Vector2 normal)
    {
        if (Mathf.Abs(normal.x) > Mathf.Abs(normal.y))
            return normal.x >= 0f ? Vector2.right : Vector2.left;

        return normal.y >= 0f ? Vector2.up : Vector2.down;
    }

    private static Vector2 RotateCW(Vector2 v) => new Vector2(v.y, -v.x);

    private static Vector2 RotateCCW(Vector2 v) => new Vector2(-v.y, v.x);
}
