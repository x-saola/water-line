using System.Collections.Generic;
using UnityEngine;

namespace Delta.ProjectName
{
    // Renders the board using the authored art prefabs (Assets/_Project/Prefabs): a static
    // ground layer (Cell, + Rock overlay where authored) plus dynamic markers for Boulder/Valve/
    // +1 Pipe cells that GridManager's runtime events keep in sync (boulder position, valve
    // open state, +1 Pipe claimed state). Pipe paths/anchors are drawn separately by PipeView.
    // All prefabs are authored at a 1 world-unit-per-cell scale (verified via SpriteRenderer
    // bounds) - _cellSize/_cellSpacing should stay near 1/0 to match.
    public class GridView : MonoBehaviour
    {
        [SerializeField] float _cellSize = 1f;
        [SerializeField] float _cellSpacing = 0f;
        [SerializeField] Camera _boardCamera; // optional: auto-frames the board on Build()

        [Header("Board prefabs (Assets/_Project/Prefabs)")]
        [SerializeField] GameObject _cellPrefab;
        [SerializeField] GameObject _rockPrefab;
        [SerializeField] GameObject _boulderPrefab;
        [SerializeField] GameObject _valvePrefab;
        [SerializeField] GameObject _plusOnePipePrefab;

        [Header("Valve state art")]
        [Tooltip("Shown on the Valve's State child once opened. Leave empty to just hide the State child instead (no art yet).")]
        [SerializeField] Sprite _valveOpenSprite;

        // Applied as a multiply-tint on the +1 Pipe marker's Visual sprite once claimed, since no
        // separate claimed/unclaimed art exists for it (unlike Valve's State child).
        static readonly Color PlusOnePipeClaimedTint = new(0.55f, 0.55f, 0.55f, 1f);

        public float CellStep => _cellSize + _cellSpacing;

        GridManager _grid;
        readonly List<GameObject> _groundCells = new();
        readonly Dictionary<(int, int), GameObject> _boulderMarkers = new();
        readonly Dictionary<(int, int), Transform> _valveStates = new();
        readonly Dictionary<(int, int), SpriteRenderer> _plusOnePipeVisuals = new();

        public void Build(GridManager grid)
        {
            Clear();
            _grid = grid;

            for (int r = 0; r < grid.Level.Rows; r++)
                for (int c = 0; c < grid.Level.Columns; c++)
                    BuildGroundCell(new GridCell(r, c));

            foreach (var obstacle in grid.Level.Obstacles)
            {
                switch (obstacle.Type)
                {
                    case ObstacleType.Boulder: BuildBoulderMarker(obstacle.Cell); break;
                    case ObstacleType.Valve: BuildValveMarker(obstacle.Cell); break;
                    case ObstacleType.PlusOnePipe: BuildPlusOnePipeMarker(obstacle.Cell); break;
                }
            }

            grid.OnBoulderPushed += OnBoulderPushed;
            grid.OnValveOpened += OnValveOpened;
            grid.OnPlusOnePipeClaimed += OnPlusOnePipeClaimed;
            grid.OnPlusOnePipeReleased += OnPlusOnePipeReleased;

            FrameCamera(grid);
        }

        public void Clear()
        {
            if (_grid != null)
            {
                _grid.OnBoulderPushed -= OnBoulderPushed;
                _grid.OnValveOpened -= OnValveOpened;
                _grid.OnPlusOnePipeClaimed -= OnPlusOnePipeClaimed;
                _grid.OnPlusOnePipeReleased -= OnPlusOnePipeReleased;
                _grid = null;
            }

            foreach (var go in _groundCells) if (go != null) Destroy(go);
            _groundCells.Clear();

            foreach (var go in _boulderMarkers.Values) if (go != null) Destroy(go);
            _boulderMarkers.Clear();
            _valveStates.Clear();
            _plusOnePipeVisuals.Clear();
        }

