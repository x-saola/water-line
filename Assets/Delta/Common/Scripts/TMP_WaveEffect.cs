using System.Threading.Tasks;
using Delta.Core;
using Delta.Services;
using DG.Tweening;
using TMPro;
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(TMP_Text))]
public class TMP_WaveEffect : MonoBehaviour
{
    [Header("Wave Shape")]
    [SerializeField] float amplitude = 10f;
    [Tooltip("Number of full wave cycles across the whole text")]
    [SerializeField] float waveCycles = 1f;
    [Tooltip("Phase shift in degrees — slides the wave left/right")]
    [Range(0f, 360f)]
    [SerializeField] float phaseOffset = 0f;

    [Header("Appear Animation")]
    [SerializeField] float appearDuration = 0.25f;
    [SerializeField] float appearStagger = 0.05f;

    TMP_Text _text;
    float[] _charScales;

    void Awake()
    {
        _text = GetComponent<TMP_Text>();
        if (Application.isPlaying) InitScales(0f);
    }

    void OnEnable()
    {
        _text = GetComponent<TMP_Text>();
        if (Application.isPlaying) InitScales(0f);
    }

    void LateUpdate() => Apply();

    void OnValidate() => Apply();

    void InitScales(float defaultScale)
    {
        if (_text == null) return;
        _text.ForceMeshUpdate();
        int count = _text.textInfo.characterCount;
        _charScales = new float[count];
        for (int i = 0; i < count; i++)
            _charScales[i] = defaultScale;
    }

    public async Task PlayAppearAnimationAsync(bool playSfx = false)
    {
        if (_text == null) return;
        _text.ForceMeshUpdate();
        int charCount = _text.textInfo.characterCount;

        _charScales = new float[charCount]; // all 0 by default

        var audioService = playSfx ? ServiceLocator.Get<IAudioService>() : null;
        var tasks = new Task[charCount];
        for (int i = 0; i < charCount; i++)
        {
            int idx = i;
            var tcs = new TaskCompletionSource<bool>();
            var seq = DOTween.Sequence()
                .AppendInterval(idx * appearStagger);
            if (playSfx)
                seq.AppendCallback(() => audioService.PlaySfx("hard_txt"));
            seq.Append(DOTween.To(
                    () => _charScales[idx],
                    x => _charScales[idx] = x,
                    1f, appearDuration).SetEase(Ease.OutBack))
                .OnComplete(() => tcs.SetResult(true));
            tasks[i] = tcs.Task;
        }

        await Task.WhenAll(tasks);
    }

    void Apply()
    {
        if (_text == null) _text = GetComponent<TMP_Text>();
        if (_text == null) return;

        _text.ForceMeshUpdate();
        TMP_TextInfo textInfo = _text.textInfo;
        int charCount = textInfo.characterCount;
        if (charCount == 0) return;

        bool applyScale = Application.isPlaying && _charScales != null;
        float phaseRad = phaseOffset * Mathf.Deg2Rad;

        // Compute X extents from all visible vertices so wave is continuous in world space
        float minX = float.MaxValue, maxX = float.MinValue;
        for (int i = 0; i < charCount; i++)
        {
            TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
            if (!charInfo.isVisible) continue;
            int meshIndex = charInfo.materialReferenceIndex;
            int vi = charInfo.vertexIndex;
            Vector3[] verts = textInfo.meshInfo[meshIndex].vertices;
            for (int v = 0; v < 4; v++)
            {
                float x = verts[vi + v].x;
                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
            }
        }

        float width = maxX - minX;
        if (width < 0.001f) return;

        for (int i = 0; i < charCount; i++)
        {
            TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
            if (!charInfo.isVisible) continue;

            int meshIndex = charInfo.materialReferenceIndex;
            int vi = charInfo.vertexIndex;
            Vector3[] verts = textInfo.meshInfo[meshIndex].vertices;

            // Precompute wave offsets from original (pre-scale) vertex positions
            float waveY0 = Mathf.Sin((verts[vi + 0].x - minX) / width * waveCycles * Mathf.PI * 2f + phaseRad) * amplitude;
            float waveY1 = Mathf.Sin((verts[vi + 1].x - minX) / width * waveCycles * Mathf.PI * 2f + phaseRad) * amplitude;
            float waveY2 = Mathf.Sin((verts[vi + 2].x - minX) / width * waveCycles * Mathf.PI * 2f + phaseRad) * amplitude;
            float waveY3 = Mathf.Sin((verts[vi + 3].x - minX) / width * waveCycles * Mathf.PI * 2f + phaseRad) * amplitude;

            // Apply per-character scale around the quad center
            if (applyScale && i < _charScales.Length)
            {
                float scale = _charScales[i];
                Vector3 center = (verts[vi] + verts[vi + 1] + verts[vi + 2] + verts[vi + 3]) * 0.25f;
                for (int v = 0; v < 4; v++)
                    verts[vi + v] = center + (verts[vi + v] - center) * scale;
            }

            // Apply wave using original positions so sampling is always smooth
            verts[vi + 0].y += waveY0;
            verts[vi + 1].y += waveY1;
            verts[vi + 2].y += waveY2;
            verts[vi + 3].y += waveY3;
        }

        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            TMP_MeshInfo meshInfo = textInfo.meshInfo[i];
            meshInfo.mesh.vertices = meshInfo.vertices;
            _text.UpdateGeometry(meshInfo.mesh, i);
        }
    }
}
