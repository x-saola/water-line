using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class PathFollower : MonoBehaviour
{
    public enum CurveType { CatmullRom, Bezier }

    [Header("Path Settings")]
    public List<Vector2> waypoints = new();
    public float speed = 3f;

    [Header("Algorithm")]
    public CurveType curveType = CurveType.CatmullRom;
    [Range(0f, 1f), Tooltip("0 = straight lines, 1 = full curve. Catmull-Rom only.")]
    public float tension = 0.5f;
    [Tooltip("Out-handle per waypoint, controls curve shape. Bezier only.")]
    public List<Vector2> bezierHandles = new();

    [Header("Options")]
    public bool loop = false;
    [Tooltip("Reverse along the same path when reaching the end, ignoring the loop curve")]
    public bool bounce = false;
    public bool lookAtDirection = true;

    [Header("Path Visual")]
    public bool showPath = false;

    private float t = 0f;
    private int direction = 1;
    private LineRenderer lineRenderer;
    private bool prevLoop;
    private bool prevBounce;
    private bool prevShowPath;
    private CurveType prevCurveType;

    void OnValidate()
    {
        bezierHandles ??= new();
        while (bezierHandles.Count < waypoints.Count)
            bezierHandles.Add(Vector2.right * 0.5f);
        while (bezierHandles.Count > waypoints.Count)
            bezierHandles.RemoveAt(bezierHandles.Count - 1);
    }

    void Start()
    {
        prevLoop = loop;
        prevBounce = bounce;
        prevShowPath = showPath;
        prevCurveType = curveType;

        if (showPath)
            BuildLineRenderer();
    }

    void BuildLineRenderer()
    {
        lineRenderer = GetComponent<LineRenderer>();

        var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");
        if (shader != null)
            lineRenderer.material = new Material(shader);

        bool isClosedLoop = loop && !bounce;
        lineRenderer.colorGradient = new Gradient
        {
            colorKeys = isClosedLoop
                ? new GradientColorKey[]
                {
                    new(Color.red,                   0.00f),
                    new(new Color(1f, 0.5f, 0f),     0.14f),
                    new(Color.yellow,                0.28f),
                    new(Color.green,                 0.43f),
                    new(Color.cyan,                  0.57f),
                    new(Color.blue,                  0.71f),
                    new(new Color(0.6f, 0f, 1f),     0.86f),
                    new(Color.red,                   1.00f),
                }
                : new GradientColorKey[]
                {
                    new(Color.red,                   0.00f),
                    new(new Color(1f, 0.5f, 0f),     0.17f),
                    new(Color.yellow,                0.33f),
                    new(Color.green,                 0.50f),
                    new(Color.cyan,                  0.67f),
                    new(Color.blue,                  0.83f),
                    new(new Color(0.6f, 0f, 1f),     1.00f),
                },
            alphaKeys = new GradientAlphaKey[] { new(1f, 0f), new(1f, 1f) }
        };

        int segmentCount = isClosedLoop ? waypoints.Count : waypoints.Count - 1;
        int stepsPerSegment = 20;
        int totalPoints = segmentCount * stepsPerSegment + 1;

        lineRenderer.positionCount = totalPoints;
        lineRenderer.loop = false;
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;
        lineRenderer.useWorldSpace = true;

        for (int i = 0; i < totalPoints; i++)
        {
            float globalT = (float)i / stepsPerSegment;
            lineRenderer.SetPosition(i, (Vector3)SamplePath(globalT));
        }
    }

    void Update()
    {
        if (waypoints == null || waypoints.Count < 2) return;

        if (loop != prevLoop || bounce != prevBounce || showPath != prevShowPath || curveType != prevCurveType)
        {
            prevLoop = loop;
            prevBounce = bounce;
            prevShowPath = showPath;
            prevCurveType = curveType;

            if (showPath)
                BuildLineRenderer();
            else if (lineRenderer != null)
                lineRenderer.positionCount = 0;
        }

        t += direction * speed * Time.deltaTime;

        if (loop && !bounce)
        {
            float maxT = waypoints.Count;
            if (t >= maxT) t -= maxT;
            else if (t < 0f) t += maxT;
        }
        else
        {
            float maxT = waypoints.Count - 1;
            if (t >= maxT) { t = maxT; direction = -1; }
            else if (t <= 0f) { t = 0f; direction = 1; }
        }

        Vector2 pos = SamplePath(t);

        if (lookAtDirection)
        {
            Vector2 ahead = SamplePath(t + direction * 0.05f);
            Vector2 dir = ahead - pos;
            if (dir.sqrMagnitude > 0.0001f)
                transform.up = (Vector3)dir.normalized;
        }

        transform.position = (Vector3)pos;
    }

    Vector2 SamplePath(float globalT)
    {
        return curveType == CurveType.Bezier
            ? SampleBezier(globalT)
            : SampleCatmullRom(globalT);
    }

    Vector2 SampleCatmullRom(float globalT)
    {
        int n = waypoints.Count;

        if (loop && !bounce)
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

        if (loop && !bounce)
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

    void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Count < 2) return;

        int stepsPerSegment = 20;
        int mainSegments = waypoints.Count - 1;

        Gizmos.color = Color.cyan;
        Vector3 prev = (Vector3)SamplePath(0f);
        for (int i = 1; i <= mainSegments * stepsPerSegment; i++)
        {
            Vector3 next = (Vector3)SamplePath((float)i / stepsPerSegment);
            Gizmos.DrawLine(prev, next);
            prev = next;
        }

        if (loop && !bounce)
        {
            Gizmos.color = Color.green;
            Vector3 closePrev = (Vector3)SamplePath(mainSegments);
            for (int i = 1; i <= stepsPerSegment; i++)
            {
                Vector3 next = (Vector3)SamplePath(mainSegments + (float)i / stepsPerSegment);
                Gizmos.DrawLine(closePrev, next);
                closePrev = next;
            }
        }

        // Arrowheads
        int totalSegments = (loop && !bounce) ? waypoints.Count : mainSegments;
        Gizmos.color = Color.white;
        for (int i = 0; i < totalSegments; i++)
        {
            Vector3 mid   = (Vector3)SamplePath(i + 0.5f);
            Vector3 ahead = (Vector3)SamplePath(i + 0.55f);
            Vector3 dir   = (ahead - mid).normalized;
            Vector3 perp  = new(-dir.y, dir.x, 0f);
            float size    = 0.15f;
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
                Vector3 wp  = (Vector3)waypoints[i];
                Vector3 hOut = (Vector3)(waypoints[i] + bezierHandles[i]);
                Vector3 hIn  = (Vector3)(waypoints[i] - bezierHandles[i]);

                Gizmos.DrawLine(wp, hOut);
                Gizmos.DrawSphere(hOut, 0.08f);
                Gizmos.DrawLine(wp, hIn);
                Gizmos.DrawSphere(hIn, 0.08f);
            }
        }

        Gizmos.color = Color.yellow;
        foreach (var wp in waypoints)
            Gizmos.DrawSphere((Vector3)wp, 0.15f);
    }
}
