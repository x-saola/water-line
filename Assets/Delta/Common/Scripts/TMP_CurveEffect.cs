using DG.Tweening;
using TMPro;
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(TMP_Text))]
public class TMP_CurveEffect : MonoBehaviour
{
    public enum CurveMode
    {
        Deform,  // vertices independently projected — glyphs stretch/compress
        Rigid    // translate + rotate each char — shape preserved
    }

    public enum AnimType
    {
        AppearZoom,  // scale 0 → 1
        BounceZoom,  // scale 1 → overshoot → 1
        TypeWriter,  // instant pop-on, pure stagger, no tween
        DropIn,      // fall from above (Y offset), landing bounce via ease
        SpinIn,      // rotate from spinStartAngle → 0 while scaling 0 → 1
        WobbleIn,    // decaying left-right wobble while scaling 0 → 1
        ShakeIdle    // continuous looping shake (not an appear animation)
    }

    // ── Curve ─────────────────────────────────────────────────────────────────

    [Header("Curve Shape")]
    [Tooltip("0 = flat  |  +1 = full circle upward  |  -1 = full circle downward")]
    [Range(-1f, 1f)]
    [SerializeField] float curvature = 0.25f;
    [Tooltip("Deform: bends vertex positions.\nRigid: moves & rotates each character, shape preserved.")]
    [SerializeField] CurveMode mode = CurveMode.Rigid;

    // ── Animation — shared ────────────────────────────────────────────────────

    [Header("Character Animation")]
    [SerializeField] AnimType animType = AnimType.AppearZoom;
    [SerializeField] float animDuration = 0.35f;
    [Min(0f)] [SerializeField] float staggerDelay = 0.05f;
    [SerializeField] Ease animEase = Ease.OutBack;
    [SerializeField] bool playOnEnable = true;

    // ── Animation — per type ──────────────────────────────────────────────────

    [Tooltip("BounceZoom: peak scale before settling at 1")]
    [Range(1f, 3f)] [SerializeField] float bounceOvershoot = 1.5f;

    [Tooltip("DropIn: starting Y offset (positive = drop from above, negative = rise from below)")]
    [SerializeField] float dropHeight = 80f;

    [Tooltip("SpinIn: starting rotation in degrees (360 = full spin)")]
    [Range(90f, 720f)] [SerializeField] float spinStartAngle = 360f;

    [Tooltip("WobbleIn: max rotation angle at peak wobble")]
    [Range(5f, 45f)] [SerializeField] float wobbleAmplitude = 15f;
    [Tooltip("WobbleIn: wobble cycles during the appear animation")]
    [Range(1f, 6f)] [SerializeField] float wobbleFrequency = 3f;

    [Tooltip("ShakeIdle: max offset in pixels")]
    [SerializeField] float shakeAmplitude = 5f;
    [Tooltip("ShakeIdle: oscillation speed")]
    [SerializeField] float shakeSpeed = 8f;
    [Tooltip("ShakeIdle: phase offset between adjacent characters (radians)")]
    [Range(0f, 5f)] [SerializeField] float phaseSpread = 1.5f;

    // ── Runtime state ──────────────────────────────────────────────────────────

    TMP_Text _text;
    float[]  _charProgress;   // per-char 0–1 progress (or scale for BounceZoom)
    Sequence _sequence;

    // ── Editor preview state ───────────────────────────────────────────────────

    [System.NonSerialized] public bool    EditorPreviewActive;
    [System.NonSerialized] public float[] EditorPreviewProgress;

    // ── Accessors for the custom editor ───────────────────────────────────────

    public AnimType CurrentAnimType  => animType;
    public float    AnimDuration     => animDuration;
    public float    StaggerDelay     => staggerDelay;
    public Ease     AnimEase         => animEase;
    public float    BounceOvershoot  => bounceOvershoot;
    public float    DropHeight       => dropHeight;
    public float    SpinStartAngle   => spinStartAngle;
    public float    WobbleAmplitude  => wobbleAmplitude;
    public float    WobbleFrequency  => wobbleFrequency;