        void BuildGroundCell(GridCell cell)
        {
            var ground = Instantiate(_cellPrefab, transform);
            ground.name = $"Ground_{cell.Row}_{cell.Column}";
            ground.transform.localPosition = BoardLayout.CellToLocalPosition(cell, _grid.Level.Columns, _grid.Level.Rows, CellStep);
            _groundCells.Add(ground);

            if (_grid.AuthoredObstacleAt(cell) == ObstacleType.Rock)
            {
                var rock = Instantiate(_rockPrefab, transform);
                rock.name = $"Rock_{cell.Row}_{cell.Column}";
                rock.transform.localPosition = ground.transform.localPosition;
                _groundCells.Add(rock);
            }
        }

        void BuildBoulderMarker(GridCell cell)
        {
            var go = Instantiate(_boulderPrefab, transform);
            go.name = $"Boulder_{cell.Row}_{cell.Column}";
            go.transform.localPosition = BoardLayout.CellToLocalPosition(cell, _grid.Level.Columns, _grid.Level.Rows, CellStep);
            _boulderMarkers[(cell.Row, cell.Column)] = go;
        }

        void BuildValveMarker(GridCell cell)
        {
            var go = Instantiate(_valvePrefab, transform);
            go.name = $"Valve_{cell.Row}_{cell.Column}";
            go.transform.localPosition = BoardLayout.CellToLocalPosition(cell, _grid.Level.Columns, _grid.Level.Rows, CellStep);

            var state = go.transform.Find("Visual/State");
            if (state != null) _valveStates[(cell.Row, cell.Column)] = state;
        }

        void BuildPlusOnePipeMarker(GridCell cell)
        {
            var go = Instantiate(_plusOnePipePrefab, transform);
            go.name = $"PlusOnePipe_{cell.Row}_{cell.Column}";
            go.transform.localPosition = BoardLayout.CellToLocalPosition(cell, _grid.Level.Columns, _grid.Level.Rows, CellStep);

            var visual = go.GetComponentInChildren<SpriteRenderer>();
            if (visual != null) _plusOnePipeVisuals[(cell.Row, cell.Column)] = visual;
        }

        void OnBoulderPushed(GridCell from, GridCell to)
        {
            var key = (from.Row, from.Column);
            if (!_boulderMarkers.TryGetValue(key, out var go)) return;

            _boulderMarkers.Remove(key);
            _boulderMarkers[(to.Row, to.Column)] = go;
            go.transform.localPosition = BoardLayout.CellToLocalPosition(to, _grid.Level.Columns, _grid.Level.Rows, CellStep);
        }

        void OnValveOpened(GridCell cell)
        {
            if (!_valveStates.TryGetValue((cell.Row, cell.Column), out var state) || state == null) return;

            if (_valveOpenSprite != null)
            {
                var renderer = state.GetComponent<SpriteRenderer>();
                if (renderer != null) renderer.sprite = _valveOpenSprite;
            }
            else
            {
                state.gameObject.SetActive(false); // no open art assigned yet - just hide the closed indicator
            }
        }

        void OnPlusOnePipeClaimed(GridCell cell, PipeId claimant)
        {
            if (_plusOnePipeVisuals.TryGetValue((cell.Row, cell.Column), out var renderer))
                renderer.color = PlusOnePipeClaimedTint;
        }

        void OnPlusOnePipeReleased(GridCell cell)
        {
            if (_plusOnePipeVisuals.TryGetValue((cell.Row, cell.Column), out var renderer))
                renderer.color = Color.white;
        }

        // Sprites render via camera, not Canvas, so the board needs an orthographic camera sized
        // to fit whatever the level's actual dimensions turn out to be.
        void FrameCamera(GridManager grid)
        {
            if (_boardCamera == null) return;

            _boardCamera.orthographic = true;
            float boardWidth = grid.Level.Columns * CellStep;
            float boardHeight = grid.Level.Rows * CellStep;
            float margin = CellStep * 0.5f;
            float aspect = _boardCamera.aspect > 0f ? _boardCamera.aspect : 1f;

            float sizeForHeight = (boardHeight + margin) / 2f;
            float sizeForWidth = (boardWidth + margin) / (2f * aspect);
            _boardCamera.orthographicSize = Mathf.Max(sizeForHeight, sizeForWidth);
        }
    }
}
