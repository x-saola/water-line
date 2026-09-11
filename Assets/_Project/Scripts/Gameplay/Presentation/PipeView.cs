using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Delta.ProjectName
{
    // Renders each pipe using the authored art prefabs (Assets/_Project/Prefabs): a fixed
    // PipeAnchor at the pipe's StartCell (position never moves, but its digit tracks
    // MovesRemaining live) plus one Pipeline segment per EDGE between consecutive path cells
    // (not one per cell - this sidesteps corner sprites entirely, since each edge is
    // unambiguously horizontal or vertical and two edges meeting at a turn cell naturally
    // overlap there). Refreshed after every accepted move/retraction; path lengths are small
    // (max move budget 30) so a full per-pipe edge rebuild is cheap and keeps this in lockstep
    // with PipeRuntimeState without incremental diffing.
    public class PipeView : MonoBehaviour
    {
        [SerializeField] float _cellSize = 1f;
        [SerializeField] float _cellSpacing = 0f;

        [Header("Pipe prefabs (Assets/_Project/Prefabs)")]
        [SerializeField] GameObject _anchorPrefab;
        [SerializeField] GameObject _pipelinePrefab;

        public float CellStep => _cellSize + _cellSpacing;

        GridManager _grid;
        readonly Dictionary<PipeId, GameObject> _anchors = new();
        readonly Dictionary<PipeId, TextMeshPro> _anchorLabels = new();
        readonly Dictionary<PipeId, List<GameObject>> _edges = new();

        // Matches the Level Editor's per-pipe anchor colors (board-cell--anchor-a/b/c).
        static readonly Color32 PipeAColor = new(35, 145, 132, 255);
        static readonly Color32 PipeBColor = new(210, 122, 45, 255);
        static readonly Color32 PipeCColor = new(210, 178, 45, 255);

        public void Attach(GridManager grid) => _grid = grid;

        public IReadOnlyList<GameObject> GetSegments(PipeId id) =>
            _edges.TryGetValue(id, out var pool) ? pool : (IReadOnlyList<GameObject>)System.Array.Empty<GameObject>();

        public void RefreshAll()
        {
            if (_grid == null) return;
            foreach (var pipe in _grid.Pipes)
                RefreshPipe(pipe);
        }

        public void Clear()
        {
            foreach (var go in _anchors.Values)
                if (go != null) Destroy(go);
            _anchors.Clear();
            _anchorLabels.Clear();

            foreach (var pool in _edges.Values)
                foreach (var go in pool)
                    if (go != null) Destroy(go);
            _edges.Clear();
            _grid = null;
        }

        void RefreshPipe(PipeRuntimeState pipe)
        {
            if (!_anchors.ContainsKey(pipe.Id))
                BuildAnchor(pipe);

            if (_anchorLabels.TryGetValue(pipe.Id, out var label) && label != null)
                label.text = pipe.MovesRemaining.ToString();

            if (!_edges.TryGetValue(pipe.Id, out var pool))
            {
                pool = new List<GameObject>();
                _edges[pipe.Id] = pool;
            }

            int edgeCount = Mathf.Max(0, pipe.Path.Count - 1);
            while (pool.Count < edgeCount)
                pool.Add(CreateEdge(pipe.Id));
            while (pool.Count > edgeCount)
            {
                var last = pool[^1];
                pool.RemoveAt(pool.Count - 1);
                if (last != null) Destroy(last);
            }

            for (int i = 0; i < edgeCount; i++)
                PositionEdge(pool[i], pipe.Path[i], pipe.Path[i + 1]);
        }

        // Position is fixed at the pipe's start for the whole level; the digit is not (updated
        // every RefreshPipe call to track MovesRemaining live).
        void BuildAnchor(PipeRuntimeState pipe)
        {
            var go = Instantiate(_anchorPrefab, transform);
            go.name = $"Pipe{pipe.Id}_Anchor";
            go.transform.localPosition =
                BoardLayout.CellToLocalPosition(pipe.Path[0], _grid.Level.Columns, _grid.Level.Rows, CellStep);

            var visual = go.transform.Find("Visual");
            if (visual != null)
            {
                // Not tinted per-pipe: the anchor's art already has its own baked-in color, so an
                // arbitrary RGB multiply (e.g. orange for Pipe B) muddies it instead of recoloring
                // it. The moving Pipeline trail (whose base art is neutral white) is what actually
                // needs to read as pipe-distinct during play.
                var label = visual.GetComponentInChildren<TextMeshPro>();
                if (label != null) _anchorLabels[pipe.Id] = label;
            }

            _anchors[pipe.Id] = go;
        }

        GameObject CreateEdge(PipeId id)
        {
            var go = Instantiate(_pipelinePrefab, transform);
            go.name = $"Pipe{id}_Edge";

            var renderer = go.GetComponentInChildren<SpriteRenderer>();
            if (renderer != null) renderer.color = ColorForPipe(id);

            return go;
        }

        // Pipeline's authored art is a vertical pill by default (taller than wide - verified via
        // SpriteRenderer bounds), so a horizontal edge needs a 90 degree rotation and a vertical
        // edge needs none.
        void PositionEdge(GameObject edge, GridCell from, GridCell to)
        {
            var fromPos = BoardLayout.CellToLocalPosition(from, _grid.Level.Columns, _grid.Level.Rows, CellStep);
            var toPos = BoardLayout.CellToLocalPosition(to, _grid.Level.Columns, _grid.Level.Rows, CellStep);

            edge.transform.localPosition = (fromPos + toPos) / 2f;
            bool isHorizontal = to.Column != from.Column;
            edge.transform.localRotation = isHorizontal ? Quaternion.Euler(0f, 0f, 90f) : Quaternion.identity;
        }

        static Color32 ColorForPipe(PipeId id) => id switch
        {
            PipeId.A => PipeAColor,
            PipeId.B => PipeBColor,
            _ => PipeCColor,
        };
    }
}