    public float TotalDuration(int n)
    {
        if (animType == AnimType.TypeWriter) return Mathf.Max(0f, (n - 1) * staggerDelay + 0.05f);
        if (animType == AnimType.ShakeIdle)  return float.MaxValue;
        return Mathf.Max(0f, (n - 1) * staggerDelay + animDuration);
    }

    // ── Unity callbacks ────────────────────────────────────────────────────────

    void Awake() => _text = GetComponent<TMP_Text>();

    void OnEnable()
    {
        _text = GetComponent<TMP_Text>();
        if (!Application.isPlaying) return;
        if (animType == AnimType.ShakeIdle) return; // always driven by LateUpdate, no appear
        if (playOnEnable) PlayAnimation();
        else              InitProgress();
    }

    void OnDisable() => _sequence?.Kill();

    void LateUpdate() => Apply();
    void OnValidate() => Apply();

    // ── Public API ─────────────────────────────────────────────────────────────

    public void PlayAnimation()
    {
        if (!Application.isPlaying) return;
        if (_text == null) _text = GetComponent<TMP_Text>();
        if (_text == null) return;

        _sequence?.Kill();
        _text.ForceMeshUpdate();
        int n = _text.textInfo.characterCount;
        if (n == 0) return;

        _charProgress = new float[n];
        _sequence = DOTween.Sequence();

        switch (animType)
        {
            // All single-progress types share the same DOTween setup (0 → 1, staggered)
            case AnimType.AppearZoom:
            case AnimType.SpinIn:
            case AnimType.WobbleIn:
            case AnimType.DropIn:
                for (int i = 0; i < n; i++)
                {
                    int idx = i;
                    _sequence.Insert(
                        idx * staggerDelay,
                        DOTween.To(() => _charProgress[idx], x => _charProgress[idx] = x, 1f, animDuration)
                               .SetEase(animEase));
                }
                break;

            case AnimType.BounceZoom:
                for (int i = 0; i < n; i++) _charProgress[i] = 1f;
                for (int i = 0; i < n; i++)
                {
                    int idx = i;
                    _sequence.Insert(
                        idx * staggerDelay,
                        DOTween.Sequence()
                            .Append(DOTween.To(() => _charProgress[idx], x => _charProgress[idx] = x,
                                               bounceOvershoot, animDuration * 0.4f).SetEase(Ease.OutQuad))
                            .Append(DOTween.To(() => _charProgress[idx], x => _charProgress[idx] = x,
                                               1f, animDuration * 0.6f).SetEase(Ease.OutElastic)));
                }
                break;

            case AnimType.TypeWriter:
                // Snap to 1 at each character's stagger time — no tween, no duration
                for (int i = 0; i < n; i++)
                {
                    int idx = i;
                    _sequence.InsertCallback(idx * staggerDelay, () => _charProgress[idx] = 1f);
                }
                break;

            case AnimType.ShakeIdle:
                for (int i = 0; i < n; i++) _charProgress[i] = 1f;
                _sequence.Kill(); // nothing to sequence
                break;
        }
    }

    // ── Internal ───────────────────────────────────────────────────────────────

    void InitProgress()
    {
        if (_text == null) return;
        _text.ForceMeshUpdate();
        int n = _text.textInfo.characterCount;
        _charProgress = new float[n];
        float init = (animType == AnimType.BounceZoom || animType == AnimType.ShakeIdle) ? 1f : 0f;
        for (int i = 0; i < n; i++) _charProgress[i] = init;
    }

