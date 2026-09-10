using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(RectTransform))]
public class UIPathFollower : MonoBehaviour
{
    public enum CurveType { CatmullRom, Bezier }

    public enum AnimationType
    {
        Loop,              // Repeats endlessly, wraps from end back to start
        LoopFixedDuration, // Same as Loop but speed is derived from fixedDuration (one cycle = fixedDuration seconds)
        PingPong,          // Plays forward then reverses back and forth endlessly
        OneTime            // Plays once from start to end, then stops
    }

    [Header("Path Settings")]
    [Tooltip("Waypoints in parent local space (matches anchoredPosition when parent pivot is centered)")]
    public List<Vector2> waypoints = new();
    [Tooltip("Units per second. Not used when animationType is LoopFixedDuration.")]
    public float speed = 300f;

    [Header("Animation")]
    public AnimationType animationType = AnimationType.Loop;
    [Tooltip("Duration in seconds for one full loop cycle. Only used by LoopFixedDuration.")]
    public float fixedDuration = 1f;
    public bool playOnAwake = true;
    public UnityEvent onComplete;

    [Header("Easing")]
    public Ease ease = Ease.Linear;

    [Header("Algorithm")]
    public CurveType curveType = CurveType.CatmullRom;
    [Range(0f, 1f), Tooltip("0 = straight lines, 1 = full curve. Catmull-Rom only.")]
    public float tension = 0.5f;
    [Tooltip("Out-handle per waypoint, controls curve shape. Bezier only.")]
    public List<Vector2> bezierHandles = new();

    [Header("Options")]
    public bool lookAtDirection = true;

    private float t = 0f;
    private float prevT = 0f;
    private int direction = 1; // used by editor Tick() only
    private bool isPlaying = false;
    private RectTransform rectTransform;
    private Tweener tween;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    void Start()
    {
        if (playOnAwake) Play();
    }

    void OnDestroy()
    {
        tween?.Kill();
    }

    void OnValidate()
    {
        bezierHandles ??= new();
        while (bezierHandles.Count < waypoints.Count)
            bezierHandles.Add(Vector2.right * 50f);
        while (bezierHandles.Count > waypoints.Count)
            bezierHandles.RemoveAt(bezierHandles.Count - 1);

        fixedDuration = Mathf.Max(fixedDuration, 0.01f);
    }

    // ── Public API ───────────────────────────────────────────────────────────

    public void Play()
    {
        if (waypoints == null || waypoints.Count < 2) return;

        tween?.Kill();
        t = 0f;
        prevT = 0f;
        direction = 1;
        isPlaying = true;

        int n = waypoints.Count;
        float safeSpeed = Mathf.Max(speed, 0.01f);

        switch (animationType)
        {
            case AnimationType.Loop:
            {
                float duration = n / safeSpeed;
                tween = DOTween.To(() => t, SetT, n, duration)
                    .SetEase(ease)
                    .SetLoops(-1, LoopType.Restart)
                    .OnKill(() => isPlaying = false);
                break;
            }
            case AnimationType.LoopFixedDuration:
            {
                tween = DOTween.To(() => t, SetT, n, fixedDuration)
                    .SetEase(ease)
                    .SetLoops(-1, LoopType.Restart)
                    .OnKill(() => isPlaying = false);
                break;
            }
            case AnimationType.PingPong:
            {
                float duration = (n - 1) / safeSpeed;
                tween = DOTween.To(() => t, SetT, n - 1, duration)
                    .SetEase(ease)
                    .SetLoops(-1, LoopType.Yoyo)
                    .OnKill(() => isPlaying = false);
                break;
            }
            case AnimationType.OneTime:
            {
                float duration = (n - 1) / safeSpeed;
                tween = DOTween.To(() => t, SetT, n - 1, duration)
                    .SetEase(ease)
                    .OnComplete(() =>
                    {
                        isPlaying = false;
                        onComplete?.Invoke();
                    })
                    .OnKill(() => isPlaying = false);
                break;
            }
        }
    }

    public void Stop()
    {
        tween?.Kill();
        tween = null;
        isPlaying = false;
        t = 0f;
        prevT = 0f;
        direction = 1;
    }

