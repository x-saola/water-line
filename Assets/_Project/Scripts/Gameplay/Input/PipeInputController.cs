using UnityEngine;

namespace Delta.ProjectName
{
    // Touch/mouse drag input: touching down on a pipe's current head starts dragging it, then
    // crossing into an adjacent cell calls MoveResolver for that step. Hit-testing raycasts
    // against this GameObject's own local XY plane (z=0) via the board camera, then applies
    // BoardLayout's inverse - GridView/PipeView must live on this same transform so their cell
    // positions line up with this input. The board is plain world-space (SpriteRenderer-based),
    // not a UI Canvas, so this cannot use RectTransformUtility.
    public class PipeInputController : MonoBehaviour
    {
        [SerializeField] float _cellSize = 140f;
        [SerializeField] float _cellSpacing = 6f;
        [SerializeField] Camera _boardCamera;

        public float CellStep => _cellSize + _cellSpacing;

        GridManager _grid;
        bool _inputLocked;

        PipeId? _draggingPipe;
        GridCell _lastProcessedCell;

        public event System.Action<PipeId, MoveResult> OnStepResolved;

        public void Attach(GridManager grid) => _grid = grid;

        public void Detach()
        {
            _grid = null;
            _draggingPipe = null;
        }

        public void SetInputLocked(bool locked)
        {
            _inputLocked = locked;
            if (locked) _draggingPipe = null;
        }

        void Update()
        {
            if (_grid == null || _inputLocked) return;

            if (Input.touchCount > 0)
            {
                var touch = Input.GetTouch(0);
                ProcessPointer(touch.position, touch.phase == TouchPhase.Began,
                    touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled);
            }
            else
            {
                ProcessPointer(Input.mousePosition, Input.GetMouseButtonDown(0), Input.GetMouseButtonUp(0));
            }
        }

        void ProcessPointer(Vector2 screenPosition, bool began, bool ended)
        {
            if (!TryScreenPointToCell(screenPosition, out var cell))
            {
                if (ended) _draggingPipe = null;
                return;
            }

            if (began)
            {
                var pipe = FindPipeWithHeadAt(cell);
                if (pipe.HasValue)
                {
                    _draggingPipe = pipe;
                    _lastProcessedCell = cell;
                }
                return;
            }

            if (_draggingPipe.HasValue &&
                (cell.Row != _lastProcessedCell.Row || cell.Column != _lastProcessedCell.Column))
            {
                var pipe = _grid.GetPipe(_draggingPipe.Value);
                var result = MoveResolver.TryStep(_grid, pipe, cell);
                if (result.Success)
                    _lastProcessedCell = cell;
                OnStepResolved?.Invoke(_draggingPipe.Value, result);
            }

            if (ended)
                _draggingPipe = null;
        }

        PipeId? FindPipeWithHeadAt(GridCell cell)
        {
            foreach (var pipe in _grid.Pipes)
                if (pipe.HeadCell.Row == cell.Row && pipe.HeadCell.Column == cell.Column)
                    return pipe.Id;
            return null;
        }

        bool TryScreenPointToCell(Vector2 screenPosition, out GridCell cell)
        {
            cell = default;
            if (_boardCamera == null) return false;

            var ray = _boardCamera.ScreenPointToRay(screenPosition);
            var boardPlane = new Plane(-transform.forward, transform.position);
            if (!boardPlane.Raycast(ray, out float distance))
                return false;

            var local = transform.InverseTransformPoint(ray.GetPoint(distance));

            int column = Mathf.FloorToInt((local.x + _grid.Level.Columns * CellStep / 2f) / CellStep);
            int row = Mathf.FloorToInt((_grid.Level.Rows * CellStep / 2f - local.y) / CellStep);

            if (column < 0 || column >= _grid.Level.Columns || row < 0 || row >= _grid.Level.Rows)
                return false;

            cell = new GridCell(row, column);
            return true;
        }
    }
}