    void Apply()
    {
        if (_text == null) _text = GetComponent<TMP_Text>();
        if (_text == null) return;

        _text.ForceMeshUpdate();
        TMP_TextInfo info = _text.textInfo;
        int n = info.characterCount;
        if (n == 0) return;

        float[] prog = Application.isPlaying ? _charProgress
                     : (EditorPreviewActive    ? EditorPreviewProgress : null);

        // ShakeIdle always drives offsets even without a progress array
        bool hasProg  = prog != null;
        bool isShake  = animType == AnimType.ShakeIdle;
        bool hasCurve = Mathf.Abs(curvature) >= 0.0001f;

        float minX = 0f, width = 1f;
        float sign = Mathf.Sign(curvature), arcAngle = 0f, radius = 0f;

        if (hasCurve)
        {
            float maxX = float.MinValue;
            minX = float.MaxValue;
            for (int i = 0; i < n; i++)
            {
                TMP_CharacterInfo ci = info.characterInfo[i];
                if (!ci.isVisible) continue;
                Vector3[] v  = info.meshInfo[ci.materialReferenceIndex].vertices;
                int       vi = ci.vertexIndex;
                for (int k = 0; k < 4; k++)
                {
                    if (v[vi + k].x < minX) minX = v[vi + k].x;
                    if (v[vi + k].x > maxX) maxX = v[vi + k].x;
                }
            }
            width = maxX - minX;
            if (width < 0.001f) return;
            arcAngle = Mathf.Abs(curvature) * Mathf.PI * 2f;
            radius   = width / arcAngle;
        }

        float curTime = GetTime();

        for (int i = 0; i < n; i++)
        {
            TMP_CharacterInfo ci = info.characterInfo[i];
            if (!ci.isVisible) continue;

            Vector3[] v  = info.meshInfo[ci.materialReferenceIndex].vertices;
            int       vi = ci.vertexIndex;

            // ── Resolve per-character transforms from progress ───────────────
            float progress   = (hasProg && i < prog.Length) ? prog[i] : 1f;
            float charScale  = 1f;
            float charRot    = 0f;   // extra rotation (radians), added to arc tangent in Rigid mode
            float offX = 0f, offY = 0f;

            if (hasProg || isShake)
            {
                switch (animType)
                {
                    case AnimType.AppearZoom:
                    case AnimType.BounceZoom:
                        charScale = progress;
                        break;

                    case AnimType.TypeWriter:
                        charScale = progress >= 1f ? 1f : 0f;
                        break;

                    case AnimType.DropIn:
                        offY = dropHeight * (1f - progress);
                        break;

                    case AnimType.SpinIn:
                        charScale = progress;
                        charRot   = spinStartAngle * Mathf.Deg2Rad * (1f - progress);
                        break;

                    case AnimType.WobbleIn:
                        charScale = progress;
                        // Decaying oscillation: zero at progress=0 and progress=1
                        charRot = Mathf.Sin(progress * wobbleFrequency * Mathf.PI * 2f)
                                * wobbleAmplitude * Mathf.Deg2Rad
                                * (1f - progress);
                        break;

                    case AnimType.ShakeIdle:
                        offX = Mathf.Sin(curTime * shakeSpeed + i * phaseSpread) * shakeAmplitude;
                        offY = Mathf.Cos(curTime * shakeSpeed * 0.7f + i * phaseSpread + 1.3f) * shakeAmplitude * 0.5f;
                        break;
                }
            }

            // ── Apply transforms + optional curve ────────────────────────────
            if (!hasCurve)
            {
                // Flat: scale → rotate → offset
                if (charScale != 1f)
                {
                    Vector3 c = (v[vi] + v[vi+1] + v[vi+2] + v[vi+3]) * 0.25f;
                    for (int k = 0; k < 4; k++) v[vi+k] = c + (v[vi+k] - c) * charScale;
                }
                if (charRot != 0f)
                {
                    Vector3 c    = (v[vi] + v[vi+1] + v[vi+2] + v[vi+3]) * 0.25f;
                    float cosR   = Mathf.Cos(charRot), sinR = Mathf.Sin(charRot);
                    for (int k = 0; k < 4; k++)
                    {
                        float lx = v[vi+k].x - c.x, ly = v[vi+k].y - c.y;
                        v[vi+k].x = c.x + lx * cosR - ly * sinR;
                        v[vi+k].y = c.y + lx * sinR + ly * cosR;
                    }
                }
                for (int k = 0; k < 4; k++) { v[vi+k].x += offX; v[vi+k].y += offY; }
            }
            else if (mode == CurveMode.Deform)
            {
                // Scale and rotate in flat space before arc projection
                if (charScale != 1f)
                {
                    Vector3 c = (v[vi] + v[vi+1] + v[vi+2] + v[vi+3]) * 0.25f;
                    for (int k = 0; k < 4; k++) v[vi+k] = c + (v[vi+k] - c) * charScale;
                }
                if (charRot != 0f)
                {
                    Vector3 c  = (v[vi] + v[vi+1] + v[vi+2] + v[vi+3]) * 0.25f;
                    float cosR = Mathf.Cos(charRot), sinR = Mathf.Sin(charRot);
                    for (int k = 0; k < 4; k++)
                    {
                        float lx = v[vi+k].x - c.x, ly = v[vi+k].y - c.y;
                        v[vi+k].x = c.x + lx * cosR - ly * sinR;
                        v[vi+k].y = c.y + lx * sinR + ly * cosR;
                    }
                }
                for (int k = 0; k < 4; k++)
                {
                    float vx = v[vi+k].x, vy = v[vi+k].y;
                    float theta = (vx - minX) / width * arcAngle - arcAngle * 0.5f;
                    float rEff  = radius + vy * sign;
                    v[vi+k].x = rEff * Mathf.Sin(theta) + offX;
                    v[vi+k].y = sign * (rEff * Mathf.Cos(theta) - radius) + offY;
                }
            }
            else // CurveMode.Rigid
            {
                float cx    = (v[vi].x + v[vi+1].x + v[vi+2].x + v[vi+3].x) * 0.25f;
                float theta = (cx - minX) / width * arcAngle - arcAngle * 0.5f;
                float arcX  = radius * Mathf.Sin(theta) + offX;
                float arcY  = sign * (radius * Mathf.Cos(theta) - radius) + offY;
                float rot   = -theta * sign + charRot;
                float cosA  = Mathf.Cos(rot), sinA = Mathf.Sin(rot);

                for (int k = 0; k < 4; k++)
                {
                    float lx = (v[vi+k].x - cx) * charScale;
                    float ly = v[vi+k].y * charScale;
                    v[vi+k].x = arcX + lx * cosA - ly * sinA;
                    v[vi+k].y = arcY + lx * sinA + ly * cosA;
                }
            }
        }

        UploadMeshes(info);
    }