    public void Pause()
    {
        tween?.Pause();
        isPlaying = false;
    }

    public void Resume()
    {
        tween?.Play();
        isPlaying = true;
    }

    public bool IsPlaying => isPlaying;

    // ── DOTween setter ───────────────────────────────────────────────────────

    private void SetT(float value)
    {
        prevT = t;
        t = value;
        ApplyPosition();
    }

    // ── Editor preview (no DOTween in edit mode) ─────────────────────────────

    // Called by the custom editor for edit-mode preview.
    public void Tick(float deltaTime)
    {
        if (!isPlaying || waypoints == null || waypoints.Count < 2) return;

        float effectiveSpeed = animationType == AnimationType.LoopFixedDuration
            ? waypoints.Count / fixedDuration
            : speed;

        prevT = t;
        t += direction * effectiveSpeed * deltaTime;

        switch (animationType)
        {
            case AnimationType.Loop:
            case AnimationType.LoopFixedDuration:
            {
                float maxT = waypoints.Count;
                if (t >= maxT) t -= maxT;
                else if (t < 0f) t += maxT;
                break;
            }

            case AnimationType.PingPong:
            {
                float maxT = waypoints.Count - 1;
                if (t >= maxT) { t = maxT; direction = -1; }
                else if (t <= 0f) { t = 0f; direction = 1; }
                break;
            }

            case AnimationType.OneTime:
            {
                float maxT = waypoints.Count - 1;
                if (t >= maxT)
                {
                    t = maxT;
                    isPlaying = false;
                    onComplete?.Invoke();
                }
                break;
            }
        }

        ApplyPosition();
    }

    // ── Position application ─────────────────────────────────────────────────

    void ApplyPosition()
    {
        var rt = rectTransform != null ? rectTransform : GetComponent<RectTransform>();
        Vector2 pos = SamplePath(t);

        if (lookAtDirection)
        {
            int moveDir = (t - prevT) >= 0f ? 1 : -1;
            Vector2 ahead = SamplePath(t + moveDir * 0.05f);
            Vector2 lookDir = ahead - pos;
            if (lookDir.sqrMagnitude > 0.0001f)
            {
                float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg - 90f;
                rt.localRotation = Quaternion.Euler(0f, 0f, angle);
            }
        }

        rt.localPosition = new Vector3(pos.x, pos.y, rt.localPosition.z);
    }

    // ── Curve sampling ───────────────────────────────────────────────────────

    bool IsClosedLoop() =>
        animationType == AnimationType.Loop || animationType == AnimationType.LoopFixedDuration;

    Vector2 SamplePath(float globalT)
    {
        return curveType == CurveType.Bezier
            ? SampleBezier(globalT)
            : SampleCatmullRom(globalT);
    }

    Vector2 SampleCatmullRom(float globalT)
    {
        int n = waypoints.Count;

        if (IsClosedLoop())
        {
            globalT = ((globalT % n) + n) % n;
            int i = (int)globalT;
            float localT = globalT - i;

            Vector2 p0 = waypoints[(i - 1 + n) % n];
            Vector2 p1 = waypoints[i % n];
            Vector2 p2 = waypoints[(i + 1) % n];
            Vector2 p3 = waypoints[(i + 2) % n];

            return Vector2.Lerp(Vector2.Lerp(p1, p2, localT), CatmullRom(p0, p1, p2, p3, localT), tension);
        }
        else
        {
            globalT = Mathf.Clamp(globalT, 0f, n - 1);
            int i = Mathf.Min((int)globalT, n - 2);
            float localT = globalT - i;

            Vector2 p0 = waypoints[Mathf.Max(i - 1, 0)];
            Vector2 p1 = waypoints[i];
            Vector2 p2 = waypoints[i + 1];
            Vector2 p3 = waypoints[Mathf.Min(i + 2, n - 1)];

            return Vector2.Lerp(Vector2.Lerp(p1, p2, localT), CatmullRom(p0, p1, p2, p3, localT), tension);
        }
    }

