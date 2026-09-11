using System.Threading.Tasks;
using UnityEngine;

namespace Delta.ProjectName
{
    // Win-state animation: a brightening pulse travels along each pipe's segments from its
    // anchor to the shared meeting cell. No bespoke water/crocodile-bathtub art exists in this
    // project (see plan's open questions) - this is a functional placeholder that satisfies the
    // spec's structural beats (plays per pipe, anchor-to-meeting-cell, skippable) without
    // fabricating specific art. ReducedMotion defaults false; nothing in the project currently
    // sets it true since there's no accessibility settings surface yet (also flagged in the plan).
    public class WaterFlowAnimator : MonoBehaviour
    {
        [SerializeField] float _segmentDelay = 0.05f;
        [SerializeField] float _pulseDuration = 0.15f;

        public bool ReducedMotion { get; set; }

        public async Task PlayAsync(PipeView pipeView, GridManager grid)
        {
            if (ReducedMotion) return; // caller applies the end-state instantly; this only skips the flourish

            foreach (var pipe in grid.Pipes)
                await AnimatePipeAsync(pipeView, pipe);
        }

        async Task AnimatePipeAsync(PipeView pipeView, PipeRuntimeState pipe)
        {
            var segments = pipeView.GetSegments(pipe.Id);
            foreach (var segment in segments)
            {
                if (segment == null) continue;
                _ = PulseAsync(segment);
                await Task.Delay((int)(_segmentDelay * 1000));
            }
        }

        async Task PulseAsync(GameObject segment)
        {
            // Edges are now prefab wrappers (Pipeline) with the sprite nested under "Visual",
            // not on the root GameObject itself.
            var renderer = segment.GetComponentInChildren<SpriteRenderer>();
            if (renderer == null) return;

            var baseColor = renderer.color;
            var highlight = Color.Lerp(baseColor, Color.white, 0.6f);

            float t = 0f;
            while (t < _pulseDuration)
            {
                t += Time.deltaTime;
                renderer.color = Color.Lerp(highlight, baseColor, t / _pulseDuration);
                await Task.Yield();
            }
            renderer.color = baseColor;
        }
    }
}