    void UploadMeshes(TMP_TextInfo info)
    {
        for (int i = 0; i < info.meshInfo.Length; i++)
        {
            TMP_MeshInfo mi = info.meshInfo[i];
            mi.mesh.SetVertices(mi.vertices);
            _text.UpdateGeometry(mi.mesh, i);
        }
    }

    float GetTime()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
            return (float)UnityEditor.EditorApplication.timeSinceStartup;
#endif
        return Time.time;
    }
}

// ── Custom Editor ─────────────────────────────────────────────────────────────

#if UNITY_EDITOR
[UnityEditor.CustomEditor(typeof(TMP_CurveEffect))]
public class TMP_CurveEffectEditor : UnityEditor.Editor
{
    double _previewStart;
    TMP_CurveEffect _fx;

    void OnEnable()  => _fx = (TMP_CurveEffect)target;
    void OnDisable() => StopEditorPreview();
    void OnDestroy() => StopEditorPreview();

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        var animTypeProp = serializedObject.FindProperty("animType");
        var type = (TMP_CurveEffect.AnimType)animTypeProp.enumValueIndex;

        var it = serializedObject.GetIterator();
        it.NextVisible(true); // skip m_Script
        while (it.NextVisible(false))
        {
            if (ShouldHide(it.name, type)) continue;
            UnityEditor.EditorGUILayout.PropertyField(it, true);
        }