    Vector2 SampleBezier(float globalT)
    {
        int n = waypoints.Count;

        if (IsClosedLoop())
        {
            globalT = ((globalT % n) + n) % n;
            int i = (int)globalT;
            float localT = globalT - i;
            int next = (i + 1) % n;

            return CubicBezier(
                waypoints[i],
                waypoints[i]    + bezierHandles[i],
                waypoints[next] - bezierHandles[next],
                waypoints[next],
                localT
            );
        }
        else
        {
            globalT = Mathf.Clamp(globalT, 0f, n - 1);
            int i = Mathf.Min((int)globalT, n - 2);
            float localT = globalT - i;

            return CubicBezier(
                waypoints[i],
                waypoints[i]     + bezierHandles[i],
                waypoints[i + 1] - bezierHandles[i + 1],
                waypoints[i + 1],
                localT
            );
        }
    }

    Vector2 CatmullRom(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;
        return 0.5f * (
            2f * p1
            + (-p0 + p2) * t
            + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2
            + (-p0 + 3f * p1 - 3f * p2 + p3) * t3
        );
    }

    Vector2 CubicBezier(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
    {
        float u = 1f - t;
        return u*u*u * p0 + 3f*u*u*t * p1 + 3f*u*t*t * p2 + t*t*t * p3;
    }

    // ── Gizmos ───────────────────────────────────────────────────────────────

    Vector3 LocalToWorld(Vector2 localPos)
    {
        Transform parent = transform.parent;
        if (parent != null)
            return parent.TransformPoint(new Vector3(localPos.x, localPos.y, 0f));
        return new Vector3(localPos.x, localPos.y, 0f);
    }

    void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Count < 2) return;

        int stepsPerSegment = 20;
        int mainSegments = waypoints.Count - 1;
        bool closedLoop = IsClosedLoop();

        Gizmos.color = Color.cyan;
        Vector3 prev = LocalToWorld(SamplePath(0f));
        for (int i = 1; i <= mainSegments * stepsPerSegment; i++)
        {
            Vector3 next = LocalToWorld(SamplePath((float)i / stepsPerSegment));
            Gizmos.DrawLine(prev, next);
            prev = next;
        }

        if (closedLoop)
        {
            Gizmos.color = Color.green;
            Vector3 closePrev = LocalToWorld(SamplePath(mainSegments));
            for (int i = 1; i <= stepsPerSegment; i++)
            {
                Vector3 next = LocalToWorld(SamplePath(mainSegments + (float)i / stepsPerSegment));
                Gizmos.DrawLine(closePrev, next);
                closePrev = next;
            }
        }

        // Arrowheads
        int totalSegments = closedLoop ? waypoints.Count : mainSegments;
        Gizmos.color = Color.white;
        for (int i = 0; i < totalSegments; i++)
        {
            Vector3 mid   = LocalToWorld(SamplePath(i + 0.5f));
            Vector3 ahead = LocalToWorld(SamplePath(i + 0.55f));
            Vector3 dir   = (ahead - mid).normalized;
            Vector3 perp  = new(-dir.y, dir.x, 0f);
            float size    = 15f;
            float half    = size * 0.5f;

            Gizmos.DrawLine(mid + size * dir, mid - half * dir + half * perp);
            Gizmos.DrawLine(mid + size * dir, mid - half * dir - half * perp);
        }

        // Bezier handles
        if (curveType == CurveType.Bezier && bezierHandles != null && bezierHandles.Count == waypoints.Count)
        {
            Gizmos.color = Color.magenta;
            for (int i = 0; i < waypoints.Count; i++)
            {
                Vector3 wp   = LocalToWorld(waypoints[i]);
                Vector3 hOut = LocalToWorld(waypoints[i] + bezierHandles[i]);
                Vector3 hIn  = LocalToWorld(waypoints[i] - bezierHandles[i]);

                Gizmos.DrawLine(wp, hOut);
                Gizmos.DrawSphere(hOut, 8f);
                Gizmos.DrawLine(wp, hIn);
                Gizmos.DrawSphere(hIn, 8f);
            }
        }

        Gizmos.color = Color.yellow;
        foreach (var wp in waypoints)
            Gizmos.DrawSphere(LocalToWorld(wp), 15f);
    }
}