        serializedObject.ApplyModifiedProperties();

        // ── Preview button ─────────────────────────────────────────────────
        UnityEditor.EditorGUILayout.Space(10);

        bool isShake    = type == TMP_CurveEffect.AnimType.ShakeIdle;
        bool shakeActive = isShake && _fx.EditorPreviewActive;

        var prevBg = GUI.backgroundColor;
        GUI.backgroundColor = shakeActive
            ? new Color(0.95f, 0.45f, 0.45f)   // red = stop
            : new Color(0.45f, 0.85f, 0.45f);   // green = play

        string label = shakeActive ? "■  Stop Preview" : "▶   Play Preview";

        if (GUILayout.Button(label, GUILayout.Height(32)))
        {
            if (Application.isPlaying)
                _fx.PlayAnimation();
            else
                StartEditorPreview();  // toggles for ShakeIdle
        }

        GUI.backgroundColor = prevBg;
    }

    // ── Conditional field visibility ──────────────────────────────────────────

    static bool ShouldHide(string prop, TMP_CurveEffect.AnimType t) => prop switch
    {
        "animDuration"    => t is TMP_CurveEffect.AnimType.TypeWriter or TMP_CurveEffect.AnimType.ShakeIdle,
        "animEase"        => t is TMP_CurveEffect.AnimType.BounceZoom or TMP_CurveEffect.AnimType.TypeWriter or TMP_CurveEffect.AnimType.ShakeIdle,
        "playOnEnable"    => t == TMP_CurveEffect.AnimType.ShakeIdle,
        "bounceOvershoot" => t != TMP_CurveEffect.AnimType.BounceZoom,
        "dropHeight"      => t != TMP_CurveEffect.AnimType.DropIn,
        "spinStartAngle"  => t != TMP_CurveEffect.AnimType.SpinIn,
        "wobbleAmplitude" or "wobbleFrequency"              => t != TMP_CurveEffect.AnimType.WobbleIn,
        "shakeAmplitude"  or "shakeSpeed" or "phaseSpread" => t != TMP_CurveEffect.AnimType.ShakeIdle,
        _                 => false
    };

    // ── Edit-mode preview ─────────────────────────────────────────────────────

    void StartEditorPreview()
    {
        if (_fx == null) return;

        bool isShake = _fx.CurrentAnimType == TMP_CurveEffect.AnimType.ShakeIdle;

        // Toggle off for ShakeIdle
        if (_fx.EditorPreviewActive && isShake) { StopEditorPreview(); return; }

        StopEditorPreview();

        if (!_fx.TryGetComponent<TMP_Text>(out var text)) return;
        text.ForceMeshUpdate();
        int n = text.textInfo.characterCount;
        if (n == 0 && !isShake) return;

        int count = Mathf.Max(1, n);
        _fx.EditorPreviewProgress = new float[count];
        float init = (_fx.CurrentAnimType == TMP_CurveEffect.AnimType.BounceZoom ||
                      _fx.CurrentAnimType == TMP_CurveEffect.AnimType.ShakeIdle) ? 1f : 0f;
        for (int i = 0; i < count; i++) _fx.EditorPreviewProgress[i] = init;

        _previewStart = UnityEditor.EditorApplication.timeSinceStartup;
        _fx.EditorPreviewActive = true;

        UnityEditor.EditorApplication.update += EditorTick;
    }

    void StopEditorPreview()
    {
        UnityEditor.EditorApplication.update -= EditorTick;
        if (_fx == null) return;
        _fx.EditorPreviewActive = false;
        if (_fx.EditorPreviewProgress != null)
            for (int i = 0; i < _fx.EditorPreviewProgress.Length; i++)
                _fx.EditorPreviewProgress[i] = 1f;
    }

    void EditorTick()
    {
        if (_fx == null || !_fx.EditorPreviewActive) { StopEditorPreview(); return; }

        bool isShake = _fx.CurrentAnimType == TMP_CurveEffect.AnimType.ShakeIdle;

        if (!isShake)
        {
            float   elapsed = (float)(UnityEditor.EditorApplication.timeSinceStartup - _previewStart);
            float[] prog    = _fx.EditorPreviewProgress;
            int     n       = prog?.Length ?? 0;
            if (n == 0) { StopEditorPreview(); return; }

            float dur     = _fx.AnimDuration;
            float stagger = _fx.StaggerDelay;

            for (int i = 0; i < n; i++)
            {
                float t = Mathf.Clamp01((elapsed - i * stagger) / Mathf.Max(0.001f, dur));

                switch (_fx.CurrentAnimType)
                {
                    case TMP_CurveEffect.AnimType.AppearZoom:
                    case TMP_CurveEffect.AnimType.SpinIn:
                    case TMP_CurveEffect.AnimType.WobbleIn:
                    case TMP_CurveEffect.AnimType.DropIn:
                        prog[i] = ApplyEase(t, _fx.AnimEase);
                        break;
                    case TMP_CurveEffect.AnimType.BounceZoom:
                        prog[i] = EditorBounce(t, _fx.BounceOvershoot);
                        break;
                    case TMP_CurveEffect.AnimType.TypeWriter:
                        prog[i] = elapsed >= i * stagger ? 1f : 0f;
                        break;
                }
            }

            float total = _fx.TotalDuration(n);
            if (elapsed >= total)
            {
                for (int i = 0; i < n; i++) prog[i] = 1f;
                StopEditorPreview();
            }
        }

        UnityEditor.SceneView.RepaintAll();
        UnityEditor.EditorUtility.SetDirty(_fx);
    }

    // ── Easing helpers ────────────────────────────────────────────────────────

    static float ApplyEase(float t, Ease ease) => ease switch
    {
        Ease.OutBack    => EaseOutBack(t),
        Ease.OutBounce  => EaseOutBounce(t),
        Ease.OutElastic => EaseOutElastic(t),
        Ease.OutQuad    => 1f - (1f - t) * (1f - t),
        Ease.OutCubic   => 1f - Mathf.Pow(1f - t, 3f),
        Ease.OutSine    => Mathf.Sin(t * Mathf.PI * 0.5f),
        Ease.InQuad     => t * t,
        Ease.InCubic    => t * t * t,
        _               => t
    };

    static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f, c3 = c1 + 1f;
        float u = t - 1f;
        return 1f + c3 * u * u * u + c1 * u * u;
    }

    static float EaseOutBounce(float t)
    {
        const float n1 = 7.5625f, d1 = 2.75f;
        if (t < 1f / d1)        return n1 * t * t;
        if (t < 2f / d1)      { t -= 1.5f   / d1; return n1 * t * t + 0.75f;     }
        if (t < 2.5f / d1)    { t -= 2.25f  / d1; return n1 * t * t + 0.9375f;   }
                                 t -= 2.625f / d1; return n1 * t * t + 0.984375f;
    }

    static float EaseOutElastic(float t)
    {
        if (t <= 0f) return 0f;
        if (t >= 1f) return 1f;
        const float c4 = 2f * Mathf.PI / 3f;
        return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * c4) + 1f;
    }

    static float EditorBounce(float t, float overshoot)
    {
        if (t < 0.4f)
        {
            float u = t / 0.4f;
            return Mathf.Lerp(1f, overshoot, 1f - (1f - u) * (1f - u));
        }
        float v = (t - 0.4f) / 0.6f;
        return Mathf.Lerp(overshoot, 1f, EaseOutElastic(v));
    }
}
#endif
